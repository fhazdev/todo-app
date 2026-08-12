using Sprout.Domain.Common;

namespace Sprout.Domain.Lists;

/// <summary>
/// One person's relationship to a list: owner or editor, active or still invited.
/// An invitation is created by email and carries no <see cref="UserId"/> until the
/// invitee signs in and claims it.
/// </summary>
public class ListMember : Entity
{
    private ListMember() { }

    internal ListMember(Guid todoListId, Guid? userId, string? invitedEmail, ListRole role, MembershipStatus status)
    {
        TodoListId = todoListId;
        UserId = userId;
        InvitedEmail = invitedEmail?.Trim().ToLowerInvariant();
        Role = role;
        Status = status;
    }

    public Guid TodoListId { get; private set; }

    /// <summary>Null while the membership is a pending invitation.</summary>
    public Guid? UserId { get; private set; }

    /// <summary>Lower-cased email the invitation was addressed to. Kept after acceptance for display.</summary>
    public string? InvitedEmail { get; private set; }

    public ListRole Role { get; private set; }

    public MembershipStatus Status { get; private set; }

    /// <summary>This member's sort choice for this list. Persisted per member, per list.</summary>
    public SortMode Sort { get; private set; } = SortMode.Category;

    /// <summary>Whether this member has the completed section expanded on this list.</summary>
    public bool ShowCompleted { get; private set; }

    public TodoList? TodoList { get; private set; }

    public bool IsOwner => Role == ListRole.Owner;

    public bool CanEdit => Status == MembershipStatus.Active;

    /// <summary>Binds a pending invitation to the account that accepted it.</summary>
    public void Accept(Guid userId)
    {
        if (Status == MembershipStatus.Active)
        {
            return;
        }

        UserId = userId;
        Status = MembershipStatus.Active;
        Touch();
    }

    public void SetSort(SortMode sort)
    {
        Sort = sort;
        Touch();
    }

    public void SetShowCompleted(bool showCompleted)
    {
        ShowCompleted = showCompleted;
        Touch();
    }

    internal void SetRole(ListRole role)
    {
        if (Role == ListRole.Owner && role != ListRole.Owner)
        {
            throw new DomainException("Transfer ownership before changing the owner's role.");
        }

        Role = role;
        Touch();
    }
}
