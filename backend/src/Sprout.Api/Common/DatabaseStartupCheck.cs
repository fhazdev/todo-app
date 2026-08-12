using Microsoft.EntityFrameworkCore;
using Sprout.Infrastructure.Persistence;

namespace Sprout.Api.Common;

public static class DatabaseStartupCheck
{
    /// <summary>
    /// Confirms at boot that the database is reachable and that Flyway has already
    /// created the schema, so a misconfigured connection string fails immediately
    /// rather than on the first user's first request.
    /// <para>
    /// This never creates or migrates anything: <c>db/migrations</c> is the only
    /// thing that changes the schema.
    /// </para>
    /// </summary>
    public static async Task VerifyDatabaseAsync(this WebApplication app)
    {
        // The integration tests swap in an in-memory provider, which has no notion
        // of connecting or of tables existing.
        if (app.Environment.IsEnvironment("Testing"))
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!await db.Database.CanConnectAsync())
        {
            throw new InvalidOperationException(
                "Sprout cannot reach its database. Check ConnectionStrings:Sprout.");
        }

        var schemaReady = await db.Database
            .SqlQuery<bool>($"SELECT to_regclass('public.todo_lists') IS NOT NULL AS \"Value\"")
            .SingleAsync();

        if (!schemaReady)
        {
            throw new InvalidOperationException(
                "The database is reachable but empty. Run the Flyway migrations in db/migrations first "
                + "(docker compose up flyway).");
        }

        logger.LogInformation("Sprout database reachable and migrated.");
    }
}
