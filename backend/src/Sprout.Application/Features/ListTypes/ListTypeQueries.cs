using MediatR;
using Microsoft.EntityFrameworkCore;
using Sprout.Application.Common.Abstractions;
using Sprout.Application.Common.Contracts;
using Sprout.Application.Common.Exceptions;

namespace Sprout.Application.Features.ListTypes;

/// <summary>Every type the caller owns, with categories and a count of lists using each.</summary>
public sealed record GetListTypesQuery : IRequest<IReadOnlyList<ListTypeDto>>;

/// <summary>One type with its ordered categories, for the Type categories screen.</summary>
public sealed record GetListTypeQuery(Guid ListTypeId) : IRequest<ListTypeDto>;

public sealed class ListTypeQueryHandlers(IAppDbContext db, ICurrentUser currentUser) :
    IRequestHandler<GetListTypesQuery, IReadOnlyList<ListTypeDto>>,
    IRequestHandler<GetListTypeQuery, ListTypeDto>
{
    public async Task<IReadOnlyList<ListTypeDto>> Handle(GetListTypesQuery request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();

        var types = await db.ListTypes
            .Include(t => t.Categories)
            .Where(t => t.OwnerId == userId)
            // The default type leads, so "New list" opens on the catch-all kind
            // rather than on whichever type happened to be seeded first.
            .OrderByDescending(t => t.IsDefault)
            .ThenBy(t => t.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);

        // One grouped count rather than a query per type.
        var listCounts = await db.TodoLists
            .Where(l => l.OwnerId == userId)
            .GroupBy(l => l.ListTypeId)
            .Select(g => new { ListTypeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ListTypeId, x => x.Count, ct);

        return [.. types.Select(t => ListTypeDto.From(t, listCounts.GetValueOrDefault(t.Id)))];
    }

    public async Task<ListTypeDto> Handle(GetListTypeQuery request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();

        var type = await db.ListTypes
            .Include(t => t.Categories)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.ListTypeId && t.OwnerId == userId, ct)
            ?? throw new NotFoundException("That list type");

        var listCount = await db.TodoLists.CountAsync(l => l.ListTypeId == type.Id, ct);
        return ListTypeDto.From(type, listCount);
    }
}
