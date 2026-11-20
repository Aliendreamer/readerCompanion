using ReaderCompanion.Core.Library;

namespace ReaderCompanion.Core.Tests;

public class DuplicateFinderTests
{
    private static DuplicateCandidate Book(int id, string title, string? author = null, string? isbn = null) =>
        new(id, title, author, isbn);

    private static IReadOnlyList<IReadOnlyList<int>> Find(
        IReadOnlyList<DuplicateCandidate> books, params (int, int)[] dismissed) =>
        DuplicateFinder.Find(books, dismissed.ToHashSet());

    [Fact]
    public void SameTitleAndAuthor_IgnoringCaseAccentsAndPunctuation_AreGrouped()
    {
        var groups = Find([
            Book(1, "Mort", "Terry Pratchett"),
            Book(2, "MORT!", "terry  pratchett"),
            Book(3, "Gödel, Escher, Bach", "Douglas Hofstadter"),
            Book(4, "Godel Escher Bach", "Douglas Hofstadter"),
        ]);

        Assert.Equal([[3, 4], [1, 2]], groups);
    }

    [Fact]
    public void SameIsbn_IgnoringHyphensAndSpaces_IsGrouped_EvenWithDifferentTitles()
    {
        var groups = Find([
            Book(1, "Mort", "Terry Pratchett", "978-0-552-13106-3"),
            Book(2, "Mort: A Discworld Novel", "Pratchett", "9780552 131063"),
        ]);

        Assert.Equal([[1, 2]], groups);
    }

    [Fact]
    public void SameTitle_DifferentAuthors_AreNotGrouped()
    {
        Assert.Empty(Find([Book(1, "Dune", "Frank Herbert"), Book(2, "Dune", "Someone Else")]));
    }

    [Fact]
    public void NoAuthor_MatchesOnlyByIsbn()
    {
        Assert.Empty(Find([Book(1, "Untitled"), Book(2, "untitled"), Book(3, "Untitled", "Someone")]));
        Assert.Equal([[1, 2]], Find([Book(1, "Untitled", isbn: "9780552131063"), Book(2, "Other", isbn: "9780552131063")]));
    }

    [Fact]
    public void DismissedPair_IsNeverGroupedTogether_EvenThroughAThirdBook()
    {
        var groups = Find(
            [Book(1, "Mort", "Terry Pratchett", "9780552131063"), // matches 3 by ISBN
             Book(2, "Mort: A Discworld Novel", "Pratchett"), // matches 3 by title and author
             Book(3, "Mort: A Discworld Novel", "Pratchett", "9780552131063")],
            (1, 2));

        Assert.DoesNotContain(groups, g => g.Contains(1) && g.Contains(2));
        Assert.Single(groups);
    }

    [Fact]
    public void ShortOrJunkIsbn_IsIgnored()
    {
        Assert.Empty(Find([Book(1, "A", "X", "123"), Book(2, "B", "Y", "123")]));
    }

    [Fact]
    public void LinksChain_IntoOneGroup()
    {
        var groups = Find([
            Book(1, "Mort", "Terry Pratchett", "9780552131063"),
            Book(2, "Mort: A Discworld Novel", "Terry Pratchett", "9780552131063"),
            Book(3, "Mort: A Discworld Novel", "Terry Pratchett"),
        ]);

        Assert.Equal([[1, 2, 3]], groups);
    }

    [Fact]
    public void DismissedPair_IsNotLinked_ButANewMatchStillIs()
    {
        var books = new[] { Book(1, "Mort", "Terry Pratchett"), Book(2, "Mort", "Terry Pratchett") };

        Assert.Empty(Find(books, (1, 2)));
        Assert.Equal([[2, 3]], Find([.. books, Book(3, "Mort", "Terry Pratchett")], (1, 2), (1, 3)));
    }

    [Fact]
    public void Groups_AreOrderedByFirstTitle_AndIdsAscending()
    {
        var groups = Find([
            Book(9, "Zed", "A"), Book(5, "Zed", "A"),
            Book(7, "Alpha", "B"), Book(3, "alpha", "B"),
        ]);

        Assert.Equal([[3, 7], [5, 9]], groups);
    }
}
