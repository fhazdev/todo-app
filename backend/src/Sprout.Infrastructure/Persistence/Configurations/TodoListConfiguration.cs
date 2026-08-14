using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sprout.Domain.Categories;
using Sprout.Domain.Lists;

namespace Sprout.Infrastructure.Persistence.Configurations;

public sealed class TodoListConfiguration : IEntityTypeConfiguration<TodoList>
{
    public void Configure(EntityTypeBuilder<TodoList> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name).HasMaxLength(120).IsRequired();
        builder.Property(l => l.OwnerId).IsRequired();

        builder.HasOne(l => l.ListType)
            .WithMany()
            .HasForeignKey(l => l.ListTypeId)
            .OnDelete(DeleteBehavior.Restrict); // A type in use cannot be deleted.

        builder.HasMany(l => l.Items)
            .WithOne(i => i.TodoList!)
            .HasForeignKey(i => i.TodoListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(l => l.Members)
            .WithOne(m => m.TodoList!)
            .HasForeignKey(m => m.TodoListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(l => l.Items)
            .HasField("_items")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(l => l.Members)
            .HasField("_members")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(l => l.OwnerId);
        builder.HasIndex(l => l.ListTypeId);

        builder.Ignore(l => l.OpenCount);
        builder.Ignore(l => l.CompletedCount);
    }
}

public sealed class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Text).HasMaxLength(500).IsRequired();
        // Nullable: categories are optional, so an uncategorised item is normal.
        builder.Property(i => i.CategoryId);
        builder.Property(i => i.Position).IsRequired();
        builder.Property(i => i.Quantity).IsRequired();

        // No HasDefaultValue anywhere in these configurations: that marks a property
        // store-generated, and EF would then omit false from the INSERT and read it
        // back. Flyway declares the column defaults; EF always sends the real value.

        // Declared as a relationship with no navigation on either side. The domain
        // never walks from item to category, so neither entity gains a property, but
        // EF has to know the dependency exists: without it, it cannot order a batch
        // that clears items and deletes their category, and Postgres rejects the
        // delete on fk_todo_items_categories_category_id. Restrict matches the SQL,
        // which is what forces the delete path to clear items first.
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.CategoryId);
        builder.HasIndex(i => new { i.TodoListId, i.Position });
        builder.HasIndex(i => new { i.TodoListId, i.IsCompleted });
    }
}

public sealed class ListMemberConfiguration : IEntityTypeConfiguration<ListMember>
{
    public void Configure(EntityTypeBuilder<ListMember> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.InvitedEmail).HasMaxLength(256);
        builder.Property(m => m.Role).HasConversion<int>();
        builder.Property(m => m.Status).HasConversion<int>();
        builder.Property(m => m.Sort).HasConversion<int>();

        // A person appears on a list once, and an invited address appears once.
        // Both are partial unique indexes in SQL, since UserId and InvitedEmail
        // are each null for half the rows.
        builder.HasIndex(m => new { m.TodoListId, m.UserId });
        builder.HasIndex(m => new { m.TodoListId, m.InvitedEmail });
        builder.HasIndex(m => m.UserId);

        builder.Ignore(m => m.IsOwner);
        builder.Ignore(m => m.CanEdit);
    }
}
