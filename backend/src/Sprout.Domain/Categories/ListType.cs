using Sprout.Domain.Common;

namespace Sprout.Domain.Categories;

/// <summary>
/// A kind of list ("Grocery list", "Movie &amp; show list") owned by one account.
/// The type owns an ordered set of categories; every item on a list of this type
/// takes one of them. Reordering the categories re-groups every such list.
/// </summary>
public class ListType : Entity
{
    private readonly List<Category> _categories = [];

    private ListType() { }

    private ListType(Guid ownerId, string name, string? blurb)
    {
        OwnerId = ownerId;
        Name = NormaliseName(name);
        Blurb = string.IsNullOrWhiteSpace(blurb) ? null : blurb.Trim();
    }

    public Guid OwnerId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>Short line under the type name on the types screen. Optional.</summary>
    public string? Blurb { get; private set; }

    /// <summary>
    /// The type offered first when creating a list: the catch-all kind, for a list
    /// that is not really a grocery run or a watchlist. At most one per account,
    /// which only the database can enforce since this aggregate cannot see its
    /// siblings; a partial unique index on the owner does it.
    /// </summary>
    public bool IsDefault { get; private set; }

    /// <summary>
    /// The backing collection, in whatever order it was loaded. Read
    /// <see cref="OrderedCategories"/> when the order matters, which is nearly always:
    /// this one exists so EF has a navigation it can populate.
    /// </summary>
    public IReadOnlyList<Category> Categories => _categories;

    /// <summary>The categories in their custom order. This order is the "By category" sort.</summary>
    public IReadOnlyList<Category> OrderedCategories => [.. _categories.OrderBy(c => c.Position)];

    /// <summary>
    /// Creates a type with no categories at all. Categories are optional: a list of
    /// this type is a plain checklist until someone adds one.
    /// </summary>
    public static ListType Create(Guid ownerId, string name, string? blurb = null) =>
        new(ownerId, name, blurb);

    /// <summary>Creates a type with an explicit ordered category set (used for seeding defaults).</summary>
    public static ListType CreateWithCategories(Guid ownerId, string name, string? blurb, params string[] categoryNames)
    {
        var type = new ListType(ownerId, name, blurb);
        foreach (var categoryName in categoryNames)
        {
            type.AddCategory(categoryName);
        }

        return type;
    }

    public void Rename(string name)
    {
        Name = NormaliseName(name);
        Touch();
    }

    /// <summary>
    /// Marks this as the account's default type, so the New list screen offers and
    /// preselects it. Used when seeding a new account.
    /// </summary>
    public ListType MarkAsDefault()
    {
        IsDefault = true;
        Touch();
        return this;
    }

    /// <summary>
    /// Appends a category. The palette index cycles with the current count, so the
    /// nth category of every type gets the nth palette colour.
    /// </summary>
    public Category AddCategory(string name)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (_categories.Any(c => string.Equals(c.Name, trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainException($"\"{trimmed}\" is already a category on {Name}.");
        }

        var category = new Category(Id, trimmed, CategoryPalette.NextIndex(_categories.Count), _categories.Count);
        _categories.Add(category);
        Touch();
        return category;
    }

    public void RenameCategory(Guid categoryId, string name)
    {
        var category = RequireCategory(categoryId);
        var trimmed = (name ?? string.Empty).Trim();
        if (_categories.Any(c => c.Id != categoryId && string.Equals(c.Name, trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainException($"\"{trimmed}\" is already a category on {Name}.");
        }

        category.Rename(trimmed);
        Touch();
    }

    /// <summary>
    /// Removes a category, including the last one: a type with none is a valid type.
    /// Callers must first clear any items that referenced it, which leaves them
    /// uncategorised.
    /// </summary>
    public void RemoveCategory(Guid categoryId)
    {
        var category = RequireCategory(categoryId);

        _categories.Remove(category);
        Renumber();
        Touch();
    }

    /// <summary>Moves a category one place towards the top. A no-op on the first row.</summary>
    public void MoveCategoryUp(Guid categoryId) => Move(categoryId, -1);

    /// <summary>Moves a category one place towards the bottom. A no-op on the last row.</summary>
    public void MoveCategoryDown(Guid categoryId) => Move(categoryId, +1);

    public Category? FindCategory(Guid categoryId) => _categories.FirstOrDefault(c => c.Id == categoryId);

    /// <summary>True when this type groups nothing, so its lists are plain checklists.</summary>
    public bool HasCategories => _categories.Count > 0;

    private void Move(Guid categoryId, int delta)
    {
        var ordered = _categories.OrderBy(c => c.Position).ToList();
        var index = ordered.FindIndex(c => c.Id == categoryId);
        if (index < 0)
        {
            throw new DomainException("That category is not on this type.");
        }

        var target = index + delta;
        if (target < 0 || target >= ordered.Count)
        {
            return; // Up on the first row and down on the last are no-ops by design.
        }

        (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i].SetPosition(i);
        }

        Touch();
    }

    private void Renumber()
    {
        var ordered = _categories.OrderBy(c => c.Position).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i].SetPosition(i);
        }
    }

    private Category RequireCategory(Guid categoryId) =>
        FindCategory(categoryId) ?? throw new DomainException("That category is not on this type.");

    private static string NormaliseName(string name)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            throw new DomainException("A list type needs a name.");
        }

        return trimmed.Length > 80
            ? throw new DomainException("A list type name cannot be longer than 80 characters.")
            : trimmed;
    }
}
