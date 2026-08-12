using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Sprout.Application.Common.Abstractions;
using Sprout.Application.Common.Services;
using Sprout.Domain.Categories;
using Sprout.Domain.Lists;
using Sprout.Infrastructure.Persistence;

namespace Sprout.Application.Tests;

/// <summary>
/// A handler under test, wired to an in-memory database and a stubbed identity.
/// <para>
/// The in-memory provider is enough here because these tests exercise handler
/// behaviour, not SQL. Anything that depends on the real schema belongs in the
/// Flyway migration and the API integration tests.
/// </para>
/// </summary>
public sealed class TestHarness : IDisposable
{
    public TestHarness(Guid? actingUserId = null)
    {
        UserId = actingUserId ?? Guid.CreateVersion7();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"sprout-tests-{Guid.CreateVersion7()}")
            // Handlers use no raw SQL, so the in-memory provider's warning about it
            // would only be noise.
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        Db = new AppDbContext(options);

        // A real object rather than a substitute: ICurrentUser.RequireUserId is a
        // default interface method, and a proxy intercepts it and hands back
        // Guid.Empty instead of running the implementation.
        CurrentUser = new FakeCurrentUser(UserId, "maya@example.com");

        Identity = Substitute.For<IIdentityService>();
        Identity.GetProfilesAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult<IReadOnlyDictionary<Guid, UserProfile>>(
                call.Arg<IEnumerable<Guid>>()
                    .Distinct()
                    .ToDictionary(id => id, id => new UserProfile(
                        id,
                        $"{id:N}@example.com",
                        id == UserId ? "Maya Kern" : "Nina Boye",
                        id == UserId ? "MK" : "NB",
                        "#c67139"))));

        Access = new ListAccess(Db, Identity);
    }

    public Guid UserId { get; }

    public AppDbContext Db { get; }

    public ICurrentUser CurrentUser { get; }

    public IIdentityService Identity { get; }

    public ListAccess Access { get; }

    /// <summary>Adds a saved type with the given categories, in order.</summary>
    public async Task<ListType> GivenTypeAsync(string name, params string[] categories)
    {
        var type = categories.Length == 0
            ? ListType.Create(UserId, name)
            : ListType.CreateWithCategories(UserId, name, null, categories);

        Db.ListTypes.Add(type);
        await Db.SaveChangesAsync();
        return type;
    }

    /// <summary>Adds a saved list owned by the acting user.</summary>
    public async Task<TodoList> GivenListAsync(string name, ListType type)
    {
        var list = TodoList.Create(UserId, name, type.Id);
        Db.TodoLists.Add(list);
        await Db.SaveChangesAsync();
        return list;
    }

    /// <summary>Drops everything EF is tracking, so the next read comes from the store.</summary>
    public void Detach() => Db.ChangeTracker.Clear();

    public void Dispose() => Db.Dispose();
}

/// <summary>A signed-in caller. See the note in <see cref="TestHarness"/> on why this is not a substitute.</summary>
public sealed class FakeCurrentUser(Guid? userId, string? email) : ICurrentUser
{
    public Guid? UserId { get; } = userId;

    public string? Email { get; } = email;
}
