using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sprout.Application.Common.Abstractions;
using Sprout.Application.Common.Exceptions;
using Sprout.Application.Common.Services;
using Sprout.Application.Features.Members;
using Sprout.Domain.Categories;
using Sprout.Infrastructure.Persistence;

namespace Sprout.Infrastructure.Identity;

/// <summary>
/// ASP.NET Identity behind the Application layer's <see cref="IIdentityService"/>.
/// Owns account creation, sign-in, Google exchange and refresh-token rotation.
/// </summary>
public sealed class IdentityService(
    UserManager<AppUser> userManager,
    AppDbContext db,
    ITokenService tokens,
    IGoogleTokenValidator google,
    IMediator mediator) : IIdentityService
{
    public async Task<AuthenticatedUser> RegisterAsync(
        string email,
        string password,
        string? displayName,
        CancellationToken ct = default)
    {
        var normalised = email.Trim().ToLowerInvariant();

        if (await userManager.FindByEmailAsync(normalised) is not null)
        {
            throw new ConflictException("There is already an account with that email address.");
        }

        var user = NewUser(normalised, displayName);
        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            // Identity's password rules land on the password field so the sign-in
            // form can show them inline.
            throw new Application.Common.Exceptions.ValidationException(
                new Dictionary<string, string[]>
                {
                    ["password"] = [.. result.Errors.Select(e => e.Description)],
                });
        }

        await SeedStarterTypesAsync(user.Id, ct);
        await ClaimInvitationsAsync(user, ct);

        return await IssueSessionAsync(user, ct);
    }

    public async Task<AuthenticatedUser> SignInAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(email.Trim().ToLowerInvariant());

        // One message for both branches, so the endpoint cannot be used to discover
        // which addresses have accounts.
        if (user is null || !await userManager.CheckPasswordAsync(user, password))
        {
            throw new UnauthorisedException("That email and password do not match an account.");
        }

        await ClaimInvitationsAsync(user, ct);
        return await IssueSessionAsync(user, ct);
    }

    public async Task<AuthenticatedUser> SignInWithGoogleAsync(string idToken, CancellationToken ct = default)
    {
        var identity = await google.ValidateAsync(idToken, ct);
        var email = identity.Email.Trim().ToLowerInvariant();

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = NewUser(email, identity.Name);
            user.EmailConfirmed = identity.EmailVerified;

            var created = await userManager.CreateAsync(user);
            if (!created.Succeeded)
            {
                throw new ConflictException(
                    $"That Google account could not be linked: {string.Join(", ", created.Errors.Select(e => e.Description))}");
            }

            await userManager.AddLoginAsync(user, new UserLoginInfo("Google", identity.Subject, "Google"));
            await SeedStarterTypesAsync(user.Id, ct);
        }

        await ClaimInvitationsAsync(user, ct);
        return await IssueSessionAsync(user, ct);
    }

    public async Task<AuthenticatedUser> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = tokens.Hash(refreshToken);

        var stored = await db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is null || !stored.IsActive)
        {
            throw new UnauthorisedException("That session has expired. Sign in again.");
        }

        var user = stored.User ?? throw new UnauthorisedException("That session no longer matches an account.");

        // Rotate: the presented token dies here and records its successor, so a
        // replayed token is recognisable as reuse.
        var (raw, replacement) = tokens.CreateRefreshToken(user.Id);
        stored.RevokedAt = DateTimeOffset.UtcNow;
        stored.ReplacedByTokenId = replacement.Id;

        db.RefreshTokens.Add(replacement);
        await db.SaveChangesAsync(ct);

        var (accessToken, expiresAt) = tokens.CreateAccessToken(user);
        return new AuthenticatedUser(ToProfile(user), accessToken, expiresAt, raw);
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = tokens.Hash(refreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is { RevokedAt: null })
        {
            stored.RevokedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<UserProfile?> FindByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        return user is null ? null : ToProfile(user);
    }

    public async Task<UserProfile?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalised = email.Trim().ToUpperInvariant();
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.NormalizedEmail == normalised, ct);
        return user is null ? null : ToProfile(user);
    }

    public async Task<IReadOnlyDictionary<Guid, UserProfile>> GetProfilesAsync(
        IEnumerable<Guid> userIds,
        CancellationToken ct = default)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, UserProfile>();
        }

        var users = await db.Users.AsNoTracking().Where(u => ids.Contains(u.Id)).ToListAsync(ct);
        return users.ToDictionary(u => u.Id, ToProfile);
    }

    private async Task<AuthenticatedUser> IssueSessionAsync(AppUser user, CancellationToken ct)
    {
        var (accessToken, expiresAt) = tokens.CreateAccessToken(user);
        var (raw, record) = tokens.CreateRefreshToken(user.Id);

        db.RefreshTokens.Add(record);
        await db.SaveChangesAsync(ct);

        return new AuthenticatedUser(ToProfile(user), accessToken, expiresAt, raw);
    }

    private Task ClaimInvitationsAsync(AppUser user, CancellationToken ct) =>
        mediator.Send(new ClaimInvitationsCommand(user.Id, user.Email ?? string.Empty), ct);

    /// <summary>
    /// Gives a new account the three types from the design, so the first list they
    /// create has something to choose from rather than an empty picker.
    /// </summary>
    private async Task SeedStarterTypesAsync(Guid userId, CancellationToken ct)
    {
        if (await db.ListTypes.AnyAsync(t => t.OwnerId == userId, ct))
        {
            return;
        }

        db.ListTypes.AddRange(
            ListType.CreateWithCategories(
                userId, "Grocery list", "Aisles you shop in",
                "Fresh produce", "Bread & bakery", "Dairy", "Meat & fish", "Pantry"),
            ListType.CreateWithCategories(
                userId, "Movie & show list", "What to watch, sorted",
                "Films", "Series", "Documentary", "With the kids"),
            // No categories: the default kind is a plain checklist, and categories are
            // something you add when you decide you want them.
            ListType.Create(userId, "Default list", "Anything at all").MarkAsDefault());

        await db.SaveChangesAsync(ct);
    }

    private static AppUser NewUser(string email, string? displayName)
    {
        var name = string.IsNullOrWhiteSpace(displayName) ? email.Split('@')[0] : displayName.Trim();

        return new AppUser
        {
            Id = Guid.CreateVersion7(),
            Email = email,
            UserName = email,
            DisplayName = name,
            Initials = ListAccess.InitialsFrom(name),
            AvatarColor = ListAccess.AvatarColorFor(email),
        };
    }

    private static UserProfile ToProfile(AppUser user) =>
        new(user.Id, user.Email ?? string.Empty, user.DisplayName, user.Initials, user.AvatarColor);
}
