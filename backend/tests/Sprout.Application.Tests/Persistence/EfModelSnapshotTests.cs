using Microsoft.EntityFrameworkCore;
using Sprout.Infrastructure.Persistence;

namespace Sprout.Application.Tests.Persistence;

/// <summary>
/// Flyway owns the schema, which means nothing stops the EF model from drifting
/// away from the SQL until a query fails in production. This pins the DDL EF
/// believes in to a committed snapshot: change a mapping and the test fails,
/// telling you to write the matching Flyway migration.
/// <para>
/// The snapshot is not applied to any database. It is the reference the hand-written
/// migrations in <c>db/migrations</c> are kept honest against.
/// </para>
/// </summary>
public class EfModelSnapshotTests
{
    private static readonly string SnapshotPath = Path.Combine(RepoRoot(), "db", "ef-model-snapshot.sql");

    [Fact]
    public void EfModelMatchesCommittedSnapshot()
    {
        var script = GenerateCreateScript();

        if (!File.Exists(SnapshotPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SnapshotPath)!);
            File.WriteAllText(SnapshotPath, script);

            Assert.Fail(
                $"No EF model snapshot existed, so one was written to {SnapshotPath}. "
                + "Review it, write the matching Flyway migration, and commit both.");
        }

        var expected = Normalise(File.ReadAllText(SnapshotPath));
        var actual = Normalise(script);

        Assert.True(
            expected == actual,
            "The EF model no longer matches db/ef-model-snapshot.sql. Write a Flyway migration for the change, "
            + $"then refresh the snapshot. Regenerate with:\n\n{script}");
    }

    /// <summary>The DDL EF Core would emit for the current model, on Npgsql.</summary>
    private static string GenerateCreateScript()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            // Never opened: GenerateCreateScript only needs the provider's SQL dialect.
            .UseNpgsql("Host=localhost;Database=sprout-model-only")
            .Options;

        using var context = new AppDbContext(options);
        return context.Database.GenerateCreateScript();
    }

    private static string Normalise(string sql) =>
        string.Join('\n', sql.ReplaceLineEndings("\n").Split('\n').Select(l => l.TrimEnd())).Trim();

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "design_handoff_shared_todo")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not locate the repository root from the test output directory.");
    }
}
