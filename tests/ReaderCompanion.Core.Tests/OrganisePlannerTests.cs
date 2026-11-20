using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Organising;

namespace ReaderCompanion.Core.Tests;

public class OrganisePlannerTests
{
    private static readonly string Root = Path.Combine(Path.GetTempPath(), "books");
    private static int _id;

    private static PlannedFile File(
        string path, string title, string[]? authors = null, string? series = null, decimal? index = null,
        string[]? genres = null, BookFormat format = BookFormat.Epub) =>
        new(++_id, Path.Combine(Root, path), title, authors ?? [], series, index, genres ?? [], format);

    private static string Rel(Move move) => Path.GetRelativePath(Root, move.To).Replace('\\', '/');

    private static IReadOnlyList<Move> Plan(OrganiseLayout layout, Func<string, bool>? exists = null, params PlannedFile[] files) =>
        OrganisePlanner.Plan(Root, layout, files, exists ?? (_ => false));

    [Fact]
    public void DefaultLayout_GenreAuthorTitle_WithLastFirstAuthor()
    {
        var hobbit = File("hobbit_final(1).epub", "The Hobbit", ["J.R.R. Tolkien"], genres: ["Fantasy", "Classics"]);

        var move = Assert.Single(Plan(OrganiseLayout.GenreAuthorTitle, files: hobbit));

        Assert.Equal(hobbit.Path, move.From);
        Assert.Equal("Fantasy/Tolkien, J.R.R/The Hobbit.epub", Rel(move));
    }

    [Fact]
    public void SeriesLayout_TwoDigitNumbers_AndNoSeriesFolder()
    {
        var moves = Plan(OrganiseLayout.AuthorSeriesTitle, files:
        [
            File("a.epub", "Mort", ["Terry Pratchett"], "Discworld", 4),
            File("b.pdf", "Interesting Times", ["Terry Pratchett"], "Discworld", 17.5m, format: BookFormat.Pdf),
            File("c.epub", "Good Omens", ["Terry Pratchett", "Neil Gaiman"]),
        ]);

        Assert.Equal(
            ["Pratchett, Terry/Discworld/04 - Mort.epub", "Pratchett, Terry/Discworld/17.5 - Interesting Times.pdf",
             "Pratchett, Terry/No series/Good Omens.epub"],
            moves.Select(Rel));
    }

    [Fact]
    public void MissingDetails_UseUnknownNames()
    {
        var move = Assert.Single(Plan(OrganiseLayout.GenreAuthorTitle, files: File("x.epub", "Poems")));

        Assert.Equal("Unknown genre/Unknown author/Poems.epub", Rel(move));
    }

    [Fact]
    public void AuthorWithCommaOrOneWord_IsKept()
    {
        var moves = Plan(OrganiseLayout.AuthorTitle, files:
            [File("a.epub", "A", ["Tolkien, J.R.R."]), File("b.epub", "B", ["Homer"])]);

        Assert.Equal(["Tolkien, J.R.R/A.epub", "Homer/B.epub"], moves.Select(Rel));
    }

    [Fact]
    public void Names_AreMadeSafe()
    {
        var move = Assert.Single(Plan(OrganiseLayout.AuthorTitle, files:
            File("a.epub", "What? A <Title>: Part 1/2...  ", ["CON"])));

        Assert.Equal("CON_/What_ A _Title__ Part 1_2.epub", Rel(move));
    }

    [Fact]
    public void LongNames_AreCutTo100Characters()
    {
        var move = Assert.Single(Plan(OrganiseLayout.AuthorTitle, files: File("a.epub", new string('x', 300), ["A"])));

        Assert.Equal(100 + ".epub".Length, Path.GetFileName(move.To).Length);
    }

    [Fact]
    public void Collisions_GetNumbered_AgainstDiskAndOtherMoves()
    {
        var taken = Path.Combine(Root, "Unknown genre", "Unknown author", "Poems.epub");

        var moves = Plan(OrganiseLayout.GenreAuthorTitle, p => string.Equals(p, taken, StringComparison.OrdinalIgnoreCase),
            File("a.epub", "Poems"), File("b.epub", "poems"));

        Assert.Equal(
            ["Unknown genre/Unknown author/Poems (2).epub", "Unknown genre/Unknown author/poems (3).epub"],
            moves.Select(Rel));
    }

    [Fact]
    public void FileAlreadyInPlace_IsNotMoved_AndItsPathIsNotACollision()
    {
        var tidy = File(Path.Combine("Fantasy", "Tolkien, J.R.R", "The Hobbit.epub"), "The Hobbit", ["J.R.R. Tolkien"], genres: ["Fantasy"]);

        Assert.Empty(Plan(OrganiseLayout.GenreAuthorTitle, p => p == tidy.Path, tidy));
    }
}
