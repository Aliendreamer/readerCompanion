namespace ReaderCompanion.UI;

/// <summary>What the Library page shows in the status bar (its book count); null on other pages.</summary>
public sealed class LibraryStatus
{
    public string? Count { get; private set; }

    public event Action? Changed;

    public void Set(string? count)
    {
        if (Count != count)
        {
            Count = count;
            Changed?.Invoke();
        }
    }
}
