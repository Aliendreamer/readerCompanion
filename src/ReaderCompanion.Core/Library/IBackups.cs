namespace ReaderCompanion.Core.Library;

/// <summary>One backup file. <see cref="BeforeRestore"/> marks the copy taken automatically before a restore.</summary>
public sealed record BackupInfo(string Path, DateTime CreatedLocal, long SizeBytes, bool BeforeRestore);

/// <summary>Copies of the library database in a folder of the user's choice. Only files named like a backup
/// (<c>readerCompanion-YYYY-MM-DD-HHmmss….db</c>) are ever listed or deleted; a restore accepts any file but checks it
/// is a readerCompanion library first.</summary>
public interface IBackups
{
    /// <summary>Writes a consistent copy of the library, named after <paramref name="nowLocal"/>.</summary>
    BackupInfo Backup(string folder, DateTime nowLocal);

    /// <summary><see cref="Backup"/> off the calling thread.</summary>
    Task<BackupInfo> BackupAsync(string folder, DateTime nowLocal, CancellationToken cancellationToken = default);

    /// <summary>The backups in the folder, newest first; an empty list when the folder does not exist.</summary>
    IReadOnlyList<BackupInfo> List(string folder);

    /// <summary>Backs up the current library next to <paramref name="backupPath"/>, then replaces the library with
    /// that backup and upgrades it to the current version. Returns the safety backup.</summary>
    /// <exception cref="InvalidDataException">The file is not a readerCompanion library; nothing changed.</exception>
    /// <exception cref="InvalidOperationException">A scan is running; nothing changed.</exception>
    Task<BackupInfo> RestoreAsync(string backupPath, DateTime nowLocal, CancellationToken cancellationToken = default);

    /// <summary>Deletes all but the newest <paramref name="keep"/> backups in the folder, never the copies taken before a
    /// restore.</summary>
    void Prune(string folder, int keep);
}
