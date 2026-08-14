using Microsoft.EntityFrameworkCore;
using Sprout.Domain.Categories;
using Sprout.Domain.Lists;
using Sprout.Infrastructure.Identity;
using Sprout.Infrastructure.Persistence;

namespace Sprout.Application.Tests.Persistence;

/// <summary>
/// Proves the EF mappings and the Flyway schema actually agree, by writing and
/// reading every table against a real Postgres.
/// <para>
/// The in-memory tests cannot catch a column-name or type mismatch, and the
/// snapshot test only compares EF to itself. This is the one that fails when the
/// hand-written SQL and the model drift apart.
/// </para>
/// <para>
/// Skipped unless a database is pointed at, so <c>dotnet test</c> stays green on a
/// machine with nothing running:
/// <code>docker compose up -d db &amp;&amp; docker compose run --rm flyway</code>
/// then set SPROUT_TEST_DB, or rely on the localhost default below.
/// </para>
/// </summary>
public class PostgresSchemaTests
{
    private const string DefaultConnection =
        "Host=localhost;Port=5432;Database=sprout;Username=sprout;Password=sprout;Timeout=3";

    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("SPROUT_TEST_DB") ?? DefaultConnection;

    [SkippableFact]
    public async Task Every_table_round_trips_against_the_flyway_schema()
    {
        await using var db = await ConnectOrSkipAsync();

        // ── Arrange: one account, one type with categories, one shared list ────
        var owner = NewUser("maya@example.com", "Maya Kern", "MK");
        var friend = NewUser("nina@example.com", "Nina Boye", "NB");
        db.Users.AddRange(owner, friend);

        var type = ListType.CreateWithCategories(
            owner.Id, $"Grocery list {Guid.CreateVersion7():N}", "Aisles you shop in",
            "Fresh produce", "Bread & bakery", "Dairy");
        db.ListTypes.Add(type);

        var list = TodoList.Create(owner.Id, "Groceries", type.Id);
        list.AddMember(friend.Id, ListRole.Editor);
        list.Invite("sam.oyelaran@example.com");

        var produce = type.OrderedCategories[0];
        var bakery = type.OrderedCategories[1];
        list.AddItem("Rocket and tomatoes", produce.Id, null, owner.Id);
        var sourdough = list.AddItem("Sourdough", bakery.Id, new DateOnly(2026, 8, 15), owner.Id);
        sourdough.Toggle(friend.Id);

        db.TodoLists.Add(list);

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = owner.Id,
            // 64 hex characters, unique per run: the database outlives the test, and
            // ix_refresh_tokens_token_hash is rightly unique.
            TokenHash = $"{Guid.CreateVersion7():N}{Guid.CreateVersion7():N}",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
        });

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        // ── Assert: everything comes back the way it went in ──────────────────
        var reread = await db.TodoLists
            .Include(l => l.ListType!).ThenInclude(t => t.Categories)
            .Include(l => l.Items)
            .Include(l => l.Members)
            .SingleAsync(l => l.Id == list.Id);

        reread.Name.ShouldBe("Groceries");
        reread.ListType!.OrderedCategories.Select(c => c.Name)
            .ShouldBe(["Fresh produce", "Bread & bakery", "Dairy"]);
        reread.ListType.OrderedCategories.Select(c => c.PaletteIndex).ShouldBe([0, 1, 2]);

        reread.Items.Count.ShouldBe(2);
        var completed = reread.Items.Single(i => i.IsCompleted);
        completed.Text.ShouldBe("Sourdough");
        completed.DueOn.ShouldBe(new DateOnly(2026, 8, 15));
        completed.CompletedAt.ShouldNotBeNull();
        completed.CompletedBy.ShouldBe(friend.Id);

        reread.Members.Count.ShouldBe(3);
        reread.Members.Single(m => m.Role == ListRole.Owner).UserId.ShouldBe(owner.Id);
        reread.Members.Single(m => m.Status == MembershipStatus.Invited)
            .InvitedEmail.ShouldBe("sam.oyelaran@example.com");
        reread.Members.ShouldAllBe(m => m.Sort == SortMode.Category);

        // Sorting works over data that came out of Postgres, not just out of memory.
        var sorted = ItemOrdering.Sort(reread.Items, reread.ListType, SortMode.Category);
        sorted[0].Text.ShouldBe("Rocket and tomatoes");
    }

    [SkippableFact]
    public async Task Clearing_items_and_deleting_their_category_saves_in_the_right_order()
    {
        await using var db = await ConnectOrSkipAsync();

        var owner = NewUser($"delcat-{Guid.CreateVersion7():N}@example.com", "Del Cat", "DC");
        db.Users.Add(owner);

        var type = ListType.CreateWithCategories(
            owner.Id, $"Type {Guid.CreateVersion7():N}", null, "Fiction");
        db.ListTypes.Add(type);

        var fiction = type.OrderedCategories[0];
        var list = TodoList.Create(owner.Id, "Someday", type.Id);
        var item = list.AddItem("Piranesi", fiction.Id, null, owner.Id);
        db.TodoLists.Add(list);

        await db.SaveChangesAsync();

        // Both changes go in one SaveChanges, and the order is EF's to get right.
        // todo_items.category_id has no navigation on either entity, so unless the
        // relationship itself is declared EF batches the category DELETE ahead of
        // the item UPDATE and Postgres rejects it on the RESTRICT foreign key. No
        // in-memory test can catch that, because nothing enforces the key there.
        item.Edit(item.Text, null, item.DueOn);
        type.RemoveCategory(fiction.Id);

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        (await db.TodoItems.SingleAsync(i => i.Id == item.Id)).CategoryId.ShouldBeNull();
        (await db.Categories.AnyAsync(c => c.Id == fiction.Id)).ShouldBeFalse();
    }

    [SkippableFact]
    public async Task An_uncategorised_item_round_trips_as_null()
    {
        await using var db = await ConnectOrSkipAsync();

        var owner = NewUser($"loose-{Guid.CreateVersion7():N}@example.com", "Loose End", "LE");
        db.Users.Add(owner);

        // A type with no categories at all, as the Default list now ships.
        var type = ListType.Create(owner.Id, $"Default list {Guid.CreateVersion7():N}", "Anything at all");
        db.ListTypes.Add(type);

        var list = TodoList.Create(owner.Id, "Bits and bobs", type.Id);
        var item = list.AddItem("Ring the vet", null, null, owner.Id);
        db.TodoLists.Add(list);

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var reread = await db.TodoItems.SingleAsync(i => i.Id == item.Id);
        reread.CategoryId.ShouldBeNull();

        var rereadType = await db.ListTypes
            .Include(t => t.Categories)
            .SingleAsync(t => t.Id == type.Id);

        rereadType.Categories.ShouldBeEmpty();
        TodoList.IsPlain([reread], rereadType).ShouldBeTrue();
    }

    [SkippableFact]
    public async Task The_database_refuses_a_second_owner_on_one_list()
    {
        await using var db = await ConnectOrSkipAsync();

        var owner = NewUser($"owner-{Guid.CreateVersion7():N}@example.com", "Owner", "OW");
        var usurper = NewUser($"usurper-{Guid.CreateVersion7():N}@example.com", "Usurper", "US");
        db.Users.AddRange(owner, usurper);

        var type = ListType.Create(owner.Id, $"Type {Guid.CreateVersion7():N}");
        db.ListTypes.Add(type);

        var list = TodoList.Create(owner.Id, "Groceries", type.Id);
        list.AddMember(usurper.Id, ListRole.Owner); // the domain allows it; the index does not
        db.TodoLists.Add(list);

        // ix_list_members_one_owner is a partial unique index, which only a real
        // database can enforce.
        await Should.ThrowAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [SkippableFact]
    public async Task The_database_refuses_a_category_name_that_differs_only_by_case()
    {
        await using var db = await ConnectOrSkipAsync();

        var owner = NewUser($"case-{Guid.CreateVersion7():N}@example.com", "Case Test", "CT");
        db.Users.Add(owner);

        var type = ListType.CreateWithCategories(
            owner.Id, $"Type {Guid.CreateVersion7():N}", null, "Dairy");
        db.ListTypes.Add(type);
        await db.SaveChangesAsync();

        // Bypasses ListType.AddCategory's own check to prove SQL is the backstop.
        db.Categories.Add(NewRawCategory(type.Id, "DAIRY"));

        await Should.ThrowAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    private static Category NewRawCategory(Guid listTypeId, string name)
    {
        var category = (Category)Activator.CreateInstance(typeof(Category), nonPublic: true)!;

        // Category's setters are private by design; reflection is the honest way to
        // build the invalid row this test needs.
        Set(category, nameof(Category.ListTypeId), listTypeId);
        Set(category, nameof(Category.Name), name);
        Set(category, nameof(Category.PaletteIndex), 1);
        Set(category, nameof(Category.Position), 1);

        return category;

        static void Set(object target, string property, object value) =>
            target.GetType().GetProperty(property)!.SetValue(target, value);
    }

    private static AppUser NewUser(string email, string displayName, string initials) => new()
    {
        Id = Guid.CreateVersion7(),
        Email = $"{Guid.CreateVersion7():N}-{email}",
        UserName = $"{Guid.CreateVersion7():N}-{email}",
        NormalizedEmail = $"{Guid.CreateVersion7():N}-{email}".ToUpperInvariant(),
        NormalizedUserName = $"{Guid.CreateVersion7():N}-{email}".ToUpperInvariant(),
        DisplayName = displayName,
        Initials = initials,
        AvatarColor = "#c67139",
        SecurityStamp = Guid.CreateVersion7().ToString(),
    };

    private static async Task<AppDbContext> ConnectOrSkipAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        var db = new AppDbContext(options);

        var reachable = false;
        try
        {
            reachable = await db.Database.CanConnectAsync();
        }
        catch
        {
            // Treated the same as unreachable: the point is to skip, not to fail.
        }

        if (!reachable)
        {
            await db.DisposeAsync();
            Skip.If(true, "No Sprout Postgres reachable. Run: docker compose up -d db && docker compose run --rm flyway");
        }

        return db;
    }
}
