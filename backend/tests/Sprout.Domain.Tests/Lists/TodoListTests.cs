using Sprout.Domain.Categories;
using Sprout.Domain.Common;
using Sprout.Domain.Lists;

namespace Sprout.Domain.Tests.Lists;

public class TodoListTests
{
    private static readonly Guid Owner = Guid.CreateVersion7();
    private static readonly Guid Friend = Guid.CreateVersion7();

    private static ListType PlainType() =>
        ListType.CreateWithCategories(Owner, "Default list", null, "Errands", "House");

    [Fact]
    public void Create_makes_the_creator_the_owner_and_an_active_member()
    {
        var list = TodoList.Create(Owner, "Weekend at the cabin", Guid.CreateVersion7());

        var member = list.Members.ShouldHaveSingleItem();
        member.UserId.ShouldBe(Owner);
        member.Role.ShouldBe(ListRole.Owner);
        member.Status.ShouldBe(MembershipStatus.Active);
        list.HasMember(Owner).ShouldBeTrue();
    }

    [Fact]
    public void An_empty_name_falls_back_to_Untitled_list()
    {
        // The design falls back rather than blocking the Create button.
        TodoList.Create(Owner, "   ", Guid.CreateVersion7()).Name.ShouldBe("Untitled list");
    }

    [Fact]
    public void Items_are_appended_so_they_land_at_the_end_of_My_order()
    {
        var type = PlainType();
        var list = TodoList.Create(Owner, "Cabin", type.Id);
        var errands = type.OrderedCategories[0].Id;

        list.AddItem("Book the ferry", errands, null, Owner);
        var second = list.AddItem("Pack the cooler", errands, null, Owner);

        second.Position.ShouldBe(1);
        list.Items.OrderBy(i => i.Position).Last().Text.ShouldBe("Pack the cooler");
    }

    [Fact]
    public void Toggling_moves_an_item_between_open_and_completed_both_ways()
    {
        var type = PlainType();
        var list = TodoList.Create(Owner, "Cabin", type.Id);
        var item = list.AddItem("Charge the camera", type.OrderedCategories[0].Id, null, Owner);

        item.Toggle(Owner).ShouldBeTrue();
        item.CompletedAt.ShouldNotBeNull();
        item.CompletedBy.ShouldBe(Owner);
        list.OpenCount.ShouldBe(0);
        list.CompletedCount.ShouldBe(1);

        item.Toggle(Friend).ShouldBeFalse();
        item.CompletedAt.ShouldBeNull();
        item.CompletedBy.ShouldBeNull();
        list.OpenCount.ShouldBe(1);
    }

    [Fact]
    public void Invite_records_a_pending_membership_with_no_account()
    {
        var list = TodoList.Create(Owner, "Cabin", Guid.CreateVersion7());

        var invited = list.Invite("Sam.Oyelaran@Gmail.com");

        invited.InvitedEmail.ShouldBe("sam.oyelaran@gmail.com"); // normalised
        invited.UserId.ShouldBeNull();
        invited.Status.ShouldBe(MembershipStatus.Invited);
        list.HasMember(Owner).ShouldBeTrue();
    }

    [Fact]
    public void Accepting_an_invitation_binds_it_to_the_account()
    {
        var list = TodoList.Create(Owner, "Cabin", Guid.CreateVersion7());
        var invited = list.Invite("nina@example.com");

        invited.Accept(Friend);

        invited.UserId.ShouldBe(Friend);
        invited.Status.ShouldBe(MembershipStatus.Active);
        list.HasMember(Friend).ShouldBeTrue();
    }

    [Fact]
    public void The_same_address_cannot_be_invited_twice()
    {
        var list = TodoList.Create(Owner, "Cabin", Guid.CreateVersion7());
        list.Invite("nina@example.com");

        Should.Throw<DomainException>(() => list.Invite("NINA@example.com"));
    }

    [Fact]
    public void The_owner_cannot_be_removed()
    {
        var list = TodoList.Create(Owner, "Cabin", Guid.CreateVersion7());
        var ownerMembership = list.Members.Single();

        Should.Throw<DomainException>(() => list.RemoveMember(ownerMembership.Id))
            .Message.ShouldContain("owner cannot be removed");
    }

    [Fact]
    public void SharedWithCount_excludes_the_person_asking()
    {
        var list = TodoList.Create(Owner, "Cabin", Guid.CreateVersion7());
        list.AddMember(Friend);
        list.Invite("sam@example.com");

        list.SharedWithCount(Owner).ShouldBe(2);
        list.SharedWithCount(Friend).ShouldBe(2);
    }

    // ── The uncategorised rule ────────────────────────────────────────────────

    [Fact]
    public void An_empty_list_is_plain()
    {
        var type = PlainType();
        TodoList.IsPlain([], type).ShouldBeTrue();
    }

    [Fact]
    public void A_list_whose_items_all_sit_in_the_catch_all_is_plain()
    {
        var type = ListType.Create(Owner, "Reading list"); // seeded with Uncategorised
        var list = TodoList.Create(Owner, "Someday", type.Id);
        var catchAll = type.OrderedCategories[0].Id;

        list.AddItem("Piranesi", catchAll, null, Owner);
        list.AddItem("The Dispossessed", catchAll, null, Owner);

        TodoList.IsPlain(list.Items, type).ShouldBeTrue();
    }

    [Fact]
    public void One_real_category_brings_the_grouping_back()
    {
        var type = ListType.Create(Owner, "Reading list");
        var catchAll = type.OrderedCategories[0].Id;
        var fiction = type.AddCategory("Fiction");

        var list = TodoList.Create(Owner, "Someday", type.Id);
        list.AddItem("Piranesi", catchAll, null, Owner);

        TodoList.IsPlain(list.Items, type).ShouldBeTrue();

        list.AddItem("The Dispossessed", fiction.Id, null, Owner);

        TodoList.IsPlain(list.Items, type).ShouldBeFalse();
    }

    [Fact]
    public void A_list_entirely_inside_one_real_category_is_not_plain()
    {
        // Every item in "Errands" is still a categorised list: the rule is about the
        // catch-all specifically, not about there being only one group.
        var type = PlainType();
        var list = TodoList.Create(Owner, "Cabin", type.Id);
        list.AddItem("Book the ferry", type.OrderedCategories[0].Id, null, Owner);

        TodoList.IsPlain(list.Items, type).ShouldBeFalse();
    }

    [Fact]
    public void ReassignCategory_moves_every_item_that_used_the_old_one()
    {
        var type = PlainType();
        var list = TodoList.Create(Owner, "Cabin", type.Id);
        var errands = type.OrderedCategories[0].Id;
        var house = type.OrderedCategories[1].Id;

        list.AddItem("Book the ferry", errands, null, Owner);
        list.AddItem("Cash for the marina", errands, null, Owner);
        list.AddItem("Split the firewood", house, null, Owner);

        list.ReassignCategory(errands, house);

        list.Items.ShouldAllBe(i => i.CategoryId == house);
    }
}
