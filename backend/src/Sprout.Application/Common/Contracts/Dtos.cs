using Sprout.Domain.Categories;
using Sprout.Domain.Lists;

namespace Sprout.Application.Common.Contracts;

/// <summary>
/// A category with its palette colours resolved. The client never derives colour
/// from an index; the server sends the three tones the design needs.
/// </summary>
public sealed record CategoryDto(
    Guid Id,
    string Name,
    int Position,
    int PaletteIndex,
    string Color,
    string Tint,
    string Deep)
{
    public static CategoryDto From(Category category)
    {
        var swatch = category.Swatch;
        return new CategoryDto(
            category.Id,
            category.Name,
            category.Position,
            category.PaletteIndex,
            swatch.Color,
            swatch.Tint,
            swatch.Deep);
    }
}

/// <summary>A list type with its ordered categories. <paramref name="ListCount"/> is how many lists use it.</summary>
public sealed record ListTypeDto(
    Guid Id,
    string Name,
    string? Blurb,
    IReadOnlyList<CategoryDto> Categories,
    int ListCount,
    bool IsDefault)
{
    public static ListTypeDto From(ListType type, int listCount = 0) =>
        new(
            type.Id,
            type.Name,
            type.Blurb,
            [.. type.OrderedCategories.Select(CategoryDto.From)],
            listCount,
            type.IsDefault);
}

/// <summary>One row on the Shared with screen.</summary>
public sealed record MemberDto(
    Guid Id,
    Guid? UserId,
    string DisplayName,
    string? Email,
    string Initials,
    string AvatarColor,
    string Role,
    string Status,
    bool IsYou);

/// <summary>One item row.</summary>
public sealed record TodoItemDto(
    Guid Id,
    string Text,
    Guid? CategoryId,
    DateOnly? DueOn,
    bool IsCompleted,
    int Position,
    int Quantity)
{
    public static TodoItemDto From(TodoItem item) =>
        new(
            item.Id,
            item.Text,
            item.CategoryId,
            item.DueOn,
            item.IsCompleted,
            item.Position,
            item.Quantity);
}

/// <summary>
/// A card on the Lists home screen. TypeColor/Tint/Deep are the type's first
/// category colours, which fill the list icon and the type chip.
/// </summary>
public sealed record TodoListSummaryDto(
    Guid Id,
    string Name,
    Guid ListTypeId,
    string TypeName,
    string TypeColor,
    string TypeTint,
    string TypeDeep,
    int OpenCount,
    int SharedWithCount,
    IReadOnlyList<MemberDto> Members);

/// <summary>
/// Everything the list detail screen needs in one round trip. IsPlain is true when
/// the list should render with no category chrome at all (the uncategorised rule).
/// </summary>
public sealed record TodoListDetailDto(
    Guid Id,
    string Name,
    ListTypeDto Type,
    string Sort,
    bool ShowCompleted,
    bool IsPlain,
    string MyRole,
    IReadOnlyList<TodoItemDto> Items,
    IReadOnlyList<MemberDto> Members)
{
    public int OpenCount => Items.Count(i => !i.IsCompleted);

    public int CompletedCount => Items.Count(i => i.IsCompleted);
}

/// <summary>The signed-in account, as returned by /auth endpoints.</summary>
public sealed record UserDto(Guid Id, string Email, string DisplayName, string Initials, string AvatarColor);

/// <summary>A session: the account plus the tokens the client stores.</summary>
public sealed record AuthResultDto(
    UserDto User,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken);
