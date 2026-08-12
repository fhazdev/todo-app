using Sprout.Domain.Lists;

namespace Sprout.Api.Contracts;

// HTTP request bodies. They stay separate from the MediatR commands so a route can
// take its id from the path while the command carries it in one place.

public sealed record RegisterRequest(string Email, string Password, string? DisplayName);

public sealed record LoginRequest(string Email, string Password);

public sealed record GoogleLoginRequest(string IdToken);

public sealed record RefreshRequest(string RefreshToken);

public sealed record CreateListTypeRequest(string Name, string? Blurb);

public sealed record RenameListTypeRequest(string Name);

public sealed record CategoryNameRequest(string Name);

/// <summary>Direction for a category reorder. "up" or "down"; anything else is rejected.</summary>
public sealed record MoveCategoryRequest(string Direction)
{
    public bool IsUp => string.Equals(Direction, "up", StringComparison.OrdinalIgnoreCase);

    public bool IsValid =>
        IsUp || string.Equals(Direction, "down", StringComparison.OrdinalIgnoreCase);
}

public sealed record CreateListRequest(string Name, Guid ListTypeId);

public sealed record RenameListRequest(string Name);

public sealed record SetSortRequest(SortMode Sort);

public sealed record SetShowCompletedRequest(bool ShowCompleted);

public sealed record AddItemRequest(string Text, Guid? CategoryId, DateOnly? DueOn);

public sealed record UpdateItemRequest(string Text, Guid CategoryId, DateOnly? DueOn);

public sealed record InviteMemberRequest(string Email);
