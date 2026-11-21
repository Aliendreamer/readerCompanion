using ReaderCompanion.Core.Metadata;

namespace ReaderCompanion.Core.Tests;

/// <summary>Series names files carry in forms seen in a real library.</summary>
public class SeriesNameTests
{
    [Theory]
    [InlineData("Serified.Dresden Files.SciFan.(#01 of 14)._.ICA Serified", "Dresden Files", 1.0)]
    [InlineData("Grouped.Dresden Files.SciFan.(#00 of 14).(10 Short Stories)_.ICC Fair", "Dresden Files", 0.0)]
    [InlineData("Serified.Tales of Alvin Maker.SciFan.(#6 of 6)._.ICA Serified", "Tales of Alvin Maker", 6.0)]
    [InlineData("Serified.Discworld.SciFan.(#08 of 41)._.ICA Serified", "Discworld", 8.0)] // no space: the longest part
    public void AReleaseTaggedName_GivesTheSeriesAndItsNumber(string written, string series, double index) =>
        Assert.Equal((series, (decimal?)index), SeriesName.Clean(written, null));

    [Fact]
    public void TheNumberInTheName_WinsOverTheFilesOwn()
    {
        // Seen in a real library: these files all also say "series index 1", whatever their place in the series.
        Assert.Equal(("Dresden Files", (decimal?)4), SeriesName.Clean("Serified.Dresden Files.SciFan.(#04 of 14)._.ICA Serified", 1));
    }

    [Theory]
    [InlineData("Discworld")]
    [InlineData("A Song of Ice and Fire")]
    [InlineData("Mr. Monk")]
    [InlineData("Star Wars: Legends (#1 of 3)")] // not the dotted release form: left alone
    public void AnyOtherName_IsLeftAsItIs(string written) => Assert.Equal((written, (decimal?)2), SeriesName.Clean(written, 2));
}
