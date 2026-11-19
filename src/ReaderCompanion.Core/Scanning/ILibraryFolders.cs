namespace ReaderCompanion.Core.Scanning;

/// <summary>Outcome of linking a folder. When refused, <see cref="RefusedReason"/> says why.</summary>
public sealed record LinkFolderResult(bool Linked, string? RefusedReason = null)
{
    /// <summary>Folders that were inside the new one and are now covered by it.</summary>
    public IReadOnlyList<string> Replaced { get; init; } = [];
}

public interface ILibraryFolders
{
    Task<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default);

    Task<LinkFolderResult> LinkAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>Unlinks the folder; its books stay in the library with their files marked missing.</summary>
    Task UnlinkAsync(string path, CancellationToken cancellationToken = default);
}
