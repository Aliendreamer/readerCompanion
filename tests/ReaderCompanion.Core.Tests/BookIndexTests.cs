using ReaderCompanion.Core.Library;

namespace ReaderCompanion.Core.Tests;

public class BookIndexTests
{
    [Fact]
    public void Find_TriesKeysInOrder_FallingBackFromIsbnToTitle()
    {
        var index = new BookIndex<int>();
        index.Add(7, MatchKeys.For(null, "Dune", "Frank Herbert"));

        Assert.Equal(7, index.Find(MatchKeys.For("9780441172719", "Dune", "Frank Herbert")));
        Assert.Equal(0, index.Find(MatchKeys.For("9780441172719", "Other", "Someone")));
    }

    [Fact]
    public void TwoBooksWithTheSameKey_TheSecondIsFoundOnceTheFirstIsRemoved()
    {
        var index = new BookIndex<int>();
        index.Add(7, MatchKeys.For(null, "Dune", "Frank Herbert"));
        index.Add(8, MatchKeys.For(null, "Dune", "Frank Herbert"));

        index.Remove(7);

        Assert.Equal(8, index.Find(MatchKeys.For(null, "Dune", "Frank Herbert")));
    }

    [Fact]
    public void Remove_ForgetsAllTheBooksKeys()
    {
        var index = new BookIndex<int>();
        index.Add(7, MatchKeys.For("9780441172719", "Dune", "Frank Herbert"));

        index.Remove(7);

        Assert.Equal(0, index.Find(MatchKeys.For("9780441172719", "Dune", "Frank Herbert")));
    }
}
