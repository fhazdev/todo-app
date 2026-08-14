using Sprout.Domain.Categories;

namespace Sprout.Domain.Lists;

/// <summary>
/// The four sorts from the design, as pure functions over items. Grouping by
/// category is the only one that needs the type, because the type's category
/// order <em>is</em> the custom sort.
/// </summary>
public static class ItemOrdering
{
    /// <summary>
    /// Orders items for display. <see cref="SortMode.Category"/> falls back to
    /// insertion order here; use <see cref="Group"/> when you need the headers too.
    /// </summary>
    public static IReadOnlyList<TodoItem> Sort(IEnumerable<TodoItem> items, ListType type, SortMode sort)
    {
        var source = items.ToList();
        return sort switch
        {
            SortMode.Alphabetical => [.. source.OrderBy(i => i.Text, StringComparer.OrdinalIgnoreCase)],

            // Dated items first, soonest first; undated after, then insertion order
            // so the tail stays stable rather than shuffling on every read.
            SortMode.DueDate =>
            [
                .. source
                    .OrderBy(i => i.DueOn.HasValue ? 0 : 1)
                    .ThenBy(i => i.DueOn ?? DateOnly.MaxValue)
                    .ThenBy(i => i.Position)
            ],

            SortMode.Category => [.. FlattenByCategory(source, type)],

            _ => [.. source.OrderBy(i => i.Position)],
        };
    }

    /// <summary>
    /// Groups items in the type's category order. Only non-empty groups are returned,
    /// so a category with nothing in it renders no header. Within a group the items
    /// keep their insertion order.
    /// <para>
    /// Uncategorised items come last, in one headerless group. Items whose category
    /// was deleted out from under them land there too: to a reader they are the same
    /// thing, an item that belongs to no group on screen.
    /// </para>
    /// </summary>
    public static IReadOnlyList<CategoryGroup> Group(IEnumerable<TodoItem> items, ListType type)
    {
        var ordered = items.OrderBy(i => i.Position).ToList();

        // Uncategorised items are kept out of the lookup entirely: a dictionary
        // cannot take a null key, and they are not a group of their own anyway.
        var byCategory = ordered
            .Where(i => i.CategoryId is not null)
            .GroupBy(i => i.CategoryId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var groups = new List<CategoryGroup>();
        foreach (var category in type.OrderedCategories)
        {
            if (byCategory.TryGetValue(category.Id, out var group) && group.Count > 0)
            {
                groups.Add(new CategoryGroup(category, group));
            }
        }

        var known = type.Categories.Select(c => c.Id).ToHashSet();
        var loose = ordered.Where(i => i.CategoryId is not { } id || !known.Contains(id)).ToList();

        if (loose.Count > 0)
        {
            groups.Add(new CategoryGroup(null, loose));
        }

        return groups;
    }

    private static IEnumerable<TodoItem> FlattenByCategory(IEnumerable<TodoItem> items, ListType type) =>
        Group(items, type).SelectMany(g => g.Items);
}

/// <summary>
/// One category header plus the items under it. <paramref name="Category"/> is null
/// for the uncategorised group, which renders without a header.
/// </summary>
public readonly record struct CategoryGroup(Category? Category, IReadOnlyList<TodoItem> Items);
