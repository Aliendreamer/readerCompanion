using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Organising;
using ReaderCompanion.Core.Scanning;

namespace ReaderCompanion.Data.Tests;

public sealed class OrganiserTests : IDisposable
{
    private readonly TempLibrary _library = new();
    private readonly string _root;

    public OrganiserTests()
    {
        _root = Path.Combine(_library.Root, "books");
        Directory.CreateDirectory(_root);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private IOrganiser Organiser => _library.Services.CreateScope().ServiceProvider.GetRequiredService<IOrganiser>();

    /// <summary>A book with one real file at <paramref name="relative"/> under the library folder.</summary>
    private string AddBook(string relative, string title, string? author = null, string? genre = null)
    {
        var path = Path.Combine(_root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, title);
        using var db = _library.NewContext();
        var names = new CatalogNames(db);
        var book = new Book { Title = title };
        names.SetAuthors(book, author is null ? [] : [author]);
        names.SetGenres(book, genre is null ? [] : [genre]);
        book.Files.Add(TestFiles.File(path));
        db.Books.Add(book);
        db.SaveChanges();
        return path;
    }

    private string Rel(string path) => Path.GetRelativePath(_root, path).Replace('\\', '/');

    private List<string> StoredPaths()
    {
        using var db = _library.NewContext();
        return [.. db.BookFiles.AsNoTracking().Select(f => f.Path).AsEnumerable().Select(Rel).Order()];
    }

    [Fact]
    public async Task Preview_ListsOnlyFilesThatWouldMove()
    {
        AddBook("hobbit_final(1).epub", "The Hobbit", "J.R.R. Tolkien", "Fantasy");
        AddBook("Fantasy/Herbert, Frank/Dune.epub", "Dune", "Frank Herbert", "Fantasy");

        var moves = await Organiser.PreviewAsync(_root, OrganiseLayout.GenreAuthorTitle, cancellationToken: Token);

        var move = Assert.Single(moves);
        Assert.Equal("Fantasy/Tolkien, J.R.R/The Hobbit.epub", Rel(move.To));
    }

    [Fact]
    public async Task Apply_MovesFiles_UpdatesTheLibrary_AndRemovesEmptyFolders()
    {
        AddBook("downloads/old/hobbit.epub", "The Hobbit", "J.R.R. Tolkien", "Fantasy");
        var moves = await Organiser.PreviewAsync(_root, OrganiseLayout.GenreAuthorTitle, cancellationToken: Token);

        var result = await Organiser.ApplyAsync(_root, moves, cancellationToken: Token);

        Assert.Equal(1, result.Moved);
        Assert.Empty(result.Failed);
        Assert.True(File.Exists(Path.Combine(_root, "Fantasy", "Tolkien, J.R.R", "The Hobbit.epub")));
        Assert.Equal(["Fantasy/Tolkien, J.R.R/The Hobbit.epub"], StoredPaths());
        Assert.False(Directory.Exists(Path.Combine(_root, "downloads")));
        Assert.True(Directory.Exists(_root));
    }

    [Fact]
    public async Task Apply_ReportsAFileThatCannotMove_AndMovesTheRest()
    {
        var gone = AddBook("a.epub", "Gone", "A Writer");
        AddBook("b.epub", "Here", "A Writer");
        var moves = await Organiser.PreviewAsync(_root, OrganiseLayout.AuthorTitle, cancellationToken: Token);
        File.Delete(gone);

        var result = await Organiser.ApplyAsync(_root, moves, cancellationToken: Token);

        Assert.Equal(1, result.Moved);
        Assert.Equal(gone, Assert.Single(result.Failed).From);
    }

    [Fact]
    public async Task Apply_IsRefused_WhileAScanRuns_AndScansWaitForOrganising()
    {
        AddBook("a.epub", "A", "A Writer");
        var moves = await Organiser.PreviewAsync(_root, OrganiseLayout.AuthorTitle, cancellationToken: Token);
        var gate = _library.Services.GetRequiredService<LibraryWriteGate>();

        Assert.True(gate.TryEnter(LibraryWork.Scan));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Organiser.ApplyAsync(_root, moves, cancellationToken: Token));
        gate.Exit();

        Assert.True(gate.TryEnter(LibraryWork.Organise));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _library.Services.GetRequiredService<ILibraryScanner>().ScanAsync(cancellationToken: Token));
        gate.Exit();
    }

    [Fact]
    public async Task PreviewApplyAndUndo_ReportWhatTheyAreDoing()
    {
        AddBook("x/hobbit.epub", "The Hobbit", "J.R.R. Tolkien", "Fantasy");
        AddBook("dune.epub", "Dune", "Frank Herbert", "Science Fiction");
        var reports = new List<OrganiseProgress>();
        var progress = new Reports(reports.Add);

        var moves = await Organiser.PreviewAsync(_root, OrganiseLayout.GenreAuthorTitle, progress, Token);
        Assert.Equal(OrganiseStage.Planning, reports[0].Stage);

        reports.Clear();
        await Organiser.ApplyAsync(_root, moves, progress, Token);
        Assert.Equal(
            [new(OrganiseStage.Moving, 0, 2), new(OrganiseStage.Moving, 1, 2), new(OrganiseStage.Moving, 2, 2), new(OrganiseStage.Tidying)],
            reports);

        reports.Clear();
        await Organiser.UndoLastAsync(progress, Token);
        Assert.Equal(new OrganiseProgress(OrganiseStage.MovingBack, 2, 2), reports.Last(r => r.Stage == OrganiseStage.MovingBack));
    }

    private sealed class Reports(Action<OrganiseProgress> report) : IProgress<OrganiseProgress>
    {
        public void Report(OrganiseProgress value) => report(value);
    }

    [Fact]
    public async Task Undo_PutsFilesBack_AndTheLibraryFollows()
    {
        AddBook("x/hobbit.epub", "The Hobbit", "J.R.R. Tolkien", "Fantasy");
        AddBook("dune.epub", "Dune", "Frank Herbert", "Science Fiction");
        await Organiser.ApplyAsync(_root, await Organiser.PreviewAsync(_root, OrganiseLayout.GenreAuthorTitle, cancellationToken: Token), cancellationToken: Token);
        var last = await Organiser.LastAsync(Token);
        Assert.Equal(2, last!.FileCount);

        var result = await Organiser.UndoLastAsync(cancellationToken: Token);

        Assert.Equal(2, result.Moved);
        Assert.Equal(["dune.epub", "x/hobbit.epub"], StoredPaths());
        Assert.True(File.Exists(Path.Combine(_root, "x", "hobbit.epub")));
        Assert.False(Directory.Exists(Path.Combine(_root, "Fantasy")));
        Assert.Null(await Organiser.LastAsync(Token));
    }

    [Fact]
    public async Task Undo_SkipsAFileMovedSince_AndListsIt()
    {
        AddBook("a.epub", "A", "A Writer");
        AddBook("b.epub", "B", "A Writer");
        await Organiser.ApplyAsync(_root, await Organiser.PreviewAsync(_root, OrganiseLayout.AuthorTitle, cancellationToken: Token), cancellationToken: Token);
        var moved = Path.Combine(_root, "Writer, A", "A.epub");
        File.Move(moved, Path.Combine(_root, "elsewhere.epub"));

        var result = await Organiser.UndoLastAsync(cancellationToken: Token);

        Assert.Equal(1, result.Moved);
        Assert.Equal(moved, Assert.Single(result.Failed).From);
        Assert.True(File.Exists(Path.Combine(_root, "b.epub")));
        Assert.Equal(1, (await Organiser.LastAsync(Token))!.FileCount); // the rest can still be undone later
    }

    [Fact]
    public async Task Apply_RefusesMovesOutsideTheFolder()
    {
        var path = AddBook("a.epub", "A", "A Writer");
        var outside = Path.Combine(_library.Root, "elsewhere", "a.epub");

        var result = await Organiser.ApplyAsync(_root, [new Move(1, path, outside)], cancellationToken: Token);

        Assert.Equal(0, result.Moved);
        Assert.Single(result.Failed);
        Assert.True(File.Exists(path));
        Assert.False(File.Exists(outside));
    }

    [Fact]
    public async Task Apply_WithNothingToMove_RecordsNothing()
    {
        await Organiser.ApplyAsync(_root, [], cancellationToken: Token);

        using var db = _library.NewContext();
        Assert.Empty(db.FileOperations);
    }

    public void Dispose() => _library.Dispose();
}
