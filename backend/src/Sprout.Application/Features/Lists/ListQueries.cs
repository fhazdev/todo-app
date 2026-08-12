using MediatR;
using Microsoft.EntityFrameworkCore;
using Sprout.Application.Common.Abstractions;
using Sprout.Application.Common.Contracts;
using Sprout.Application.Common.Services;
using Sprout.Domain.Categories;
using Sprout.Domain.Lists;

namespace Sprout.Application.Features.Lists;

/// <summary>Every list the caller owns or is a member of, as cards for the home screen.</summary>
public sealed record GetListsQuery : IRequest<IReadOnlyList<TodoListSummaryDto>>;

/// <summary>
/// One list with its type, categories, items, members and the caller's own sort
/// preference: everything the list detail screen renders, in one round trip.
/// </summary>
public sealed record GetListQuery(Guid ListId) : IRequest<TodoListDetailDto>;

public sealed class ListQueryHandlers(IAppDbContext db, ICurrentUser currentUser, ListAccess access) :
    IRequestHandler<GetListsQuery, IReadOnlyList<TodoListSummaryDto>>,
    IRequestHandler<GetListQuery, TodoListDetailDto>
{
    public async Task<IReadOnlyList<TodoListSummaryDto>> Handle(GetListsQuery request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();

        var lists = await db.TodoLists
            .Include(l => l.ListType!).ThenInclude(t => t.Categories)
            .Include(l => l.Items)
            .Include(l => l.Members)
            .Where(l => l.Members.Any(m => m.UserId == userId && m.Status == MembershipStatus.Active))
            .OrderByDescending(l => l.UpdatedAt)
            .AsNoTracking()
            .ToListAsync(ct);

        var cards = new List<TodoListSummaryDto>(lists.Count);
        foreach (var list in lists)
        {
            var type = list.ListType!;

            // The card's icon and type chip take the type's first category colours.
            var swatch = type.OrderedCategories.Count > 0
                ? type.OrderedCategories[0].Swatch
                : CategoryPalette.At(0);

            cards.Add(new TodoListSummaryDto(
                list.Id,
                list.Name,
                type.Id,
                type.Name,
                swatch.Color,
                swatch.Tint,
                swatch.Deep,
                list.OpenCount,
                list.SharedWithCount(userId),
                await access.ProjectMembersAsync(list, userId, ct)));
        }

        return cards;
    }

    public async Task<TodoListDetailDto> Handle(GetListQuery request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var list = await access.RequireMembershipAsync(request.ListId, userId, ct);

        var type = list.ListType!;
        var membership = list.MemberFor(userId)!;

        // Items go over the wire in the caller's chosen order, with completed ones
        // last, so the client renders straight down the array.
        var open = ItemOrdering.Sort(list.Items.Where(i => !i.IsCompleted), type, membership.Sort);
        var completed = list.Items
            .Where(i => i.IsCompleted)
            .OrderByDescending(i => i.CompletedAt)
            .ToList();

        return new TodoListDetailDto(
            list.Id,
            list.Name,
            ListTypeDto.From(type),
            membership.Sort.ToString(),
            membership.ShowCompleted,
            TodoList.IsPlain(list.Items, type),
            membership.Role.ToString(),
            [.. open.Concat(completed).Select(TodoItemDto.From)],
            await access.ProjectMembersAsync(list, userId, ct));
    }
}
