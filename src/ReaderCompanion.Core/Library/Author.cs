namespace ReaderCompanion.Core.Library;

public sealed class Author
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string NormalizedName { get; set; } = "";
}

public sealed class BookAuthor
{
    public int BookId { get; set; }

    public Book Book { get; set; } = null!;

    public int AuthorId { get; set; }

    public Author Author { get; set; } = null!;

    /// <summary>Position of the author in the book's author list, starting at 0.</summary>
    public int Order { get; set; }
}
