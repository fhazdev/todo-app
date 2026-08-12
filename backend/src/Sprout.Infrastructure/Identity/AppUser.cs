using Microsoft.AspNetCore.Identity;

namespace Sprout.Infrastructure.Identity;

/// <summary>
/// The Identity user. Lives in Infrastructure on purpose: the Domain knows accounts
/// only as a <see cref="Guid"/>, so nothing in the model depends on ASP.NET Identity.
/// </summary>
public class AppUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Up to two letters for the member avatar circles.</summary>
    public string Initials { get; set; } = string.Empty;

    /// <summary>A hex colour from the design's member palette. Stable for the life of the account.</summary>
    public string AvatarColor { get; set; } = "#c67139";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Roles exist so the schema is ready for them; Sprout does not use them yet.</summary>
public class AppRole : IdentityRole<Guid>
{
    public AppRole() { }

    public AppRole(string name) : base(name) { }
}

/// <summary>
/// A refresh token, stored hashed-by-value so a stolen database row cannot be
/// replayed as-is. Rotated on every use: refreshing revokes the old token and
/// records which one replaced it, so token reuse is detectable.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid UserId { get; set; }

    /// <summary>SHA-256 of the token the client holds. The raw value is never stored.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? RevokedAt { get; set; }

    public Guid? ReplacedByTokenId { get; set; }

    public AppUser? User { get; set; }

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTimeOffset.UtcNow;
}
