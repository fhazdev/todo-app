using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Sprout.Infrastructure.Identity;

/// <summary>
/// Builds the bearer token validation parameters from <see cref="JwtOptions"/>.
/// <para>
/// Deliberately not done inline in AddJwtBearer: that reads configuration at
/// registration time, so any source added afterwards (user secrets, a mounted
/// secrets file, a test host's overrides) is silently ignored and the signing key
/// ends up empty. Resolving through the options pipeline reads the final values.
/// </para>
/// </summary>
public sealed class ConfigureJwtBearerOptions(IOptions<JwtOptions> jwtOptions)
    : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(JwtBearerOptions options)
    {
        var jwt = jwtOptions.Value;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),

            // No grace period on expiry: the client refreshes silently, and the
            // default five minutes is a long time to keep accepting a dead token.
            ClockSkew = TimeSpan.Zero,
        };
    }

    public void Configure(string? name, JwtBearerOptions options) => Configure(options);
}
