namespace ReaderCompanion.Core.Library;

/// <summary>Two books the user said are not duplicates; <see cref="BookIdA"/> is the smaller id.</summary>
public sealed class DismissedDuplicate
{
    public int BookIdA { get; set; }

    public int BookIdB { get; set; }
}
