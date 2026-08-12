using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Sprout.Infrastructure.Persistence;

namespace Sprout.Api.Tests;

/// <summary>
/// Hosts the real API in-process, with the database swapped for an in-memory one.
/// Everything else is genuine: routing, model binding, the MediatR pipeline, JWT
/// validation and the problem-details handler.
/// </summary>
public sealed class SproutApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"sprout-api-{Guid.CreateVersion7()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Skips the Flyway-schema startup check, which has nothing to verify here.
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                // Long enough to satisfy the 32-byte minimum the options validator enforces.
                ["Jwt:SigningKey"] = "integration-test-signing-key-at-least-32-bytes-long",
                ["Jwt:Issuer"] = "sprout",
                ["Jwt:Audience"] = "sprout-app",
                ["ConnectionStrings:Sprout"] = "Host=unused;Database=unused",
                ["GoogleAuth:ClientId"] = string.Empty,
            }));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<AppDbContext>();

            // Removing the options object is not enough. Since EF Core 9 the provider
            // is applied through a separate IDbContextOptionsConfiguration<T>
            // registration, which would otherwise still call UseNpgsql on the options
            // built below and leave two providers on one context.
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }

    /// <summary>Registers a fresh account and returns a client already carrying its bearer token.</summary>
    public async Task<(HttpClient Client, Guid UserId, string Email)> SignedInClientAsync(string? email = null)
    {
        var client = CreateClient();
        var address = email ?? $"{Guid.CreateVersion7():N}@example.com";

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { email = address, password = "correct-horse", displayName = "Maya Kern" });

        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>()
            ?? throw new InvalidOperationException("Registration returned no body.");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, auth.User.Id, address);
    }

    public sealed record AuthResponse(UserResponse User, string AccessToken, DateTimeOffset AccessTokenExpiresAt, string RefreshToken);

    public sealed record UserResponse(Guid Id, string Email, string DisplayName, string Initials, string AvatarColor);
}
