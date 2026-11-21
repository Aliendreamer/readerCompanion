namespace ReaderCompanion.UI.Library;

/// <summary>Shows a long list a page at a time: the first <see cref="PageSize"/> items, a page more on request. A
/// different list (a new preview, a new scan) starts again from the first page.</summary>
public sealed class Pager
{
    /// <summary>120 fills whole rows of 4, 5, 6, 8, 10 or 12 covers in the grid, and is quick to build.</summary>
    public const int PageSize = 120;

    private object? _list;

    public int Limit { get; private set; } = PageSize;

    /// <summary>The items to show now.</summary>
    public IReadOnlyList<T> Page<T>(IReadOnlyList<T> items)
    {
        if (!ReferenceEquals(items, _list))
        {
            _list = items;
            Limit = PageSize;
        }

        return items.Count <= Limit ? items : [.. items.Take(Limit)];
    }

    /// <summary>How many of <paramref name="count"/> items are shown.</summary>
    public int Shown(int count) => Math.Min(Limit, count);

    public void More() => Limit += PageSize;

    public void Reset() => Limit = PageSize;
}
