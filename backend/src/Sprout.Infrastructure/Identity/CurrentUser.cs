using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using Sprout.Application.Common.Abstractions;

namespace Sprout.Infrastructure.Identity;

/// <summary>
/// Reads the caller out of the validated bearer token. This is the only place that
/// touches HttpContext for identity, which is what keeps the token issuer swappable
/// without the Application layer noticing.
/// </summary>
public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? UserId =>
        Guid.TryParse(Find(ClaimTypes.NameIdentifier) ?? Find(JwtRegisteredClaimNames.Sub), out var id)
            ? id
            : null;

    public string? Email => Find(ClaimTypes.Email) ?? Find(JwtRegisteredClaimNames.Email);

    private string? Find(string claimType) => accessor.HttpContext?.User.FindFirstValue(claimType);
}
