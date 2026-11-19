namespace ReaderCompanion.Core.Library;

public sealed class LibraryFolder
{
    public int Id { get; set; }

    public string Path { get; set; } = "";

    public DateTime AddedUtc { get; set; }
}
