using ReaderCompanion.Core.Goodreads;

namespace ReaderCompanion.Core.Tests;

/// <summary>Cases from a real export compared with a real library.</summary>
public class GoodreadsMatchTests
{
    [Theory]
    [InlineData("Clean Architecture: A Craftsman's Guide to Software Structure and Design", "clean architecture")]
    [InlineData("Lean from the Trenches: Managing Large-Scale Projects with Kanban \\( PDFDrive.com \\).epub", "lean from the trenches")]
    [InlineData("Refactoring: Improving the Design of Existing Code (Addison-wesley Object Technology Series)", "refactoring")]
    [InlineData("High-Performance Browser Networking", "high performance browser networking")]
    [InlineData("Effective TypeScript ( PDFDrive.com ).pdf", "effective typescript")]
    [InlineData("Node.js Design Patterns", "node js design patterns")]
    [InlineData("Mistborn [Special Edition]", "mistborn")]
    public void MainTitle_IsTheTitleBeforeItsSubtitle_WithoutNotesOrFileExtension(string title, string main) =>
        Assert.Equal(main, GoodreadsMatch.MainTitle(title));

    [Fact]
    public void MainTitle_OfOnlyANote_IsEmpty() => Assert.Equal("", GoodreadsMatch.MainTitle("(PDFDrive.com)"));

    [Theory]
    [InlineData("Kleppmann, Martin", "kleppmann")]
    [InlineData("Mario Casciaro & Luciano Mammino", "casciaro mammino")]
    [InlineData("Betsy Beyer, Chris Jones, Jennifer Petoff, and Niall Richard Murphy", "beyer jones petoff murphy")]
    [InlineData("Preslav Nakov; Panayot Dobrikov", "nakov dobrikov")]
    [InlineData("Robert C. Martin", "martin")]
    [InlineData("Andy   Hunt", "hunt")]
    [InlineData("Unknown", "")]
    [InlineData("www.ebook777.com", "")]
    [InlineData("Anonymous", "")]
    public void Surnames_SplitJoinedNames_AndUnderstandSurnameFirst(string author, string surnames) =>
        Assert.Equal(surnames.Split(' ', StringSplitOptions.RemoveEmptyEntries), GoodreadsMatch.Surnames([author]));

    [Fact]
    public void ShareAnAuthor_ComparesSurnames()
    {
        Assert.True(GoodreadsMatch.ShareAnAuthor(["Andrew Hunt", "David Thomas"], ["Andy Hunt", "Dave Thomas", "Ward Cunningham"]));
        Assert.True(GoodreadsMatch.ShareAnAuthor(["Kleppmann, Martin"], ["Martin Kleppmann", "Chris Riccomini"]));
        Assert.False(GoodreadsMatch.ShareAnAuthor(["Unknown"], ["Martin Fowler"]));
        Assert.False(GoodreadsMatch.ShareAnAuthor(["Preslav Nakov"], ["Стойне Василев"]));
    }
}
