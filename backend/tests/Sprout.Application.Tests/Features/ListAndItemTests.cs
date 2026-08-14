using MediatR;
using NSubstitute;
using Sprout.Application.Common.Exceptions;
using Sprout.Application.Features.Items;
using Sprout.Application.Features.Lists;
using Sprout.Application.Features.Members;
using Sprout.Domain.Categories;
using Sprout.Domain.Lists;

namespace Sprout.Application.Tests.Features;

public class ListAndItemTests
{
    private static ItemCommandHandlers Items(TestHarness h) => new(h.Db, h.CurrentUser, h.Access);

    private static ListQueryHandlers Queries(TestHarness h) => new(h.Db, h.CurrentUser, h.Access);

    private static ListCommandHandlers Lists(TestHarness h, IMediator? mediator = null) =>
        new(h.Db, h.CurrentUser, h.Access, mediator ?? Substitute.For<IMediator>());

    // ── Adding items ──────────────────────────────────────────────────────────

    [Fact]
    public async Task An_item_with_no_category_stays_uncategorised()
    {
        using var h = new TestHarness();
        var type = await h.GivenTypeAsync("Grocery list", "Fresh produce", "Dairy");
        var list = await h.GivenListAsync("Groceries", type);

        var item = await Items(h).Handle(new AddItemCommand(list.Id, "Bananas", null, null), default);

        // Not filed into the first category on the user's behalf: categories are
        // optional, so leaving one off is a choice rather than an omission.
        item.CategoryId.ShouldBeNull();
        item.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public async Task An_item_can_be_added_to_a_type_that_has_no_categories()
    {
        using var h = new TestHarness();
        var type = await h.GivenTypeAsync("Default list");
        var list = await h.GivenListAsync("Bits and bobs", type);

        var item = await Items(h).Handle(new AddItemCommand(list.Id, "Ring the vet", null, null), default);

        item.CategoryId.ShouldBeNull();
    }

    [Fact]
    public async Task An_items_category_can_be_cleared_again()
    {
        using var h = new TestHarness();
        var type = await h.GivenTypeAsync("Grocery list", "Dairy");
        var list = await h.GivenListAsync("Groceries", type);
        var dairy = type.OrderedCategories[0].Id;

        var item = await Items(h).Handle(new AddItemCommand(list.Id, "Halloumi", dairy, null), default);
        item.CategoryId.ShouldBe(dairy);

        var cleared = await Items(h).Handle(
            new UpdateItemCommand(list.Id, item.Id, "Halloumi", null, null), default);

        cleared.CategoryId.ShouldBeNull();
    }

    [Fact]
    public async Task An_item_cannot_take_a_category_from_a_different_type()
    {
        using var h = new TestHarness();
        var grocery = await h.GivenTypeAsync("Grocery list", "Dairy");
        var films = await h.GivenTypeAsync("Movie & show list", "Films");
        var list = await h.GivenListAsync("Groceries", grocery);

        var error = await Should.ThrowAsync<ValidationException>(() =>
            Items(h).Handle(
                new AddItemCommand(list.Id, "Dune: Part Two", films.OrderedCategories[0].Id, null), default));

        error.Errors.ShouldContainKey("categoryId");
    }

    [Fact]
    public async Task Toggling_completes_and_reopens_an_item()
    {
        using var h = new TestHarness();
        var type = await h.GivenTypeAsync("Grocery list", "Dairy");
        var list = await h.GivenListAsync("Groceries", type);
        var item = await Items(h).Handle(new AddItemCommand(list.Id, "Halloumi", null, null), default);

        (await Items(h).Handle(new ToggleItemCommand(list.Id, item.Id), default)).IsCompleted.ShouldBeTrue();
        (await Items(h).Handle(new ToggleItemCommand(list.Id, item.Id), default)).IsCompleted.ShouldBeFalse();
    }

    // ── Access ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task A_non_member_gets_not_found_rather_than_forbidden()
    {
        using var owner = new TestHarness();
        var type = await owner.GivenTypeAsync("Grocery list", "Dairy");
        var list = await owner.GivenListAsync("Groceries", type);

        using var stranger = new TestHarness();
        var handlers = new ItemCommandHandlers(
            owner.Db, stranger.CurrentUser, new Common.Services.ListAccess(owner.Db, stranger.Identity));

        // 404, not 403: the API must not confirm that a list they cannot see exists.
        await Should.ThrowAsync<NotFoundException>(() =>
            handlers.Handle(new AddItemCommand(list.Id, "Sneaky", null, null), default));
    }

    [Fact]
    public async Task An_editor_can_add_items_but_not_rename_the_list()
    {
        using var owner = new TestHarness();
        var type = await owner.GivenTypeAsync("Grocery list", "Dairy");
        var list = await owner.GivenListAsync("Groceries", type);

        using var editor = new TestHarness();
        list.AddMember(editor.UserId, ListRole.Editor);
        await owner.Db.SaveChangesAsync();

        var access = new Common.Services.ListAccess(owner.Db, editor.Identity);
        var itemHandlers = new ItemCommandHandlers(owner.Db, editor.CurrentUser, access);
        var listHandlers = new ListCommandHandlers(
            owner.Db, editor.CurrentUser, access, Substitute.For<IMediator>());

        var added = await itemHandlers.Handle(new AddItemCommand(list.Id, "Oat milk", null, null), default);
        added.Text.ShouldBe("Oat milk");

        await Should.ThrowAsync<ForbiddenException>(() =>
            listHandlers.Handle(new RenameListCommand(list.Id, "Renamed by an editor"), default));
    }

    // ── Deleting a list ───────────────────────────────────────────────────────

    [Fact]
    public async Task Deleting_a_list_takes_its_items_and_memberships_with_it()
    {
        using var h = new TestHarness();
        var type = await h.GivenTypeAsync("Grocery list", "Dairy");
        var list = await h.GivenListAsync("Groceries", type);
        await Items(h).Handle(new AddItemCommand(list.Id, "Halloumi", null, null), default);

        await Lists(h).Handle(new DeleteListCommand(list.Id), default);
        h.Detach();

        h.Db.TodoLists.Any(l => l.Id == list.Id).ShouldBeFalse();
        h.Db.TodoItems.Any(i => i.TodoListId == list.Id).ShouldBeFalse();
        h.Db.ListMembers.Any(m => m.TodoListId == list.Id).ShouldBeFalse();

        // The type outlives the list that used it.
        h.Db.ListTypes.Any(t => t.Id == type.Id).ShouldBeTrue();
    }

    [Fact]
    public async Task An_editor_cannot_delete_the_list()
    {
        using var owner = new TestHarness();
        var type = await owner.GivenTypeAsync("Grocery list", "Dairy");
        var list = await owner.GivenListAsync("Groceries", type);

        using var editor = new TestHarness();
        list.AddMember(editor.UserId, ListRole.Editor);
        await owner.Db.SaveChangesAsync();

        var handlers = new ListCommandHandlers(
            owner.Db,
            editor.CurrentUser,
            new Common.Services.ListAccess(owner.Db, editor.Identity),
            Substitute.For<IMediator>());

        // 403 rather than 404 here: an editor already knows the list exists, so
        // there is nothing left to leak by naming the real reason.
        await Should.ThrowAsync<ForbiddenException>(() =>
            handlers.Handle(new DeleteListCommand(list.Id), default));

        owner.Db.TodoLists.Any(l => l.Id == list.Id).ShouldBeTrue();
    }

    [Fact]
    public async Task A_non_member_deleting_a_list_gets_not_found()
    {
        using var owner = new TestHarness();
        var type = await owner.GivenTypeAsync("Grocery list", "Dairy");
        var list = await owner.GivenListAsync("Groceries", type);

        using var stranger = new TestHarness();
        var handlers = new ListCommandHandlers(
            owner.Db,
            stranger.CurrentUser,
            new Common.Services.ListAccess(owner.Db, stranger.Identity),
            Substitute.For<IMediator>());

        await Should.ThrowAsync<NotFoundException>(() =>
            handlers.Handle(new DeleteListCommand(list.Id), default));
    }

    // ── Reading a list ────────────────────────────────────────────────────────

    [Fact]
    public async Task The_detail_view_returns_open_items_in_sort_order_then_completed_ones()
    {
        using var h = new TestHarness();
        var type = await h.GivenTypeAsync("Grocery list", "Fresh produce", "Bread & bakery");
        var list = await h.GivenListAsync("Groceries", type);

        var produce = type.OrderedCategories[0].Id;
        var bakery = type.OrderedCategories[1].Id;

        await Items(h).Handle(new AddItemCommand(list.Id, "Sourdough", bakery, null), default);
        await Items(h).Handle(new AddItemCommand(list.Id, "Bananas", produce, null), default);
        var done = await Items(h).Handle(new AddItemCommand(list.Id, "Halloumi", produce, null), default);
        await Items(h).Handle(new ToggleItemCommand(list.Id, done.Id), default);

        var detail = await Queries(h).Handle(new GetListQuery(list.Id), default);

        detail.Sort.ShouldBe(nameof(SortMode.Category));
        detail.Items.Select(i => i.Text).ShouldBe(["Bananas", "Sourdough", "Halloumi"]);
        detail.Items[^1].IsCompleted.ShouldBeTrue();
        detail.OpenCount.ShouldBe(2);
        detail.CompletedCount.ShouldBe(1);
    }

    [Fact]
    public async Task The_sort_choice_is_stored_per_member()
    {
        using var owner = new TestHarness();
        var type = await owner.GivenTypeAsync("Grocery list", "Dairy");
        var list = await owner.GivenListAsync("Groceries", type);

        using var editor = new TestHarness();
        list.AddMember(editor.UserId, ListRole.Editor);
        await owner.Db.SaveChangesAsync();

        await Lists(owner).Handle(new SetListSortCommand(list.Id, SortMode.Alphabetical), default);

        var editorAccess = new Common.Services.ListAccess(owner.Db, editor.Identity);
        var mine = await Queries(owner).Handle(new GetListQuery(list.Id), default);
        var theirs = await new ListQueryHandlers(owner.Db, editor.CurrentUser, editorAccess)
            .Handle(new GetListQuery(list.Id), default);

        mine.Sort.ShouldBe(nameof(SortMode.Alphabetical));
        theirs.Sort.ShouldBe(nameof(SortMode.Category)); // untouched by the other member
    }

    [Fact]
    public async Task A_list_of_uncategorised_items_reports_itself_as_plain()
    {
        using var h = new TestHarness();
        var type = await h.GivenTypeAsync("Reading list"); // no categories
        var list = await h.GivenListAsync("Someday", type);
        await Items(h).Handle(new AddItemCommand(list.Id, "Piranesi", null, null), default);

        var detail = await Queries(h).Handle(new GetListQuery(list.Id), default);

        detail.IsPlain.ShouldBeTrue();
        detail.Type.Categories.ShouldBeEmpty();
    }

    [Fact]
    public async Task The_home_screen_shows_lists_the_caller_was_invited_to()
    {
        using var owner = new TestHarness();
        var type = await owner.GivenTypeAsync("Grocery list", "Dairy");
        var list = await owner.GivenListAsync("Groceries", type);

        using var friend = new TestHarness();
        list.AddMember(friend.UserId, ListRole.Editor);
        await owner.Db.SaveChangesAsync();

        var friendAccess = new Common.Services.ListAccess(owner.Db, friend.Identity);
        var cards = await new ListQueryHandlers(owner.Db, friend.CurrentUser, friendAccess)
            .Handle(new GetListsQuery(), default);

        var card = cards.ShouldHaveSingleItem();
        card.Name.ShouldBe("Groceries");
        card.SharedWithCount.ShouldBe(1);
        card.TypeColor.ShouldBe(CategoryPalette.At(0).Color);
    }

    // ── Membership ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Inviting_an_address_with_no_account_leaves_the_membership_pending()
    {
        using var h = new TestHarness();
        var type = await h.GivenTypeAsync("Grocery list", "Dairy");
        var list = await h.GivenListAsync("Groceries", type);

        h.Identity.FindByEmailAsync("sam@example.com", Arg.Any<CancellationToken>())
            .Returns((Common.Abstractions.UserProfile?)null);

        var handlers = new MemberHandlers(h.Db, h.CurrentUser, h.Access, h.Identity);
        var member = await handlers.Handle(new InviteMemberCommand(list.Id, "Sam@Example.com"), default);

        member.Status.ShouldBe(nameof(MembershipStatus.Invited));
        member.UserId.ShouldBeNull();
        member.Email.ShouldBe("sam@example.com");
    }

    [Fact]
    public async Task Inviting_someone_who_already_has_an_account_adds_them_immediately()
    {
        using var h = new TestHarness();
        var type = await h.GivenTypeAsync("Grocery list", "Dairy");
        var list = await h.GivenListAsync("Groceries", type);

        var ninaId = Guid.CreateVersion7();
        h.Identity.FindByEmailAsync("nina@example.com", Arg.Any<CancellationToken>())
            .Returns(new Common.Abstractions.UserProfile(ninaId, "nina@example.com", "Nina Boye", "NB", "#7a8a5e"));

        var handlers = new MemberHandlers(h.Db, h.CurrentUser, h.Access, h.Identity);
        var member = await handlers.Handle(new InviteMemberCommand(list.Id, "nina@example.com"), default);

        member.Status.ShouldBe(nameof(MembershipStatus.Active));
        member.UserId.ShouldBe(ninaId);
    }

    [Fact]
    public async Task Signing_up_claims_any_invitations_addressed_to_that_email()
    {
        using var h = new TestHarness();
        var type = await h.GivenTypeAsync("Grocery list", "Dairy");
        var list = await h.GivenListAsync("Groceries", type);
        list.Invite("sam@example.com");
        await h.Db.SaveChangesAsync();

        var samId = Guid.CreateVersion7();
        var claimed = await new ClaimInvitationsHandler(h.Db)
            .Handle(new ClaimInvitationsCommand(samId, "Sam@Example.com"), default);

        claimed.ShouldBe(1);
        h.Db.ListMembers.Single(m => m.UserId == samId).Status.ShouldBe(MembershipStatus.Active);
    }

    [Fact]
    public async Task Only_the_owner_can_remove_a_member()
    {
        using var owner = new TestHarness();
        var type = await owner.GivenTypeAsync("Grocery list", "Dairy");
        var list = await owner.GivenListAsync("Groceries", type);

        using var editor = new TestHarness();
        var membership = list.AddMember(editor.UserId, ListRole.Editor);
        await owner.Db.SaveChangesAsync();

        var handlers = new MemberHandlers(
            owner.Db,
            editor.CurrentUser,
            new Common.Services.ListAccess(owner.Db, editor.Identity),
            editor.Identity);

        await Should.ThrowAsync<ForbiddenException>(() =>
            handlers.Handle(new RemoveMemberCommand(list.Id, membership.Id), default));
    }
}
