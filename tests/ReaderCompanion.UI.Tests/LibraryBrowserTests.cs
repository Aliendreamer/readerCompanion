using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Preferences;
using ReaderCompanion.UI.Library;

namespace ReaderCompanion.UI.Tests;

public class LibraryBrowserTests
{
    private static int _nextId;

    private static BookListItem Book(
        string title,
        string[]? authors = null,
        string[]? genres = null,
        string? series = null,
        decimal? index = null,
        BookFormat[]? formats = null,
        bool missing = false,
        DateTime added = default) =>
        new(++_nextId, title, authors ?? [], series, index, missing)
        {
            Genres = genres ?? [],
            Formats = formats ?? [BookFormat.Epub],
            DateAddedUtc = added,
        };

    private static readonly BookListItem Dune = Book("Dune", ["Frank Herbert"], ["Science Fiction"]);
    private static readonly BookListItem Mort = Book("Mort", ["Terry Pratchett"], ["Fantasy"], "Discworld", 4);
    private static readonly BookListItem Geb = Book("Gödel, Escher, Bach", ["Douglas Hofstadter"], ["Nonfiction"]);

    private static IReadOnlyList<string> Titles(BrowseResult result) =>
        [.. result.Sections.SelectMany(s => s.Books).Select(b => b.Title)];

    // ---------- 2.1 options and result shape ----------

    [Fact]
    public void Options_ListValuesPresent_Sorted_WithoutDuplicates()
    {
        var omens = Book("Good Omens", ["Terry Pratchett", "Neil Gaiman"], ["Fantasy", "Humour"], formats: [BookFormat.Pdf]);

        var options = LibraryBrowser.Options([Dune, Mort, omens]);

        Assert.Equal(["Fantasy", "Humour", "Science Fiction"], options.Genres);
        Assert.Equal(["Frank Herbert", "Neil Gaiman", "Terry Pratchett"], options.Authors);
        Assert.Equal(["Discworld"], options.Collections);
        Assert.Equal([BookFormat.Epub, BookFormat.Pdf], options.Formats);
    }

    [Fact]
    public void NoQuery_ShowsEverything_InOneUnnamedSection()
    {
        var result = LibraryBrowser.Browse([Mort, Dune], new BrowseQuery());

        var section = Assert.Single(result.Sections);
        Assert.Null(section.Name);
        Assert.Equal(["Dune", "Mort"], Titles(result));
        Assert.Equal(2, result.Total);
        Assert.Equal(2, result.Shown);
        Assert.False(new BrowseQuery().IsNarrowed);
    }

    // ---------- 2.2 filters ----------

    [Fact]
    public void TwoGenres_MatchAnyOfThem()
    {
        var result = LibraryBrowser.Browse([Dune, Mort, Geb], new BrowseQuery { Genres = ["Science Fiction", "Fantasy"] });

        Assert.Equal(["Dune", "Mort"], Titles(result));
        Assert.Equal(3, result.Total);
        Assert.Equal(2, result.Shown);
    }

    [Fact]
    public void DifferentFilters_MustAllMatch()
    {
        var player = Book("The Player of Games", ["Iain M. Banks"], ["Science Fiction"]);
        var excession = Book("Excession", ["Iain M. Banks"], ["Science Fiction"]);
        var bridge = Book("The Bridge", ["Iain M. Banks"], ["Fantasy"]);

        var result = LibraryBrowser.Browse(
            [player, excession, bridge, Dune],
            new BrowseQuery { Genres = ["Science Fiction"], Authors = ["Iain M. Banks"] });

        Assert.Equal(["Excession", "The Player of Games"], Titles(result));
    }

    [Fact]
    public void CollectionFilter_MatchesTheSeries()
    {
        var result = LibraryBrowser.Browse([Dune, Mort], new BrowseQuery { Collections = ["Discworld"] });

        Assert.Equal(["Mort"], Titles(result));
    }

    [Fact]
    public void UserCollections_CountAsCollections_InOptionsFilterAndGrouping()
    {
        var mort = Mort with { Collections = ["Book club"] };

        Assert.Equal(["Book club", "Discworld"], LibraryBrowser.Options([mort, Dune]).Collections);
        Assert.Equal(["Mort"], Titles(LibraryBrowser.Browse([mort, Dune], new BrowseQuery { Collections = ["Book club"] })));
        Assert.Equal(
            ["Book club", "Discworld", "No collection"],
            LibraryBrowser.Browse([mort, Dune], new BrowseQuery { Group = LibraryGroup.Collection }).Sections.Select(s => s.Name));
    }

    [Fact]
    public void StatusFilter_MatchesAnyPickedStatus()
    {
        var read = Dune with { ReadStatus = ReadStatus.Read };
        var wanted = Book("Wanted") with { ReadStatus = ReadStatus.WantToRead, Owned = false };

        var result = LibraryBrowser.Browse([read, Mort, wanted], new BrowseQuery { Statuses = [ReadStatus.NotStarted, ReadStatus.WantToRead] });

        Assert.Equal(["Mort", "Wanted"], Titles(result));
    }

    [Fact]
    public void Options_CountEachStatus_AndWhichWantToReadBooksHaveFiles()
    {
        var options = LibraryBrowser.Options(
        [
            Dune with { ReadStatus = ReadStatus.Read },
            Mort with { ReadStatus = ReadStatus.WantToRead },
            Book("Wanted", ["Some Writer"], ["Fantasy"]) with { ReadStatus = ReadStatus.WantToRead, Owned = false },
            Book("Also wanted", ["some writer"]) with { ReadStatus = ReadStatus.WantToRead, Owned = false },
        ]);

        Assert.Equal(1, options.StatusCounts[ReadStatus.Read]);
        Assert.Equal(3, options.StatusCounts[ReadStatus.WantToRead]);
        Assert.False(options.StatusCounts.ContainsKey(ReadStatus.Reading));
        Assert.Equal(1, options.WantToReadWithFiles);
        Assert.Equal(2, options.AuthorCounts["Some Writer"]); // names counted as they are matched: ignoring case
        Assert.Equal(2, options.GenreCounts["Fantasy"]); // Mort and Wanted
    }

    [Fact]
    public void FormatFilter_MatchesABookWithAFileInThatFormat()
    {
        var epubOnly = Book("Epub only", formats: [BookFormat.Epub]);
        var both = Book("Both", formats: [BookFormat.Epub, BookFormat.Pdf]);

        var result = LibraryBrowser.Browse([epubOnly, both], new BrowseQuery { Formats = [BookFormat.Pdf] });

        Assert.Equal(["Both"], Titles(result));
    }

    [Fact]
    public void MissingOnly_ShowsOnlyBooksWithoutAPresentFile()
    {
        var gone = Book("Gone", missing: true);

        var result = LibraryBrowser.Browse([Dune, gone], new BrowseQuery { MissingOnly = true });

        Assert.Equal(["Gone"], Titles(result));
        Assert.True(new BrowseQuery { MissingOnly = true }.IsNarrowed);
    }

    // ---------- 2.3 search ----------

    [Fact]
    public void Search_MatchesAuthorPart_IgnoringCase()
    {
        var result = LibraryBrowser.Browse([Dune, Mort], new BrowseQuery { Search = "pratch" });

        Assert.Equal(["Mort"], Titles(result));
    }

    [Fact]
    public void Search_IgnoresAccents_AndMatchesTitles()
    {
        var result = LibraryBrowser.Browse([Dune, Geb], new BrowseQuery { Search = "godel" });

        Assert.Equal(["Gödel, Escher, Bach"], Titles(result));
    }

    [Fact]
    public void BlankSearch_DoesNotNarrow()
    {
        var query = new BrowseQuery { Search = "   " };

        Assert.Equal(2, LibraryBrowser.Browse([Dune, Mort], query).Shown);
        Assert.False(query.IsNarrowed);
    }

    // ---------- 2.4 sort ----------

    [Fact]
    public void SortByTitle_IgnoresCase()
    {
        var result = LibraryBrowser.Browse([Book("beta"), Book("Alpha"), Book("Gamma")], new BrowseQuery());

        Assert.Equal(["Alpha", "beta", "Gamma"], Titles(result));
    }

    [Fact]
    public void SortByAuthor_FirstAuthorThenTitle_UnknownAuthorLast()
    {
        var anon = Book("Anon");
        var b = Book("B", ["Zed"]);
        var a = Book("A", ["Zed"]);
        var c = Book("C", ["Adams"]);

        var result = LibraryBrowser.Browse([anon, b, a, c], new BrowseQuery { Sort = LibrarySort.Author });

        Assert.Equal(["C", "A", "B", "Anon"], Titles(result));
    }

    [Fact]
    public void SortBySeries_NameThenPosition_NoSeriesLast()
    {
        var light = Book("The Light Fantastic", series: "Discworld", index: 2);
        var colour = Book("The Colour of Magic", series: "Discworld", index: 1);
        var foundation = Book("Foundation", series: "Foundation", index: 1);
        var loose = Book("Aardvark");

        var result = LibraryBrowser.Browse([loose, light, foundation, colour], new BrowseQuery { Sort = LibrarySort.Series });

        Assert.Equal(["The Colour of Magic", "The Light Fantastic", "Foundation", "Aardvark"], Titles(result));
    }

    [Fact]
    public void SortByDateAdded_NewestFirst()
    {
        var older = Book("Dune", added: new DateTime(2026, 11, 19));
        var newer = Book("Mort", added: new DateTime(2026, 11, 20));

        var result = LibraryBrowser.Browse([older, newer], new BrowseQuery { Sort = LibrarySort.DateAdded });

        Assert.Equal(["Mort", "Dune"], Titles(result));
    }

    // ---------- 2.5 grouping ----------

    [Fact]
    public void GroupByAuthor_FansOutCoAuthoredBooks()
    {
        var omens = Book("Good Omens", ["Terry Pratchett", "Neil Gaiman"]);
        var mort = Book("Mort", ["Terry Pratchett"]);

        var result = LibraryBrowser.Browse([omens, mort], new BrowseQuery { Group = LibraryGroup.Author });

        Assert.Equal(["Neil Gaiman", "Terry Pratchett"], result.Sections.Select(s => s.Name));
        Assert.Equal(["Good Omens"], result.Sections[0].Books.Select(b => b.Title));
        Assert.Equal(["Good Omens", "Mort"], result.Sections[1].Books.Select(b => b.Title));
        Assert.Equal(2, result.Shown); // each book counted once
    }

    [Fact]
    public void Grouping_PutsTheEmptyBucketLast()
    {
        var anon = Book("Anon");
        var untagged = Book("Untagged", ["Zed"]);

        Assert.Equal(
            ["Frank Herbert", "Zed", "Unknown author"],
            LibraryBrowser.Browse([anon, Dune, untagged], new BrowseQuery { Group = LibraryGroup.Author }).Sections.Select(s => s.Name));
        Assert.Equal(
            ["Science Fiction", "No genre"],
            LibraryBrowser.Browse([Dune, untagged], new BrowseQuery { Group = LibraryGroup.Genre }).Sections.Select(s => s.Name));
        Assert.Equal(
            ["Discworld", "No collection"],
            LibraryBrowser.Browse([Dune, Mort], new BrowseQuery { Group = LibraryGroup.Collection }).Sections.Select(s => s.Name));
    }

    [Fact]
    public void CatchAllSection_IsMarked()
    {
        var result = LibraryBrowser.Browse([Dune, Book("Bare")], new BrowseQuery { Group = LibraryGroup.Genre });

        Assert.Equal([false, true], result.Sections.Select(s => s.IsCatchAll));
    }

    [Fact]
    public void GroupByCollection_KeepsSeriesOrder_WhateverTheSort()
    {
        var light = Book("The Light Fantastic", series: "Discworld", index: 2, added: new DateTime(2026, 1, 2));
        var colour = Book("The Colour of Magic", series: "Discworld", index: 1, added: new DateTime(2026, 1, 1));

        var result = LibraryBrowser.Browse(
            [light, colour],
            new BrowseQuery { Group = LibraryGroup.Collection, Sort = LibrarySort.DateAdded });

        Assert.Equal(["The Colour of Magic", "The Light Fantastic"], Titles(result));
    }

    [Fact]
    public void GroupByGenre_SortsInsideEachSection()
    {
        var b = Book("B", genres: ["Fantasy"], added: new DateTime(2026, 1, 1));
        var a = Book("A", genres: ["Fantasy"], added: new DateTime(2026, 1, 2));

        var result = LibraryBrowser.Browse([b, a], new BrowseQuery { Group = LibraryGroup.Genre, Sort = LibrarySort.DateAdded });

        Assert.Equal(["A", "B"], Titles(result));
    }

    [Fact]
    public void NothingMatches_GivesNoSections()
    {
        var result = LibraryBrowser.Browse([Dune], new BrowseQuery { Search = "zzz", Group = LibraryGroup.Author });

        Assert.Empty(result.Sections);
        Assert.Equal(0, result.Shown);
    }
}
