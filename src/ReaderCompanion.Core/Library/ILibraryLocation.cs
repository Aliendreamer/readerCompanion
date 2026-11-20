namespace ReaderCompanion.Core.Library;

/// <summary>Where the library keeps its data: the database and the covers folder.</summary>
public interface ILibraryLocation
{
    string DataFolder { get; }

    /// <summary>The library database file, inside <see cref="DataFolder"/>.</summary>
    string DatabasePath { get; }
}
