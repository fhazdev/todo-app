namespace Sprout.Domain.Lists;

/// <summary>Membership role on a shared list. Owner is unique per list.</summary>
public enum ListRole
{
    /// <summary>Created the list. Can rename, delete, invite and remove members.</summary>
    Owner = 0,

    /// <summary>Can read and change items, but not the membership or the list itself.</summary>
    Editor = 1,
}

/// <summary>Whether a membership row is a live member or an unaccepted invitation.</summary>
public enum MembershipStatus
{
    /// <summary>Invited by email; no account has claimed the invitation yet.</summary>
    Invited = 0,

    /// <summary>Accepted; <see cref="ListMember.UserId"/> is populated.</summary>
    Active = 1,
}

/// <summary>How a member has chosen to sort one list. Persisted per member per list.</summary>
public enum SortMode
{
    /// <summary>Grouped by the type's category order, headers per non-empty group.</summary>
    Category = 0,

    /// <summary>Insertion order.</summary>
    MyOrder = 1,

    /// <summary>Dated items first, soonest first; undated after.</summary>
    DueDate = 2,

    /// <summary>A to Z by title, case-insensitive.</summary>
    Alphabetical = 3,
}
