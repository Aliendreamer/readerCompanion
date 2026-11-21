namespace ReaderCompanion.UI.Library;

/// <summary>Which filter-tree sections are open, kept while the app runs (not saved). Every section starts closed.</summary>
public sealed class FilterTreeState
{
    private readonly Dictionary<string, bool> _open = [];

    public bool IsOpen(string id) => _open.GetValueOrDefault(id);

    public void Toggle(string id) => _open[id] = !IsOpen(id);
}
