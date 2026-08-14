using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sprout.Domain.Categories;

namespace Sprout.Infrastructure.Persistence.Configurations;

public sealed class ListTypeConfiguration : IEntityTypeConfiguration<ListType>
{
    public void Configure(EntityTypeBuilder<ListType> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).HasMaxLength(80).IsRequired();
        builder.Property(t => t.Blurb).HasMaxLength(120);
        builder.Property(t => t.OwnerId).IsRequired();
        builder.Property(t => t.IsDefault).IsRequired();

        // Categories are only ever reached through their type. EF writes straight to
        // the backing field, so the public property can stay read-only.
        builder.HasMany(t => t.Categories)
            .WithOne(c => c.ListType!)
            .HasForeignKey(c => c.ListTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(t => t.Categories)
            .HasField("_categories")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(t => t.OrderedCategories);

        builder.HasIndex(t => t.OwnerId);

        // One type name per account, case-insensitive. Enforced in SQL as an index on
        // lower(name); EF only needs to know the column pair is unique.
        builder.HasIndex(t => new { t.OwnerId, t.Name }).IsUnique();
    }
}

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(60).IsRequired();
        builder.Property(c => c.PaletteIndex).IsRequired();
        builder.Property(c => c.Position).IsRequired();

        builder.HasIndex(c => new { c.ListTypeId, c.Position });
        builder.HasIndex(c => new { c.ListTypeId, c.Name }).IsUnique();

        builder.Ignore(c => c.Swatch);
    }
}
