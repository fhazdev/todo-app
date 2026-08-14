using Sprout.Domain.Common;

namespace Sprout.Domain.Lists;

/// <summary>
/// A single row on a list. Items toggle between open and completed and back;
/// completed items move to the collapsible section at the bottom of the list.
/// </summary>
public class TodoItem : Entity
{
    /// <summary>An item is for at least one of the thing. Zero would mean "delete it".</summary>
    public const int MinQuantity = 1;

    private TodoItem() { }

    internal TodoItem(Guid todoListId, string text, Guid? categoryId, DateOnly? dueOn, int position, Guid createdBy)
    {
        TodoListId = todoListId;
        Text = Normalise(text);
        CategoryId = categoryId;
        DueOn = dueOn;
        Position = position;
        CreatedBy = createdBy;
    }

    public Guid TodoListId { get; private set; }

    public string Text { get; private set; } = string.Empty;

    /// <summary>
    /// A category belonging to the list's type, or null when the item is not filed
    /// under one. Categories are optional, so null is an ordinary state rather than
    /// missing data.
    /// </summary>
    public Guid? CategoryId { get; private set; }

    public DateOnly? DueOn { get; private set; }

    /// <summary>How many of it. Never below <see cref="MinQuantity"/>.</summary>
    public int Quantity { get; private set; } = MinQuantity;

    public bool IsCompleted { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Who ticked it last. Shown nowhere yet, but shared lists want the audit.</summary>
    public Guid? CompletedBy { get; private set; }

    /// <summary>Insertion order within the list. Drives the "My order" sort.</summary>
    public int Position { get; private set; }

    public Guid CreatedBy { get; private set; }

    public TodoList? TodoList { get; private set; }

    /// <summary>Flips completion. Returns the new state so callers can respond without re-reading.</summary>
    public bool Toggle(Guid actingUserId) => SetCompleted(!IsCompleted, actingUserId);

    public bool SetCompleted(bool completed, Guid actingUserId)
    {
        if (IsCompleted == completed)
        {
            return IsCompleted;
        }

        IsCompleted = completed;
        CompletedAt = completed ? DateTimeOffset.UtcNow : null;
        CompletedBy = completed ? actingUserId : null;
        Touch();
        return IsCompleted;
    }

    public void Edit(string text, Guid? categoryId, DateOnly? dueOn)
    {
        Text = Normalise(text);
        CategoryId = categoryId;
        DueOn = dueOn;
        Touch();
    }

    /// <summary>
    /// Sets how many of it. The floor is enforced here rather than by the caller, so
    /// the stepper in the UI cannot be the only thing standing between the database
    /// and a quantity of zero.
    /// </summary>
    public void SetQuantity(int quantity)
    {
        if (quantity < MinQuantity)
        {
            throw new DomainException($"An item needs a quantity of at least {MinQuantity}.");
        }

        Quantity = quantity;
        Touch();
    }

    internal void MoveToCategory(Guid? categoryId)
    {
        CategoryId = categoryId;
        Touch();
    }

    internal void SetPosition(int position)
    {
        Position = position;
        Touch();
    }

    private static string Normalise(string text)
    {
        var trimmed = (text ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            throw new DomainException("An item needs some text.");
        }

        return trimmed.Length > 500
            ? throw new DomainException("An item cannot be longer than 500 characters.")
            : trimmed;
    }
}
