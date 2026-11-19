namespace ReaderCompanion.Core.Library;

public sealed class Genre
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string NormalizedName { get; set; } = "";
}

public sealed class BookGenre
{
    public int BookId { get; set; }

    public Book Book { get; set; } = null!;

    public int GenreId { get; set; }

    public Genre Genre { get; set; } = null!;
}
