using ReaderCompanion.Core.Library;

namespace ReaderCompanion.UI.Library;

/// <summary>Which book of a duplicate group to keep unless the user picks another.</summary>
public static class DuplicatePick
{
    /// <summary>The edited book; else the one with the most details filled in; else one with an EPUB; else the
    /// lowest id.</summary>
    public static int Default(IReadOnlyList<BookDetails> books) => books
        .OrderByDescending(b => b.Edited)
        .ThenByDescending(Filled)
        .ThenByDescending(b => b.Book.Formats.Contains(BookFormat.Epub))
        .ThenBy(b => b.Book.Id)
        .First().Book.Id;

    private static int Filled(BookDetails d) =>
        (d.Book.Authors.Count > 0 ? 1 : 0)
        + (d.Book.Series is null ? 0 : 1)
        + (d.Book.Genres.Count > 0 ? 1 : 0)
        + (d.Isbn is null ? 0 : 1)
        + (d.Language is null ? 0 : 1)
        + (d.Book.CoverFileName is null ? 0 : 1);
}
