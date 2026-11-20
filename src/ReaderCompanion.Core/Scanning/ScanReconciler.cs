using ReaderCompanion.Core.Library;

namespace ReaderCompanion.Core.Scanning;

/// <summary>Decides, without touching disk or database, how a scan's findings change the library.</summary>
public static class ScanReconciler
{
    /// <summary>True when a file can be trusted unchanged without re-reading it.</summary>
    public static bool IsUnchanged(KnownFile? known, long sizeBytes, DateTime lastWriteUtc) =>
        known is not null && known.SizeBytes == sizeBytes && known.LastWriteUtc == lastWriteUtc;

    public static ScanPlan Plan(
        IReadOnlyList<KnownFile> known, IReadOnlyList<FoundFile> found, IReadOnlyCollection<string> unavailableRoots)
    {
        var actions = new List<ScanAction>();
        var knownByPath = known.ToDictionary(k => k.Path, LibraryPaths.Comparer);
        var foundPaths = found.Select(f => f.Path).ToHashSet(LibraryPaths.Comparer);
        bool Offline(KnownFile k) => unavailableRoots.Any(root => LibraryPaths.IsSameOrUnder(k.Path, root));

        // Book that owns each content, preferring a present file.
        var bookByHash = known
            .OrderBy(k => k.Status == FileStatus.Present ? 0 : 1)
            .GroupBy(k => k.ContentHash)
            .ToDictionary(g => g.Key, g => g.First().BookId);

        var filesByBook = known.ToLookup(k => k.BookId);
        var presentAfterScan = new HashSet<int>();
        var changed = new List<(KnownFile File, FoundFile Now)>();
        var unknownPaths = new List<FoundFile>();

        foreach (var f in found)
        {
            if (!knownByPath.TryGetValue(f.Path, out var k))
            {
                unknownPaths.Add(f);
            }
            else if (k.ContentHash == f.ContentHash)
            {
                actions.Add(k.Status == FileStatus.Missing ? new Reappear(k) : new KeepFile(k));
                presentAfterScan.Add(k.FileId);
            }
            else
            {
                changed.Add((k, f));
            }
        }

        // Known files no longer at their path (outside unavailable folders) can be the source of a move.
        var gone = known.Where(k => !foundPaths.Contains(k.Path) && !Offline(k)).ToList();
        var moveSources = gone.GroupBy(k => k.ContentHash).ToDictionary(g => g.Key, g => new Queue<KnownFile>(g));
        var moved = new HashSet<int>();
        var gainedCopy = new HashSet<(int BookId, string Hash)>(); // copies that joined a book this scan
        var newContent = new Dictionary<string, List<FoundFile>>();

        foreach (var f in unknownPaths)
        {
            if (moveSources.TryGetValue(f.ContentHash, out var sources) && sources.Count > 0)
            {
                var source = sources.Dequeue();
                actions.Add(new MoveFile(source, f));
                moved.Add(source.FileId);
                presentAfterScan.Add(source.FileId);
            }
            else if (bookByHash.TryGetValue(f.ContentHash, out var bookId))
            {
                actions.Add(new JoinBook(bookId, f));
                gainedCopy.Add((bookId, f.ContentHash));
            }
            else
            {
                (newContent.TryGetValue(f.ContentHash, out var group) ? group : newContent[f.ContentHash] = []).Add(f);
            }
        }

        foreach (var (k, now) in changed)
        {
            if (bookByHash.TryGetValue(now.ContentHash, out var otherBook) && otherBook != k.BookId)
            {
                actions.Add(new ReassignFile(k, otherBook, now));
            }
            // Split only if an identical copy of the old content stays with the book; a merged book's other files
            // (different content, e.g. the PDF of an EPUB) do not count.
            else if (gainedCopy.Contains((k.BookId, k.ContentHash))
                || filesByBook[k.BookId].Any(o => o.FileId != k.FileId && o.ContentHash == k.ContentHash
                    && presentAfterScan.Contains(o.FileId)))
            {
                actions.Add(new SplitToNewBook(k, now));
            }
            else
            {
                actions.Add(new UpdateContent(k, now));
            }
        }

        actions.AddRange(newContent.Values.Select(files => new AddBook(files)));
        actions.AddRange(gone
            .Where(k => !moved.Contains(k.FileId) && k.Status == FileStatus.Present)
            .Select(k => new MarkMissing(k)));

        return new ScanPlan(actions);
    }
}
