using Sprout.Domain.Categories;
using Sprout.Domain.Common;

namespace Sprout.Domain.Lists;

/// <summary>
/// A list of items, of exactly one <see cref="ListType"/>, shared with zero or more
/// people. The type decides which categories its items can take; the list never
/// overrides them.
/// </summary>
public class TodoList : Entity
{
    private readonly List<TodoItem> _items = [];
    private readonly List<ListMember> _members = [];

    private TodoList() { }

    private TodoList(Guid ownerId, string name, Guid listTypeId)
    {
        OwnerId = ownerId;
        Name = NormaliseName(name);
        ListTypeId = listTypeId;
    }

    public Guid OwnerId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public Guid ListTypeId { get; private set; }

    public ListType? ListType { get; private set; }

    public IReadOnlyList<TodoItem> Items => _items;

    public IReadOnlyList<ListMember> Members => _members;

    public int OpenCount => _items.Count(i => !i.IsCompleted);

    public int CompletedCount => _items.Count(i => i.IsCompleted);

    /// <summary>Creates a list and its owner membership in one step.</summary>
    public static TodoList Create(Guid ownerId, string name, Guid listTypeId)
    {
        var list = new TodoList(ownerId, name, listTypeId);
        list._members.Add(new ListMember(list.Id, ownerId, null, ListRole.Owner, MembershipStatus.Active));
        return list;
    }

    public void Rename(string name)
    {
        Name = NormaliseName(name);
        Touch();
    }

    /// <summary>
    /// Appends an open item. New items always land at the end of "My order" and
    /// inside their category group.
    /// </summary>
    public TodoItem AddItem(string text, Guid? categoryId, DateOnly? dueOn, Guid createdBy)
    {
        var position = _items.Count == 0 ? 0 : _items.Max(i => i.Position) + 1;
        var item = new TodoItem(Id, text, categoryId, dueOn, position, createdBy);
        _items.Add(item);
        Touch();
        return item;
    }

    public TodoItem RequireItem(Guid itemId) =>
        _items.FirstOrDefault(i => i.Id == itemId)
        ?? throw new DomainException("That item is not on this list.");

    public void RemoveItem(Guid itemId)
    {
        _items.Remove(RequireItem(itemId));
        Touch();
    }

    /// <summary>Adds an active member, or returns the existing membership if there is one.</summary>
    public ListMember AddMember(Guid userId, ListRole role = ListRole.Editor)
    {
        var existing = _members.FirstOrDefault(m => m.UserId == userId);
        if (existing is not null)
        {
            return existing;
        }

        var member = new ListMember(Id, userId, null, role, MembershipStatus.Active);
        _members.Add(member);
        Touch();
        return member;
    }

    /// <summary>
    /// Records an invitation by email. Invitations stay pending until the invitee
    /// signs in with that address and claims them.
    /// </summary>
    public ListMember Invite(string email, ListRole role = ListRole.Editor)
    {
        var normalised = (email ?? string.Empty).Trim().ToLowerInvariant();
        if (normalised.Length == 0)
        {
            throw new DomainException("An invitation needs an email address.");
        }

        var existing = _members.FirstOrDefault(m => m.InvitedEmail == normalised);
        if (existing is not null)
        {
            throw new DomainException($"{normalised} is already on this list.");
        }

        var member = new ListMember(Id, null, normalised, role, MembershipStatus.Invited);
        _members.Add(member);
        Touch();
        return member;
    }

    public void RemoveMember(Guid memberId)
    {
        var member = _members.FirstOrDefault(m => m.Id == memberId)
            ?? throw new DomainException("That person is not on this list.");

        if (member.IsOwner)
        {
            throw new DomainException("The owner cannot be removed from their own list.");
        }

        _members.Remove(member);
        Touch();
    }

    public ListMember? MemberFor(Guid userId) => _members.FirstOrDefault(m => m.UserId == userId);

    public bool HasMember(Guid userId) =>
        _members.Any(m => m.UserId == userId && m.Status == MembershipStatus.Active);

    /// <summary>Number of people other than the acting user who share this list.</summary>
    public int SharedWithCount(Guid actingUserId) => _members.Count(m => m.UserId != actingUserId);

    /// <summary>
    /// Moves every item out of a category that is about to be deleted. Called before
    /// <see cref="ListType.RemoveCategory"/> so no item is left pointing at nothing.
    /// </summary>
    public void ReassignCategory(Guid fromCategoryId, Guid? toCategoryId)
    {
        foreach (var item in _items.Where(i => i.CategoryId == fromCategoryId))
        {
            item.MoveToCategory(toCategoryId);
        }

        Touch();
    }

    /// <summary>
    /// The "uncategorised" rule from the handoff: a list shows no category chrome at
    /// all when nothing on it sits in a category the type still has. That covers an
    /// empty list, a list whose items are all uncategorised, and one whose categories
    /// were deleted out from under it. A single filed item brings the grouping back.
    /// </summary>
    public static bool IsPlain(IEnumerable<TodoItem> items, ListType type) =>
        !items.Any(i => i.CategoryId is { } id && type.FindCategory(id) is not null);

    private static string NormaliseName(string name)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return "Untitled list"; // The design falls back rather than rejecting.
        }

        return trimmed.Length > 120
            ? throw new DomainException("A list name cannot be longer than 120 characters.")
            : trimmed;
    }
}
