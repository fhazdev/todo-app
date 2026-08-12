using Microsoft.EntityFrameworkCore;
using Sprout.Application.Common.Exceptions;
using Sprout.Application.Features.ListTypes;
using Sprout.Domain.Categories;

namespace Sprout.Application.Tests.Features;

public class ListTypeCommandTests
{
    private static ListTypeCommandHandlers Handlers(TestHarness harness) =>
        new(harness.Db, harness.CurrentUser);

    [Fact]
    public async Task Creating_a_type_seeds_it_with_a_catch_all_category()
    {
        using var harness = new TestHarness();

        var type = await Handlers(harness).Handle(
            new CreateListTypeCommand("Reading list", "Books to get to"), default);

        type.Categories.ShouldHaveSingleItem().Name.ShouldBe(Category.CatchAllName);
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
    public async Task Moving_the_top_category_up_is_a_no_op_rather_than_an_error()
    {
        using var harness = new TestHarness();
        var type = await harness.GivenTypeAsync("Grocery list", "Produce", "Bakery");

        var result = await Handlers(harness).Handle(
            new MoveCategoryCommand(type.Id, type.OrderedCategories[0].Id, Up: true), default);

        result.Categories.Select(c => c.Name).ShouldBe(["Produce", "Bakery"]);
    }

    [Fact]
    public async Task Deleting_a_category_rehomes_its_items_to_the_catch_all()
    {
        using var harness = new TestHarness();
        var type = await harness.GivenTypeAsync("Reading list", Category.CatchAllName, "Fiction");
        var list = await harness.GivenListAsync("Someday", type);

        var fiction = type.OrderedCategories.First(c => c.Name == "Fiction");
        var catchAll = type.OrderedCategories.First(c => c.IsCatchAll);
        list.AddItem("Piranesi", fiction.Id, null, harness.UserId);
        await harness.Db.SaveChangesAsync();

        await Handlers(harness).Handle(new DeleteCategoryCommand(type.Id, fiction.Id), default);
        harness.Detach();

        var item = await harness.Db.TodoItems.SingleAsync();
        item.CategoryId.ShouldBe(catchAll.Id);
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
