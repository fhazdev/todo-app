using Microsoft.EntityFrameworkCore;
using Sprout.Application.Common.Exceptions;
using Sprout.Application.Features.ListTypes;
using Sprout.Domain.Categories;
using Sprout.Domain.Common;

namespace Sprout.Application.Tests.Features;

public class ListTypeCommandTests
{
    private static ListTypeCommandHandlers Handlers(TestHarness harness) =>
        new(harness.Db, harness.CurrentUser);

    [Fact]
    public async Task Creating_a_type_leaves_it_without_categories()
    {
        using var harness = new TestHarness();

        var type = await Handlers(harness).Handle(
            new CreateListTypeCommand("Reading list", "Books to get to"), default);

        type.Categories.ShouldBeEmpty();
        type.Blurb.ShouldBe("Books to get to");
    }

    [Fact]
    public async Task Two_types_cannot_share_a_name_within_one_account()
    {
        using var harness = new TestHarness();
        await harness.GivenTypeAsync("Grocery list", "Dairy");

        await Should.ThrowAsync<ConflictException>(() =>
            Handlers(harness).Handle(new CreateListTypeCommand("grocery list", null), default));
    }

    [Fact]
    public async Task Another_account_may_use_the_same_type_name()
    {
        using var mine = new TestHarness();
        await mine.GivenTypeAsync("Grocery list", "Dairy");

        // A second account against the same store: the uniqueness rule is per owner.
        var theirs = new TestHarness();
        var type = await Handlers(theirs).Handle(new CreateListTypeCommand("Grocery list", null), default);

        type.Name.ShouldBe("Grocery list");
        theirs.Dispose();
    }

    [Fact]
    public async Task Adding_a_category_appends_it_with_the_next_palette_colour()
    {
        using var harness = new TestHarness();
        var type = await harness.GivenTypeAsync("Grocery list", "Fresh produce", "Bread & bakery");

        var updated = await Handlers(harness).Handle(new AddCategoryCommand(type.Id, "Dairy"), default);

        updated.Categories.Select(c => c.Name).ShouldBe(["Fresh produce", "Bread & bakery", "Dairy"]);
        updated.Categories[2].PaletteIndex.ShouldBe(2);
        updated.Categories[2].Position.ShouldBe(2);
    }

    [Fact]
    public async Task Categories_come_back_in_their_custom_order()
    {
        using var harness = new TestHarness();
        var type = await harness.GivenTypeAsync("Grocery list", "Produce", "Bakery", "Dairy");
        var bakery = type.OrderedCategories[1];

        await Handlers(harness).Handle(new MoveCategoryCommand(type.Id, bakery.Id, Up: true), default);
        harness.Detach();

        var reread = await new ListTypeQueryHandlers(harness.Db, harness.CurrentUser)
            .Handle(new GetListTypeQuery(type.Id), default);

        reread.Categories.Select(c => c.Name).ShouldBe(["Bakery", "Produce", "Dairy"]);
    }

    [Fact]
    public async Task Renaming_a_category_keeps_its_place_and_its_colour()
    {
        using var harness = new TestHarness();
        var type = await harness.GivenTypeAsync("Grocery list", "Produce", "Bakery", "Dairy");
        var bakery = type.OrderedCategories[1];

        var updated = await Handlers(harness).Handle(
            new RenameCategoryCommand(type.Id, bakery.Id, "Bread & bakery"), default);

        // Renaming is not reordering: the row stays put, so no list of this type
        // re-groups behind the user's back.
        updated.Categories.Select(c => c.Name).ShouldBe(["Produce", "Bread & bakery", "Dairy"]);
        updated.Categories[1].PaletteIndex.ShouldBe(bakery.PaletteIndex);
        updated.Categories[1].Position.ShouldBe(1);
    }

    [Fact]
    public async Task A_category_cannot_be_renamed_onto_a_sibling_name()
    {
        using var harness = new TestHarness();
        var type = await harness.GivenTypeAsync("Grocery list", "Produce", "Bakery");

        await Should.ThrowAsync<DomainException>(() => Handlers(harness).Handle(
            new RenameCategoryCommand(type.Id, type.OrderedCategories[1].Id, "produce"), default));
    }

    [Fact]
    public async Task Moving_the_top_category_up_is_a_no_op_rather_than_an_error()
    {
        using var harness = new TestHarness();
        var type = await harness.GivenTypeAsync("Grocery list", "Produce", "Bakery");

        var result = await Handlers(harness).Handle(
            new MoveCategoryCommand(type.Id, type.OrderedCategories[0].Id, Up: true), default);

        result.Categories.Select(c => c.Name).ShouldBe(["Produce", "Bakery"]);
    }

    [Fact]
    public async Task Deleting_a_category_leaves_its_items_uncategorised()
    {
        using var harness = new TestHarness();
        var type = await harness.GivenTypeAsync("Reading list", "Non-fiction", "Fiction");
        var list = await harness.GivenListAsync("Someday", type);

        var fiction = type.OrderedCategories.First(c => c.Name == "Fiction");
        list.AddItem("Piranesi", fiction.Id, null, harness.UserId);
        await harness.Db.SaveChangesAsync();

        await Handlers(harness).Handle(new DeleteCategoryCommand(type.Id, fiction.Id), default);
        harness.Detach();

        // Cleared rather than shuffled into a category nobody chose.
        var item = await harness.Db.TodoItems.SingleAsync();
        item.CategoryId.ShouldBeNull();
    }

    [Fact]
    public async Task Deleting_the_only_category_is_allowed_and_empties_the_type()
    {
        using var harness = new TestHarness();
        var type = await harness.GivenTypeAsync("Reading list", "Fiction");
        var list = await harness.GivenListAsync("Someday", type);

        list.AddItem("Piranesi", type.OrderedCategories[0].Id, null, harness.UserId);
        await harness.Db.SaveChangesAsync();

        var updated = await Handlers(harness).Handle(
            new DeleteCategoryCommand(type.Id, type.OrderedCategories[0].Id), default);
        harness.Detach();

        updated.Categories.ShouldBeEmpty();
        (await harness.Db.TodoItems.SingleAsync()).CategoryId.ShouldBeNull();
    }

    [Fact]
    public async Task A_type_still_in_use_cannot_be_deleted()
    {
        using var harness = new TestHarness();
        var type = await harness.GivenTypeAsync("Grocery list", "Dairy");
        await harness.GivenListAsync("Groceries", type);

        var error = await Should.ThrowAsync<ConflictException>(() =>
            Handlers(harness).Handle(new DeleteListTypeCommand(type.Id), default));

        error.Message.ShouldContain("still used by 1 list");
    }

    [Fact]
    public async Task Another_accounts_type_is_not_found_rather_than_forbidden()
    {
        using var harness = new TestHarness();
        var mine = await harness.GivenTypeAsync("Grocery list", "Dairy");

        // A different acting user against the same store must not even learn it exists.
        using var other = new TestHarness();
        var handlers = new ListTypeCommandHandlers(harness.Db, other.CurrentUser);

        await Should.ThrowAsync<NotFoundException>(() =>
            handlers.Handle(new AddCategoryCommand(mine.Id, "Snacks"), default));
    }
}
