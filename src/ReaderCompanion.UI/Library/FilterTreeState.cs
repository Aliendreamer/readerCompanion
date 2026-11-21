namespace ReaderCompanion.UI.Library;

/// <summary>Which filter-tree sections are open, kept while the app runs (not saved). Long sections start closed.</summary>
public sealed class FilterTreeState
{
    private static readonly HashSet<string> ClosedAtFirst = ["filter-author", "filter-collection"];
    private readonly Dictionary<string, bool> _open = [];

    public bool IsOpen(string id) => _open.TryGetValue(id, out var open) ? open : !ClosedAtFirst.Contains(id);

    public void Toggle(string id) => _open[id] = !IsOpen(id);
}
