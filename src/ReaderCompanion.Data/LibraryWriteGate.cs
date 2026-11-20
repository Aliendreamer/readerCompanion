namespace ReaderCompanion.Data;

internal enum LibraryWork
{
    None,
    Scan,
    Organise,
    Restore,
}

/// <summary>Lets one kind of work change files and the catalog at a time: a scan or an organise, never both.</summary>
internal sealed class LibraryWriteGate
{
    private int _holder;

    public LibraryWork Holder => (LibraryWork)Volatile.Read(ref _holder);

    public bool TryEnter(LibraryWork work) => Interlocked.CompareExchange(ref _holder, (int)work, (int)LibraryWork.None) == (int)LibraryWork.None;

    public void Exit() => Volatile.Write(ref _holder, (int)LibraryWork.None);

    /// <summary>Why the library cannot be changed right now, for the user.</summary>
    public string BusyMessage => Holder switch
    {
        LibraryWork.Scan => "A scan is running; try again when it has finished.",
        LibraryWork.Organise => "Files are being organised; try again when that is done.",
        LibraryWork.Restore => "A backup is being restored; try again when that is done.",
        _ => "The library is busy; try again in a moment.",
    };
}
