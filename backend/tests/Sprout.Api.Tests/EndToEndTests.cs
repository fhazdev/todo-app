using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Sprout.Api.Tests;

/// <summary>
/// Walks the API the way the app does: sign up, pick a type, make a list, add and
/// tick items, reorder categories, share it. Each test asserts on the HTTP surface,
/// not on internals.
/// </summary>
public class EndToEndTests(SproutApiFactory factory) : IClassFixture<SproutApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // ── Auth ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Registering_returns_a_session_and_seeds_the_three_starter_types()
    {
        var (client, _, _) = await factory.SignedInClientAsync();

        var types = await client.GetFromJsonAsync<List<ListTypeResponse>>("/api/list-types", Json);

        types.ShouldNotBeNull();
        types.Select(t => t.Name).ShouldBe(["Grocery list", "Movie & show list", "Default list"]);
        types[0].Categories.Select(c => c.Name)
            .ShouldBe(["Fresh produce", "Bread & bakery", "Dairy", "Meat & fish", "Pantry"]);

        // Colours are resolved server-side; the client never derives them.
        types[0].Categories[0].Color.ShouldBe("#c67139");
        types[0].Categories[0].Tint.ShouldBe("#ffe1d0");
        types[0].Categories[0].Deep.ShouldBe("#8c491a");
    }

    [Fact]
    public async Task An_unauthenticated_request_is_401()
    {
        var response = await factory.CreateClient().GetAsync("/api/lists");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Auth_me_requires_a_token()
    {
        // Guards the fix for the AllowAnonymous-on-the-class mistake: /auth/me must
        // never be reachable without a bearer token.
        var response = await factory.CreateClient().GetAsync("/api/auth/me");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_short_password_comes_back_as_a_field_error()
    {
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/auth/register", new { email = "short@example.com", password = "abc" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("errors").GetProperty("password").EnumerateArray().ShouldNotBeEmpty();
    }

    [Fact]
    public async Task A_wrong_password_gives_the_same_answer_as_an_unknown_account()
    {
        var (_, _, email) = await factory.SignedInClientAsync();
        var client = factory.CreateClient();

        var wrongPassword = await client.PostAsJsonAsync(
            "/api/auth/login", new { email, password = "not-the-password" });
        var noSuchAccount = await client.PostAsJsonAsync(
            "/api/auth/login", new { email = "nobody@example.com", password = "not-the-password" });

        wrongPassword.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        noSuchAccount.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var a = await wrongPassword.Content.ReadFromJsonAsync<JsonElement>();
        var b = await noSuchAccount.Content.ReadFromJsonAsync<JsonElement>();
        a.GetProperty("detail").GetString().ShouldBe(b.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task A_refresh_token_can_be_exchanged_once_and_is_then_dead()
    {
        var client = factory.CreateClient();
        var register = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { email = $"{Guid.CreateVersion7():N}@example.com", password = "correct-horse" });

        var session = await register.Content.ReadFromJsonAsync<SproutApiFactory.AuthResponse>(Json);

        var first = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = session!.RefreshToken });
        first.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Rotation: the token just spent must not work a second time.
        var replay = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = session.RefreshToken });
        replay.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── Lists and items ───────────────────────────────────────────────────────

    [Fact]
    public async Task A_list_can_be_created_filled_ticked_and_read_back()
    {
        var (client, _, _) = await factory.SignedInClientAsync();
        var grocery = await GroceryTypeAsync(client);

        var created = await client.PostAsJsonAsync(
            "/api/lists", new { name = "Groceries", listTypeId = grocery.Id });
        created.StatusCode.ShouldBe(HttpStatusCode.Created);

        var list = await created.Content.ReadFromJsonAsync<ListDetailResponse>(Json);
        list!.Name.ShouldBe("Groceries");
        list.MyRole.ShouldBe("Owner");

        var produce = grocery.Categories[0];
        var pantry = grocery.Categories[4];

        await client.PostAsJsonAsync($"/api/lists/{list.Id}/items", new { text = "Olive oil", categoryId = pantry.Id });
        await client.PostAsJsonAsync($"/api/lists/{list.Id}/items", new { text = "Bananas", categoryId = produce.Id });

        var detail = await client.GetFromJsonAsync<ListDetailResponse>($"/api/lists/{list.Id}", Json);

        // Default sort is By category, so the type's order wins over insertion order.
        detail!.Items.Select(i => i.Text).ShouldBe(["Bananas", "Olive oil"]);

        var toggled = await client.PostAsync($"/api/lists/{list.Id}/items/{detail.Items[0].Id}/toggle", null);
        toggled.StatusCode.ShouldBe(HttpStatusCode.OK);

        var afterToggle = await client.GetFromJsonAsync<ListDetailResponse>($"/api/lists/{list.Id}", Json);
        afterToggle!.Items.Count(i => i.IsCompleted).ShouldBe(1);
        afterToggle.Items[^1].Text.ShouldBe("Bananas"); // completed items sink to the end
    }

    [Fact]
    public async Task An_empty_list_name_falls_back_to_Untitled_list()
    {
        var (client, _, _) = await factory.SignedInClientAsync();
        var grocery = await GroceryTypeAsync(client);

        var created = await client.PostAsJsonAsync("/api/lists", new { name = "", listTypeId = grocery.Id });
        var list = await created.Content.ReadFromJsonAsync<ListDetailResponse>(Json);

        list!.Name.ShouldBe("Untitled list");
    }

    [Fact]
    public async Task The_sort_choice_survives_a_reload()
    {
        var (client, _, _) = await factory.SignedInClientAsync();
        var list = await NewListAsync(client);

        var set = await client.PutAsJsonAsync($"/api/lists/{list.Id}/sort", new { sort = "Alphabetical" });
        set.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var reread = await client.GetFromJsonAsync<ListDetailResponse>($"/api/lists/{list.Id}", Json);
        reread!.Sort.ShouldBe("Alphabetical");
    }

    [Fact]
    public async Task Someone_elses_list_is_404_not_403()
    {
        var (owner, _, _) = await factory.SignedInClientAsync();
        var list = await NewListAsync(owner);

        var (stranger, _, _) = await factory.SignedInClientAsync();
        var response = await stranger.GetAsync($"/api/lists/{list.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Categories ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Reordering_a_types_categories_regroups_the_lists_that_use_it()
    {
        var (client, _, _) = await factory.SignedInClientAsync();
        var grocery = await GroceryTypeAsync(client);
        var list = await NewListAsync(client, grocery);

        var produce = grocery.Categories[0];
        var pantry = grocery.Categories[4];

        await client.PostAsJsonAsync($"/api/lists/{list.Id}/items", new { text = "Bananas", categoryId = produce.Id });
        await client.PostAsJsonAsync($"/api/lists/{list.Id}/items", new { text = "Olive oil", categoryId = pantry.Id });

        var before = await client.GetFromJsonAsync<ListDetailResponse>($"/api/lists/{list.Id}", Json);
        before!.Items[0].Text.ShouldBe("Bananas");

        // Walk Pantry from position 4 to position 0.
        for (var i = 0; i < 4; i++)
        {
            var move = await client.PostAsJsonAsync(
                $"/api/list-types/{grocery.Id}/categories/{pantry.Id}/move", new { direction = "up" });
            move.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        var after = await client.GetFromJsonAsync<ListDetailResponse>($"/api/lists/{list.Id}", Json);
        after!.Items[0].Text.ShouldBe("Olive oil");
    }

    [Fact]
    public async Task A_bad_move_direction_is_rejected()
    {
        var (client, _, _) = await factory.SignedInClientAsync();
        var grocery = await GroceryTypeAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/list-types/{grocery.Id}/categories/{grocery.Categories[0].Id}/move",
            new { direction = "sideways" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_new_type_starts_with_one_Uncategorised_category()
    {
        var (client, _, _) = await factory.SignedInClientAsync();

        var created = await client.PostAsJsonAsync(
            "/api/list-types", new { name = "Reading list", blurb = "Books to get to" });

        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var type = await created.Content.ReadFromJsonAsync<ListTypeResponse>(Json);

        type!.Categories.ShouldHaveSingleItem().Name.ShouldBe("Uncategorised");
    }

    [Fact]
    public async Task A_type_still_in_use_cannot_be_deleted()
    {
        var (client, _, _) = await factory.SignedInClientAsync();
        var grocery = await GroceryTypeAsync(client);
        await NewListAsync(client, grocery);

        var response = await client.DeleteAsync($"/api/list-types/{grocery.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // ── Sharing ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task An_invited_account_sees_the_list_and_can_tick_things_off()
    {
        var (owner, _, _) = await factory.SignedInClientAsync();
        var list = await NewListAsync(owner);
        await owner.PostAsJsonAsync($"/api/lists/{list.Id}/items", new { text = "Split the firewood" });

        var friendEmail = $"{Guid.CreateVersion7():N}@example.com";
        var invite = await owner.PostAsJsonAsync($"/api/lists/{list.Id}/members", new { email = friendEmail });
        invite.StatusCode.ShouldBe(HttpStatusCode.Created);

        var invited = await invite.Content.ReadFromJsonAsync<MemberResponse>(Json);
        invited!.Status.ShouldBe("Invited");

        // Signing up with the invited address claims the pending membership.
        var (friend, _, _) = await factory.SignedInClientAsync(friendEmail);

        var theirLists = await friend.GetFromJsonAsync<List<ListSummaryResponse>>("/api/lists", Json);
        var shared = theirLists.ShouldHaveSingleItem();
        shared.Id.ShouldBe(list.Id);
        shared.SharedWithCount.ShouldBe(1);

        var detail = await friend.GetFromJsonAsync<ListDetailResponse>($"/api/lists/{list.Id}", Json);
        var toggle = await friend.PostAsync($"/api/lists/{list.Id}/items/{detail!.Items[0].Id}/toggle", null);
        toggle.StatusCode.ShouldBe(HttpStatusCode.OK);

        // The owner sees the change: completion is shared state.
        var ownersView = await owner.GetFromJsonAsync<ListDetailResponse>($"/api/lists/{list.Id}", Json);
        ownersView!.Items[0].IsCompleted.ShouldBeTrue();
    }

    [Fact]
    public async Task An_editor_cannot_rename_the_list()
    {
        var (owner, _, _) = await factory.SignedInClientAsync();
        var list = await NewListAsync(owner);

        var friendEmail = $"{Guid.CreateVersion7():N}@example.com";
        await owner.PostAsJsonAsync($"/api/lists/{list.Id}/members", new { email = friendEmail });
        var (friend, _, _) = await factory.SignedInClientAsync(friendEmail);

        var response = await friend.PutAsJsonAsync($"/api/lists/{list.Id}", new { name = "Mine now" });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task An_invalid_invitation_address_is_a_field_error()
    {
        var (client, _, _) = await factory.SignedInClientAsync();
        var list = await NewListAsync(client);

        var response = await client.PostAsJsonAsync($"/api/lists/{list.Id}/members", new { email = "not-an-email" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("errors").GetProperty("email").EnumerateArray().ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Health_is_anonymous()
    {
        var response = await factory.CreateClient().GetAsync("/health");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task<ListTypeResponse> GroceryTypeAsync(HttpClient client)
    {
        var types = await client.GetFromJsonAsync<List<ListTypeResponse>>("/api/list-types", Json);
        return types!.First(t => t.Name == "Grocery list");
    }

    private static async Task<ListDetailResponse> NewListAsync(HttpClient client, ListTypeResponse? type = null)
    {
        type ??= await GroceryTypeAsync(client);

        var created = await client.PostAsJsonAsync(
            "/api/lists", new { name = "Weekend at the cabin", listTypeId = type.Id });

        return (await created.Content.ReadFromJsonAsync<ListDetailResponse>(Json))!;
    }

    // Response shapes, declared here so the tests read the JSON the client will read
    // rather than reaching into the server's own DTOs.
    private sealed record CategoryResponse(Guid Id, string Name, int Position, string Color, string Tint, string Deep);

    private sealed record ListTypeResponse(Guid Id, string Name, string? Blurb, List<CategoryResponse> Categories, int ListCount);

    private sealed record ItemResponse(Guid Id, string Text, Guid CategoryId, DateOnly? DueOn, bool IsCompleted);

    private sealed record MemberResponse(Guid Id, Guid? UserId, string DisplayName, string? Email, string Role, string Status);

    private sealed record ListSummaryResponse(Guid Id, string Name, string TypeName, int OpenCount, int SharedWithCount);

    private sealed record ListDetailResponse(
        Guid Id, string Name, ListTypeResponse Type, string Sort, bool ShowCompleted,
        bool IsPlain, string MyRole, List<ItemResponse> Items, List<MemberResponse> Members);
}
