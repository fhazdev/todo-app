namespace Sprout.Application.Common.Abstractions;

/// <summary>
/// Everything the Application layer needs from ASP.NET Identity, expressed without
/// naming it. Infrastructure implements this over UserManager and SignInManager.
/// </summary>
public interface IIdentityService
{
    Task<AuthenticatedUser> RegisterAsync(string email, string password, string? displayName, CancellationToken ct = default);

    Task<AuthenticatedUser> SignInAsync(string email, string password, CancellationToken ct = default);

    /// <summary>
    /// Exchanges a Google ID token for a Sprout account, creating one on first sight.
    /// </summary>
    Task<AuthenticatedUser> SignInWithGoogleAsync(string idToken, CancellationToken ct = default);

    Task<AuthenticatedUser> RefreshAsync(string refreshToken, CancellationToken ct = default);

    Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken ct = default);

    Task<UserProfile?> FindByIdAsync(Guid userId, CancellationToken ct = default);

    Task<UserProfile?> FindByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>Profiles for a set of ids, for rendering avatar stacks without N queries.</summary>
    Task<IReadOnlyDictionary<Guid, UserProfile>> GetProfilesAsync(IEnumerable<Guid> userIds, CancellationToken ct = default);
}

/// <summary>A signed-in session: who it is, plus the tokens the client should hold.</summary>
public sealed record AuthenticatedUser(
    UserProfile User,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken);

/// <summary>
/// The public shape of an account. <paramref name="Initials"/> and
/// <paramref name="AvatarColor"/> drive the member avatar circles in the design.
/// </summary>
public sealed record UserProfile(
    Guid Id,
    string Email,
    string DisplayName,
    string Initials,
    string AvatarColor);
