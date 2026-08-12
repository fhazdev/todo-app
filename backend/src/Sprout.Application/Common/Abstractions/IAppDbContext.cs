using Microsoft.EntityFrameworkCore;
using Sprout.Domain.Categories;
using Sprout.Domain.Lists;

namespace Sprout.Application.Common.Abstractions;

/// <summary>
/// The persistence surface handlers are allowed to see. The concrete DbContext,
/// its provider and its Identity tables all stay in Infrastructure.
/// </summary>
public interface IAppDbContext
{
    DbSet<ListType> ListTypes { get; }

    DbSet<Category> Categories { get; }

    DbSet<TodoList> TodoLists { get; }

    DbSet<TodoItem> TodoItems { get; }

    DbSet<ListMember> ListMembers { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
