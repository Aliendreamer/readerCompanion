using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Metadata;
using ReaderCompanion.Core.Scanning;

namespace ReaderCompanion.Data;

internal sealed class LibraryScanner(IServiceScopeFactory scopes, LibraryDataPaths paths) : ILibraryScanner
{
    private const int BatchSize = 100;
    private const int ProgressIntervalMs = 100;

    private static readonly EnumerationOptions Enumeration = new()
    {
        RecurseSubdirectories = true,
        IgnoreInaccessible = true,
        MatchType = MatchType.Simple,
        AttributesToSkip = FileAttributes.ReparsePoint, // symlinked folders are not followed (no loops)
    };

    private int _running;

    public bool IsScanning => Volatile.Read(ref _running) == 1;

    public Task<ScanSummary> ScanAsync(IProgress<ScanProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
        {
            return Task.FromException<ScanSummary>(new InvalidOperationException("A scan is already running."));
        }

        return Task.Run(
            () =>
            {
                try
                {
                    return Scan(progress, cancellationToken);
                }
                finally
                {
                    Volatile.Write(ref _running, 0);
                }
            },
            CancellationToken.None);
    }

    private ScanSummary Scan(IProgress<ScanProgress>? progress, CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();

        var folders = db.LibraryFolders.AsNoTracking().Select(f => f.Path).ToList();
        var known = db.BookFiles.AsNoTracking()
            .Select(f => new KnownFile(f.Id, f.BookId, f.Path, f.SizeBytes, f.LastWriteUtc, f.ContentHash, f.Status))
            .ToList();
        var knownByPath = known.ToDictionary(k => k.Path, LibraryPaths.Comparer);

        var unavailable = new List<string>();
        var candidates = new List<FileInfo>();
        foreach (var root in folders)
        {
            var files = TryEnumerate(root, known);
            if (files is null)
            {
                unavailable.Add(root);
            }
            else
            {
                candidates.AddRange(files);
            }
        }

        candidates = candidates.DistinctBy(f => f.FullName, LibraryPaths.Comparer).ToList();

        var found = new List<FoundFile>(candidates.Count);
        var filesRead = 0;
        var sinceReport = Stopwatch.StartNew();
        for (var i = 0; i < candidates.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var info = candidates[i];
            knownByPath.TryGetValue(info.FullName, out var k);
            var unchanged = ScanReconciler.IsUnchanged(k, info.Length, info.LastWriteTimeUtc);
            var hash = unchanged ? k!.ContentHash : TryHashFile(info.FullName);
            if (hash is not null)
            {
                filesRead += unchanged ? 0 : 1;
                found.Add(new FoundFile(info.FullName, info.Length, info.LastWriteTimeUtc, hash));
            }

            if (i == candidates.Count - 1 || sinceReport.ElapsedMilliseconds >= ProgressIntervalMs)
            {
                progress?.Report(new ScanProgress(i + 1, candidates.Count));
                sinceReport.Restart();
            }
        }

        var plan = ScanReconciler.Plan(known, found, unavailable);
        IReadOnlyList<UnreadableFile> unreadable = [];
        if (plan.Actions.Any(a => a is not KeepFile))
        {
            var writer = new CatalogWriter(db, paths.CoversFolder, DateTime.UtcNow);
            var applied = 0;
            foreach (var action in plan.Actions)
            {
                writer.Apply(action);
                if (++applied % BatchSize == 0)
                {
                    db.SaveChanges();
                }
            }

            db.SaveChanges();
            unreadable = writer.Unreadable;
        }

        int Count<T>() => plan.Actions.OfType<T>().Count();
        return new ScanSummary
        {
            Added = Count<AddBook>() + Count<SplitToNewBook>(),
            Moved = Count<MoveFile>(),
            Updated = Count<UpdateContent>() + Count<ReassignFile>(),
            MarkedMissing = Count<MarkMissing>(),
            FilesRead = filesRead,
            Unreadable = unreadable,
            UnavailableFolders = unavailable,
        };
    }

    /// <summary>
    /// The supported files in a library folder (FileInfo from enumeration carries size and write time, so one stat
    /// per file), or null when the folder must be treated as unavailable: missing, failing to list, or completely
    /// empty although it had books — an unmounted drive's mount point looks exactly like that.
    /// </summary>
    private static List<FileInfo>? TryEnumerate(string root, List<KnownFile> known)
    {
        try
        {
            var folder = new DirectoryInfo(root);
            if (!folder.Exists)
            {
                return null;
            }

            var hadBooks = known.Any(k => k.Status == FileStatus.Present && LibraryPaths.IsSameOrUnder(k.Path, root));
            if (hadBooks && !folder.EnumerateFileSystemInfos().Any())
            {
                return null;
            }

            return folder.EnumerateFiles("*", Enumeration).Where(f => BookMetadataReader.IsSupported(f.Name)).ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>SHA-256 of the file, or null if it vanished or became unreadable during the scan.</summary>
    private static string? TryHashFile(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            return Convert.ToHexStringLower(SHA256.HashData(stream));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
