using Sprout.Domain.Common;

namespace Sprout.Domain.Categories;

/// <summary>
/// One category belonging to a <see cref="ListType"/>. The category set and its
/// order live on the type, not on the list, so reordering here re-groups every
/// list of that type at once.
/// </summary>
public class Category : Entity
{
    private Category() { }

    internal Category(Guid listTypeId, string name, int paletteIndex, int position)
    {
        ListTypeId = listTypeId;
        Name = Normalise(name);
        PaletteIndex = CategoryPalette.Normalise(paletteIndex);
        Position = position;
    }

    public Guid ListTypeId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>Index into <see cref="CategoryPalette.Swatches"/>. Colours are derived, never stored.</summary>
    public int PaletteIndex { get; private set; }

    /// <summary>Zero-based position within the type. This <em>is</em> the custom sort order.</summary>
    public int Position { get; private set; }

    public ListType? ListType { get; private set; }

    public PaletteSwatch Swatch => CategoryPalette.At(PaletteIndex);

    internal void Rename(string name)
    {
        Name = Normalise(name);
        Touch();
    }

    internal void SetPosition(int position)
    {
        Position = position;
        Touch();
    }

    private static string Normalise(string name)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            throw new DomainException("A category needs a name.");
        }

        return trimmed.Length > 60
            ? throw new DomainException("A category name cannot be longer than 60 characters.")
            : trimmed;
    }
}
