using Sprout.Domain.Common;
using Sprout.Domain.Lists;

namespace Sprout.Domain.Tests.Lists;

/// <summary>
/// How many of an item. The floor lives in the entity rather than in the stepper
/// that drives it, so nothing reachable can put a zero on a list.
/// </summary>
public class TodoItemQuantityTests
{
    private static readonly Guid Owner = Guid.CreateVersion7();

    private static TodoItem NewItem() =>
        TodoList.Create(Owner, "Groceries", Guid.CreateVersion7())
            .AddItem("Lettuce", null, null, Owner);

    [Fact]
    public void An_item_starts_at_one()
    {
        NewItem().Quantity.ShouldBe(1);
        TodoItem.MinQuantity.ShouldBe(1);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(12)]
    [InlineData(999)]
    public void A_quantity_at_or_above_the_floor_is_kept(int quantity)
    {
        var item = NewItem();

        item.SetQuantity(quantity);

        item.Quantity.ShouldBe(quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void A_quantity_below_the_floor_is_refused(int quantity)
    {
        var item = NewItem();

        Should.Throw<DomainException>(() => item.SetQuantity(quantity))
            .Message.ShouldContain("at least 1");
    }

    [Fact]
    public void A_refused_quantity_leaves_the_previous_one_alone()
    {
        var item = NewItem();
        item.SetQuantity(4);

        Should.Throw<DomainException>(() => item.SetQuantity(0));

        // The guard runs before the assignment, so a rejected change is not a
        // half-applied one.
        item.Quantity.ShouldBe(4);
    }

    [Fact]
    public void Setting_the_quantity_stamps_the_item_as_changed()
    {
        var item = NewItem();
        var before = item.UpdatedAt;

        item.SetQuantity(3);

        // Shared lists lean on UpdatedAt to know something moved.
        item.UpdatedAt.ShouldBeGreaterThan(before);
    }

    [Fact]
    public void Quantity_survives_completing_and_reopening()
    {
        var item = NewItem();
        item.SetQuantity(6);

        item.Toggle(Owner);
        item.Toggle(Owner);

        item.Quantity.ShouldBe(6);
    }
}
