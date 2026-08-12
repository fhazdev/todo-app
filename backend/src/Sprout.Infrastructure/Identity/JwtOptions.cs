using System.ComponentModel.DataAnnotations;

namespace Sprout.Infrastructure.Identity;

/// <summary>
/// Token settings, bound from configuration and validated at startup so a missing
/// signing key fails the boot rather than the first sign-in.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// The HMAC signing key. Must be at least 32 bytes. Supply it as a secret in
    /// every environment; there is deliberately no default.
    /// </summary>
    [Required, MinLength(32)]
    public string SigningKey { get; init; } = string.Empty;

    [Required]
    public string Issuer { get; init; } = "sprout";

    [Required]
    public string Audience { get; init; } = "sprout-app";

    /// <summary>Short by design: the client refreshes silently.</summary>
    [Range(1, 1440)]
    public int AccessTokenMinutes { get; init; } = 15;

    [Range(1, 365)]
    public int RefreshTokenDays { get; init; } = 30;
}

/// <summary>Google sign-in settings. Sign-in with Google is skipped when no client id is set.</summary>
public sealed class GoogleAuthOptions
{
    public const string SectionName = "GoogleAuth";

    /// <summary>The OAuth client id, which is also the audience Google ID tokens must carry.</summary>
    public string ClientId { get; init; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId);
}
