using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sprout.Application.Common.Abstractions;
using Sprout.Domain.Categories;
using Sprout.Domain.Common;
using Sprout.Domain.Lists;
using Sprout.Infrastructure.Identity;

namespace Sprout.Infrastructure.Persistence;

/// <summary>
/// The single context: Sprout's own tables plus the ASP.NET Identity ones.
/// <para>
/// EF migrations are switched off for this project. Flyway owns the schema, so the
/// mappings here describe a database that already exists; every change is a new
/// versioned SQL file under <c>db/migrations</c>, never a scaffolded migration.
/// </para>
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, AppRole, Guid>(options), IAppDbContext
{
    public DbSet<ListType> ListTypes => Set<ListType>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<TodoList> TodoLists => Set<TodoList>();

    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    public DbSet<ListMember> ListMembers => Set<ListMember>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // The domain assigns its own UUIDv7 keys in the Entity constructor. Left to
        // itself EF treats a non-empty Guid key as an existing row, so an entity
        // discovered through a navigation is tracked as Modified and the insert never
        // happens. Saying so once here covers every aggregate.
        foreach (var entity in builder.Model.GetEntityTypes()
                     .Where(e => typeof(Entity).IsAssignableFrom(e.ClrType)))
        {
            builder.Entity(entity.ClrType).Property(nameof(Entity.Id)).ValueGeneratedNever();
        }

        // Applied last so it also catches the Identity tables, which we do not
        // configure by hand.
        builder.ApplySnakeCaseNames();
    }

    /// <summary>
    /// Stamps <see cref="Entity.UpdatedAt"/> on anything modified, so callers never
    /// have to remember to. Creation timestamps are set in the constructors.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Entity>().Where(e => e.State == EntityState.Modified))
        {
            entry.Entity.Touch();
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
