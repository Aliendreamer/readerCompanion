using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Organising;

namespace ReaderCompanion.Core.Tests;

/// <summary>One layout: Group / Author / [Series /] [NN - ]Title, the group decided per author.</summary>
public class OrganisePlannerTests
{
    private static readonly string Root = Path.Combine(Path.GetTempPath(), "books");
    private static int _id;

    private static PlannedFile File(
        string path, string title, string[]? authors = null, string? series = null, decimal? index = null,
        string[]? genres = null, BookFormat format = BookFormat.Epub) =>
        new(++_id, Path.Combine(Root, path), title, authors ?? [], series, index, genres ?? [], format);

    private static string Rel(Move move) => Path.GetRelativePath(Root, move.To).Replace('\\', '/');

    private static IReadOnlyList<Move> Plan(Func<string, bool>? exists = null, params PlannedFile[] files) =>
        OrganisePlanner.Plan(Root, files, exists ?? (_ => false));

    [Fact]
    public void ASeriesBook_GoesUnderItsAuthorsGroup_WithItsNumber()
    {
        var moves = Plan(files:
        [
            File("a.epub", "The Player of Games", ["Iain M. Banks"], "Culture", 2, ["Science Fiction"]),
            File("b.epub", "Excession", ["Iain M. Banks"], "Culture", 5, ["Science Fiction"]),
            File("c.pdf", "Interesting Times", ["Terry Pratchett"], "Discworld", 17.5m, ["Fantasy"], BookFormat.Pdf),
        ]);

        Assert.Equal(
            ["Science Fiction/Iain M. Banks/Culture/02 - The Player of Games.epub",
             "Science Fiction/Iain M. Banks/Culture/05 - Excession.epub",
             "Fantasy/Terry Pratchett/Discworld/17.5 - Interesting Times.pdf"],
            moves.Select(Rel));
    }

    [Fact]
    public void AStandaloneBook_SitsRightUnderItsAuthor_WithTheAuthorsOtherBooks()
    {
        var moves = Plan(files:
        [
            File("a.epub", "The Player of Games", ["Iain M. Banks"], "Culture", 2, ["Science Fiction"]),
            File("b.epub", "Excession", ["Iain M. Banks"], "Culture", 5, ["Science Fiction"]),
            File("c.epub", "The Algebraist", ["Iain M. Banks"], genres: ["Fantasy"]), // its own genre does not split the author
            File("d.epub", "Matter", ["Iain M. Banks"], "Culture"), // a series without a number
        ]);

        Assert.Equal("Science Fiction/Iain M. Banks/The Algebraist.epub", Rel(moves[2]));
        Assert.Equal("Science Fiction/Iain M. Banks/Culture/Matter.epub", Rel(moves[3]));
    }

    [Fact]
    public void AnAuthorsGroup_IsTheirMostCommonGenre_TiesAlphabetical()
    {
        var moves = Plan(files:
        [
            File("a.epub", "A", ["Writer One"], genres: ["Horror", "Fantasy"]),
            File("b.epub", "B", ["Writer One"], genres: ["Fantasy"]),
            File("c.epub", "C", ["Writer Two"], genres: ["Mystery"]),
            File("d.epub", "D", ["Writer Two"], genres: ["Crime"]),
        ]);

        Assert.Equal(["Fantasy/Writer One/A.epub", "Fantasy/Writer One/B.epub", "Crime/Writer Two/C.epub", "Crime/Writer Two/D.epub"],
            moves.Select(Rel));
    }

    [Fact]
    public void WithoutGenres_TheAuthorsGroupIsTheFolderTheirBooksAreIn()
    {
        var moves = Plan(files:
        [
            File(Path.Combine("ProgramingBooks", "clean-architecture.epub"), "Clean Architecture", ["Robert C. Martin"]),
            File(Path.Combine("ProgramingBooks", "Robert C. Martin", "Clean Code.epub"), "Clean Code", ["Robert C. Martin"]),
            File(Path.Combine("ProgramingBooks", "Dan Vanderkam", "Effective TypeScript.epub"), "Effective TypeScript", ["Dan Vanderkam"]),
        ]);

        Assert.Equal(["ProgramingBooks/Robert C. Martin/Clean Architecture.epub"], moves.Select(Rel)); // the others are in place
    }

    [Fact]
    public void WithoutGenres_AFolderNamedAfterTheAuthor_IsNotAGroup()
    {
        var moves = Plan(files:
        [
            File(Path.Combine("Stephen King", "Novels", "Thinner - Stephen King.epub"), "Thinner", ["Stephen King"]),
            File(Path.Combine("Iain Banks", "The Algebraist", "The Algebraist - Iain M. Banks.epub"), "The Algebraist", ["Iain M. Banks"]),
            File("loose.epub", "Loose", ["Some Writer"]), // in the root: no folder, no group
        ]);

        Assert.Equal(["Stephen King/Thinner.epub", "Iain M. Banks/The Algebraist.epub", "Some Writer/Loose.epub"], moves.Select(Rel));
    }

    [Fact]
    public void WithoutGenres_AFolderOfOneAuthorsBooks_IsNotAGroup()
    {
        // Seen in a real library: a book kept in its own folder. Only a folder holding several authors is a group.
        var moves = Plan(files:
        [
            File(Path.Combine("Code Complete, 2nd Edition", "Code Complete - Steve McConnell.epub"), "Code Complete", ["Steve McConnell"]),
            File(Path.Combine("qabooks", "a.epub"), "Agile Testing", ["Lisa Crispin"]),
            File(Path.Combine("qabooks", "b.epub"), "The Art of Unit Testing", ["Roy Osherove"]),
        ]);

        Assert.Equal(
            ["Steve McConnell/Code Complete.epub", "qabooks/Lisa Crispin/Agile Testing.epub", "qabooks/Roy Osherove/The Art of Unit Testing.epub"],
            moves.Select(Rel));
    }

    [Theory]
    [InlineData("Thomas H. Cormen, Charles E. Leiserson, Ronald L. Rivest, Clifford Stein", "Thomas H. Cormen")]
    [InlineData("By Kent Beck, Cynthia Andres", "Kent Beck")]
    [InlineData("Kleppmann, Martin", "Martin Kleppmann")]
    [InlineData("Mario Casciaro & Luciano Mammino", "Mario Casciaro")]
    [InlineData("Iain M. Banks", "Iain M. Banks")]
    public void SeveralAuthorsInOneName_TheFolderIsTheFirstPerson_GivenNameFirst(string written, string folder)
    {
        var move = Assert.Single(Plan(files: File("a.epub", "A Book", [written])));

        Assert.Equal($"{folder}/A Book.epub", Rel(move));
    }

    [Theory]
    [InlineData("")] // none
    [InlineData("Unknown")]
    [InlineData("www.ebook777.com")]
    public void ABookWithoutAUsableAuthor_IsNotMoved(string author)
    {
        string[] authors = author.Length == 0 ? [] : [author];

        Assert.Empty(Plan(files: File(Path.Combine("ProgramingBooks", "learning-docker.pdf"), "learning-docker", authors, genres: ["Programming"])));
    }

    [Fact]
    public void Names_AreMadeSafe()
    {
        var move = Assert.Single(Plan(files: File("a.epub", "What? A <Title>: Part 1/2...  ", ["CON"])));

        Assert.Equal("CON_/What_ A _Title__ Part 1_2.epub", Rel(move));
    }

    [Fact]
    public void LongNames_AreCutTo100Characters()
    {
        var move = Assert.Single(Plan(files: File("a.epub", new string('x', 300), ["A Writer"])));

        Assert.Equal(100 + ".epub".Length, Path.GetFileName(move.To).Length);
    }

    [Fact]
    public void Collisions_GetNumbered_AgainstDiskAndOtherMoves()
    {
        var taken = Path.Combine(Root, "Poetry", "Ann Writer", "Poems.epub");

        var moves = Plan(p => string.Equals(p, taken, StringComparison.OrdinalIgnoreCase),
            File("a.epub", "Poems", ["Ann Writer"], genres: ["Poetry"]), File("b.epub", "poems", ["Ann Writer"], genres: ["Poetry"]));

        Assert.Equal(["Poetry/Ann Writer/Poems (2).epub", "Poetry/Ann Writer/poems (3).epub"], moves.Select(Rel));
    }

    [Fact]
    public void FileAlreadyInPlace_IsNotMoved_AndItsPathIsNotACollision()
    {
        var tidy = File(Path.Combine("Fantasy", "J.R.R. Tolkien", "The Hobbit.epub"), "The Hobbit", ["J.R.R. Tolkien"], genres: ["Fantasy"]);

        Assert.Empty(Plan(p => p == tidy.Path, tidy));
    }
}
