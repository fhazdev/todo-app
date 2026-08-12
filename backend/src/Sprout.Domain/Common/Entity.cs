namespace Sprout.Domain.Common;

/// <summary>
/// Base for every persisted aggregate. Ids are client-generatable Guids so a
/// handler can build a whole object graph before it ever touches the database.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.CreateVersion7();

    public DateTimeOffset CreatedAt { get; protected set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; protected set; } = DateTimeOffset.UtcNow;

    public void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
