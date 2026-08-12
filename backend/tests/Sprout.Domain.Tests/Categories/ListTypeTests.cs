using Sprout.Domain.Categories;
using Sprout.Domain.Common;

namespace Sprout.Domain.Tests.Categories;

public class ListTypeTests
{
    private static readonly Guid Owner = Guid.CreateVersion7();

    [Fact]
    public void Create_seeds_a_single_catch_all_category()
    {
        var type = ListType.Create(Owner, "Reading list");

        var only = type.OrderedCategories.ShouldHaveSingleItem();
        only.Name.ShouldBe(Category.CatchAllName);
        only.IsCatchAll.ShouldBeTrue();
        only.Position.ShouldBe(0);
    }

    [Fact]
    public void Categories_take_the_next_palette_colour_and_cycle_after_six()
    {
        var type = ListType.CreateWithCategories(Owner, "Grocery list", null, "A", "B", "C", "D", "E", "F");
        type.AddCategory("G");

        // The seventh category wraps back to the first colour.
        type.OrderedCategories.Select(c => c.PaletteIndex).ShouldBe([0, 1, 2, 3, 4, 5, 0]);
        type.OrderedCategories[6].Swatch.Color.ShouldBe("#c67139");
    }

    [Fact]
    public void Category_names_are_unique_within_a_type_ignoring_case()
    {
        var type = ListType.CreateWithCategories(Owner, "Grocery list", null, "Dairy");

        Should.Throw<DomainException>(() => type.AddCategory("dairy"))
            .Message.ShouldContain("already a category");
    }

    [Fact]
    public void Category_names_may_repeat_across_different_types()
    {
        var grocery = ListType.CreateWithCategories(Owner, "Grocery list", null, "Dairy");
        var other = ListType.CreateWithCategories(Owner, "Default list", null, "Dairy");

        grocery.OrderedCategories[0].Name.ShouldBe("Dairy");
        other.OrderedCategories[0].Name.ShouldBe("Dairy");
    }

    [Fact]
    public void MoveCategoryUp_swaps_with_the_row_above_and_renumbers()
    {
        var type = ListType.CreateWithCategories(Owner, "Grocery list", null, "Produce", "Bakery", "Dairy");
        var bakery = type.OrderedCategories[1];

        type.MoveCategoryUp(bakery.Id);

        type.OrderedCategories.Select(c => c.Name).ShouldBe(["Bakery", "Produce", "Dairy"]);
        type.OrderedCategories.Select(c => c.Position).ShouldBe([0, 1, 2]);
    }

    [Fact]
    public void MoveCategoryDown_swaps_with_the_row_below()
    {
        var type = ListType.CreateWithCategories(Owner, "Grocery list", null, "Produce", "Bakery", "Dairy");

        type.MoveCategoryDown(type.OrderedCategories[0].Id);

        type.OrderedCategories.Select(c => c.Name).ShouldBe(["Bakery", "Produce", "Dairy"]);
    }

    [Fact]
    public void Moving_the_first_row_up_or_the_last_down_does_nothing()
    {
        var type = ListType.CreateWithCategories(Owner, "Grocery list", null, "Produce", "Bakery", "Dairy");

        type.MoveCategoryUp(type.OrderedCategories[0].Id);
        type.MoveCategoryDown(type.OrderedCategories[^1].Id);

        type.OrderedCategories.Select(c => c.Name).ShouldBe(["Produce", "Bakery", "Dairy"]);
    }

    [Fact]
    public void Palette_index_survives_a_reorder()
    {
        // Colour belongs to the category, not to its position: moving a row must not
        // repaint the list.
        var type = ListType.CreateWithCategories(Owner, "Grocery list", null, "Produce", "Bakery");
        var bakeryColour = type.OrderedCategories[1].Swatch.Color;

        type.MoveCategoryUp(type.OrderedCategories[1].Id);

        type.OrderedCategories[0].Name.ShouldBe("Bakery");
        type.OrderedCategories[0].Swatch.Color.ShouldBe(bakeryColour);
    }

    [Fact]
    public void RemoveCategory_renumbers_the_rest()
    {
        var type = ListType.CreateWithCategories(Owner, "Grocery list", null, "Produce", "Bakery", "Dairy");

        type.RemoveCategory(type.OrderedCategories[0].Id);

        type.OrderedCategories.Select(c => c.Name).ShouldBe(["Bakery", "Dairy"]);
        type.OrderedCategories.Select(c => c.Position).ShouldBe([0, 1]);
    }

    [Fact]
    public void The_last_category_cannot_be_removed()
    {
        var type = ListType.Create(Owner, "Reading list");

        Should.Throw<DomainException>(() => type.RemoveCategory(type.OrderedCategories[0].Id))
            .Message.ShouldContain("at least one category");
    }

    [Fact]
    public void FallbackCategoryFor_prefers_the_catch_all()
    {
        var type = ListType.CreateWithCategories(
            Owner, "Default list", null, "Errands", Category.CatchAllName, "House");

        var fallback = type.FallbackCategoryFor(type.OrderedCategories[0].Id);

        fallback.Name.ShouldBe(Category.CatchAllName);
    }

    [Fact]
    public void FallbackCategoryFor_falls_back_to_the_first_remaining_category()
    {
        var type = ListType.CreateWithCategories(Owner, "Default list", null, "Errands", "House", "Food");

        var fallback = type.FallbackCategoryFor(type.OrderedCategories[0].Id);

        fallback.Name.ShouldBe("House");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_type_needs_a_name(string name) =>
        Should.Throw<DomainException>(() => ListType.Create(Owner, name));

    [Fact]
    public void Names_are_trimmed()
    {
        var type = ListType.Create(Owner, "  Reading list  ");
        type.Name.ShouldBe("Reading list");
    }
}
