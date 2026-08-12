using Sprout.Domain.Categories;
using Sprout.Domain.Lists;

namespace Sprout.Domain.Tests.Lists;

/// <summary>
/// The four sorts from the design, and the grouping rule that makes "By category"
/// the type's own category order rather than anything the list decides.
/// </summary>
public class ItemOrderingTests
{
    private static readonly Guid Owner = Guid.CreateVersion7();
    private static readonly Guid Actor = Guid.CreateVersion7();

    private static (TodoList List, ListType Type) Grocery()
    {
        var type = ListType.CreateWithCategories(
            Owner, "Grocery list", null, "Fresh produce", "Bread & bakery", "Dairy", "Meat & fish", "Pantry");

        var list = TodoList.Create(Owner, "Groceries", type.Id);

        Guid Cat(string name) => type.OrderedCategories.First(c => c.Name == name).Id;

        // Added out of category order on purpose: insertion order and category order
        // must be distinguishable in the assertions below.
        list.AddItem("Olive oil", Cat("Pantry"), null, Actor);
        list.AddItem("Bananas", Cat("Fresh produce"), null, Actor);
        list.AddItem("Sourdough", Cat("Bread & bakery"), new DateOnly(2026, 8, 15), Actor);
        list.AddItem("Chicken thighs", Cat("Meat & fish"), null, Actor);
        list.AddItem("Rocket and tomatoes", Cat("Fresh produce"), new DateOnly(2026, 8, 13), Actor);

        return (list, type);
    }

    [Fact]
    public void MyOrder_is_insertion_order()
    {
        var (list, type) = Grocery();

        var sorted = ItemOrdering.Sort(list.Items, type, SortMode.MyOrder);

        sorted.Select(i => i.Text).ShouldBe(
            ["Olive oil", "Bananas", "Sourdough", "Chicken thighs", "Rocket and tomatoes"]);
    }

    [Fact]
    public void Alphabetical_is_case_insensitive_A_to_Z()
    {
        var (list, type) = Grocery();

        var sorted = ItemOrdering.Sort(list.Items, type, SortMode.Alphabetical);

        sorted.Select(i => i.Text).ShouldBe(
            ["Bananas", "Chicken thighs", "Olive oil", "Rocket and tomatoes", "Sourdough"]);
    }

    [Fact]
    public void DueDate_puts_dated_items_first_soonest_first_and_undated_after()
    {
        var (list, type) = Grocery();

        var sorted = ItemOrdering.Sort(list.Items, type, SortMode.DueDate);

        sorted.Take(2).Select(i => i.Text).ShouldBe(["Rocket and tomatoes", "Sourdough"]);
        sorted.Skip(2).ShouldAllBe(i => i.DueOn == null);
    }

    [Fact]
    public void DueDate_keeps_undated_items_in_insertion_order()
    {
        var (list, type) = Grocery();

        var sorted = ItemOrdering.Sort(list.Items, type, SortMode.DueDate);

        sorted.Skip(2).Select(i => i.Text).ShouldBe(["Olive oil", "Bananas", "Chicken thighs"]);
    }

    [Fact]
    public void Category_sort_follows_the_types_category_order_not_insertion_order()
    {
        var (list, type) = Grocery();

        var sorted = ItemOrdering.Sort(list.Items, type, SortMode.Category);

        sorted.Select(i => i.Text).ShouldBe(
        [
            "Bananas", "Rocket and tomatoes",  // Fresh produce, in insertion order
            "Sourdough",                        // Bread & bakery
            "Chicken thighs",                   // Meat & fish
            "Olive oil",                        // Pantry
        ]);
    }

    [Fact]
    public void Reordering_the_type_reorders_every_list_of_that_type()
    {
        var (list, type) = Grocery();
        var pantry = type.OrderedCategories.First(c => c.Name == "Pantry");

        type.MoveCategoryUp(pantry.Id);
        type.MoveCategoryUp(pantry.Id);
        type.MoveCategoryUp(pantry.Id);
        type.MoveCategoryUp(pantry.Id);

        var sorted = ItemOrdering.Sort(list.Items, type, SortMode.Category);

        sorted[0].Text.ShouldBe("Olive oil");
    }

    [Fact]
    public void Group_emits_no_header_for_a_category_with_no_items()
    {
        var (list, type) = Grocery();

        var groups = ItemOrdering.Group(list.Items, type);

        // Dairy has nothing on this list, so it gets no header.
        groups.Count.ShouldBe(4);
        groups.ShouldNotContain(g => g.Category!.Name == "Dairy");
    }

    [Fact]
    public void Group_counts_each_group()
    {
        var (list, type) = Grocery();

        var produce = ItemOrdering.Group(list.Items, type)[0];

        produce.Category!.Name.ShouldBe("Fresh produce");
        produce.Items.Count.ShouldBe(2);
    }

    [Fact]
    public void Items_whose_category_vanished_still_appear_in_a_headerless_group()
    {
        var (list, type) = Grocery();
        var pantry = type.OrderedCategories.First(c => c.Name == "Pantry");
        type.RemoveCategory(pantry.Id);

        var groups = ItemOrdering.Group(list.Items, type);

        var orphans = groups.ShouldHaveSingleItem(g => g.Category is null);
        orphans.Items.ShouldHaveSingleItem().Text.ShouldBe("Olive oil");
    }
}

file static class ShouldlyHelpers
{
    /// <summary>Shouldly has no single-item-matching-predicate overload; this reads better than Where().Single().</summary>
    public static T ShouldHaveSingleItem<T>(this IEnumerable<T> source, Func<T, bool> predicate) =>
        source.Where(predicate).ShouldHaveSingleItem();
}
