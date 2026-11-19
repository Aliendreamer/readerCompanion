using ReaderCompanion.Core.Metadata;

namespace ReaderCompanion.Core.Tests;

public class NameNormalizerTests
{
    [Theory]
    [InlineData("  Ursula K. Le Guin  ")]
    [InlineData("ursula k.  le guin")]
    [InlineData("URSULA\tK.\nLE GUIN")]
    public void Normalize_IgnoresCaseAndWhitespace(string name) =>
        Assert.Equal(NameNormalizer.Normalize("Ursula K. Le Guin"), NameNormalizer.Normalize(name));

    [Fact]
    public void Normalize_DifferentNames_Differ() =>
        Assert.NotEqual(NameNormalizer.Normalize("Le Guin"), NameNormalizer.Normalize("LeGuin"));

    [Fact]
    public void Clean_TrimsAndCollapsesWhitespace_KeepingCase() =>
        Assert.Equal("Ursula K. Le Guin", NameNormalizer.Clean("  Ursula   K. Le\tGuin "));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Clean_BlankValue_ReturnsNull(string? value) => Assert.Null(NameNormalizer.Clean(value));
}
