namespace Sprout.Domain.Categories;

/// <summary>
/// One slot of the Organic category palette: the solid colour used for dots,
/// checkbox rings and type icons; the tint used as a chip background; and the
/// deep tone used for chip and header text.
/// </summary>
public readonly record struct PaletteSwatch(string Color, string Tint, string Deep);

/// <summary>
/// The six-colour category palette from the design system. New categories take
/// the next colour in the cycle: index = (count of existing categories) % 6.
/// </summary>
public static class CategoryPalette
{
    public static readonly IReadOnlyList<PaletteSwatch> Swatches =
    [
        new("#c67139", "#ffe1d0", "#8c491a"),
        new("#7a8a5e", "#e1eecc", "#56633f"),
        new("#b2622d", "#fff2eb", "#643312"),
        new("#82796a", "#eee7db", "#474238"),
        new("#f6a06b", "#fff2eb", "#8c491a"),
        new("#56633f", "#f0fae1", "#272e1b"),
    ];

    public static int Count => Swatches.Count;

    /// <summary>Wraps any integer into a valid palette index, including negatives.</summary>
    public static int Normalise(int paletteIndex) => ((paletteIndex % Count) + Count) % Count;

    public static PaletteSwatch At(int paletteIndex) => Swatches[Normalise(paletteIndex)];

    /// <summary>The palette index a category should take given how many already exist.</summary>
    public static int NextIndex(int existingCategoryCount) => Normalise(existingCategoryCount);
}
