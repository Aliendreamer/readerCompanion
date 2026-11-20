namespace ReaderCompanion.Core.Library;

public enum CollectionKind
{
    Series,
    User,
}

/// <summary>A link from a book to one of the user's own collections (series use <see cref="Book.Series"/>).</summary>
public sealed class BookCollection
{
    public int BookId { get; set; }

    public Book Book { get; set; } = null!;

    public int CollectionId { get; set; }

    public Collection Collection { get; set; } = null!;
}

public sealed class Collection
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string NormalizedName { get; set; } = "";

    public CollectionKind Kind { get; set; }
}
