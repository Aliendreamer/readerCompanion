using ReaderCompanion.Core.Library;

namespace ReaderCompanion.Core.Tests;

/// <summary>File subjects mapped onto the fixed BISAC-based list; cases from a real library's tags.</summary>
public class GenreMapTests
{
    [Theory]
    [InlineData("Science Fiction", "Science Fiction")]
    [InlineData("SciFi", "Science Fiction")]
    [InlineData("Sci-Fi", "Science Fiction")]
    [InlineData("Fiction - Science Fiction", "Science Fiction")]
    [InlineData("Science Fiction - General", "Science Fiction")]
    [InlineData("FICTION / Science Fiction / Space Opera", "Science Fiction")]
    [InlineData("Space Opera", "Science Fiction")]
    [InlineData("Star Wars", "Science Fiction")]
    [InlineData("Fantasy", "Fantasy")]
    [InlineData("Fantasy Fiction", "Fantasy")]
    [InlineData("Fiction - Fantasy", "Fantasy")]
    [InlineData("Fantasy - General", "Fantasy")]
    [InlineData("Magic", "Fantasy")]
    [InlineData("Forgotten Realms", "Fantasy")]
    [InlineData("Saga of Drizzt Do'Urden", "Fantasy")]
    [InlineData("Shannara (Imaginary Place)", "Fantasy")]
    [InlineData("Historical Fiction", "Historical Fiction")]
    [InlineData("Action & Adventure", "Action & Adventure")]
    [InlineData("Horror", "Horror")]
    [InlineData("Computers / Programming / General", "Programming & Computers")]
    [InlineData("History", "History")]
    [InlineData("Biography & Autobiography", "Biography & Memoir")]
    [InlineData("FIC028000", "Science Fiction")]
    [InlineData("FIC009020", "Fantasy")]
    [InlineData("COM051000", "Programming & Computers")]
    public void ASubject_MapsToItsGenre(string subject, string genre) =>
        Assert.Equal([genre], GenreMap.Map([subject]));

    [Theory]
    [InlineData("Fiction")]
    [InlineData("General")]
    [InlineData("Epic")]
    [InlineData("Contemporary")]
    [InlineData("High Tech")]
    [InlineData("Galactic Republic Era")]
    [InlineData("")]
    public void ASubjectThatIsNoGenre_IsDropped(string subject) => Assert.Empty(GenreMap.Map([subject]));

    [Fact]
    public void SeveralSubjects_GiveEachGenreOnce_InTheListsOrder()
    {
        var genres = GenreMap.Map(["Fiction", "SciFi", "Fantasy Fiction", "General", "Science Fiction & Fantasy", "Space Opera"]);

        Assert.Equal(["Fantasy", "Science Fiction"], genres);
    }

    [Fact]
    public void ScienceAndScienceFiction_AreNotConfused()
    {
        Assert.Equal(["Science"], GenreMap.Map(["Science / Physics"]));
        Assert.Equal(["Science Fiction"], GenreMap.Map(["Science Fiction"]));
    }

    [Fact]
    public void TheListItself_MapsToItself()
    {
        Assert.Equal(GenreMap.All, GenreMap.Map(GenreMap.All));
    }
}
