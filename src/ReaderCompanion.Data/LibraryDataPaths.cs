namespace ReaderCompanion.Data;

/// <summary>Where the library keeps its files: the database and, next to it, the covers folder.</summary>
internal sealed record LibraryDataPaths(string DatabasePath) : ReaderCompanion.Core.Library.ILibraryLocation
{
    public string DataFolder => Path.GetDirectoryName(Path.GetFullPath(DatabasePath))!;

    public string CoversFolder => Path.Combine(DataFolder, "covers");
}
