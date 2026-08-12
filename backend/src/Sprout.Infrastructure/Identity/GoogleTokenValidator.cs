using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using Sprout.Application.Common.Exceptions;

namespace Sprout.Infrastructure.Identity;

/// <summary>What Google tells us about the person behind a verified ID token.</summary>
public sealed record GoogleIdentity(string Subject, string Email, bool EmailVerified, string? Name, string? Picture);

public interface IGoogleTokenValidator
{
    Task<GoogleIdentity> ValidateAsync(string idToken, CancellationToken ct = default);
}

/// <summary>
/// Verifies a Google ID token against Google's published keys and insists the
/// audience is our own client id, so a token minted for a different app cannot be
/// replayed here.
/// </summary>
public sealed class GoogleTokenValidator(IOptions<GoogleAuthOptions> options) : IGoogleTokenValidator
{
    private readonly GoogleAuthOptions _options = options.Value;

    public async Task<GoogleIdentity> ValidateAsync(string idToken, CancellationToken ct = default)
    {
        if (!_options.IsConfigured)
        {
            throw new ForbiddenException("Sign in with Google is not configured on this server.");
        }

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [_options.ClientId],
                });
        }
        catch (InvalidJwtException ex)
        {
            throw new UnauthorisedException($"Google could not verify that sign-in: {ex.Message}");
        }

        return string.IsNullOrWhiteSpace(payload.Email)
            ? throw new UnauthorisedException("That Google account has no email address on it.")
            : new GoogleIdentity(
                payload.Subject,
                payload.Email,
                payload.EmailVerified,
                payload.Name,
                payload.Picture);
    }
}
