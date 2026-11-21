using System.Text;
using ReaderCompanion.Client;

namespace ReaderCompanion.Data.Tests;

/// <summary>The built-in OAuth client is kept scrambled and only unscrambled in memory.</summary>
public class ScramblerTests
{
    private static readonly byte[] Key = [0x5a, 0x13, 0xc7, 0x88, 0x01, 0xfe, 0x42];

    [Fact]
    public void Scrambled_UnscramblesBackToTheText()
    {
        var scrambled = Scrambler.Scramble("GOCSPX-a-secret", Key);

        Assert.Equal("GOCSPX-a-secret", Scrambler.Unscramble(scrambled, Key));
    }

    [Fact]
    public void Scrambled_DoesNotHoldTheTextReadably()
    {
        var scrambled = Scrambler.Scramble("GOCSPX-a-secret", Key);

        Assert.DoesNotContain("GOCSPX", Encoding.UTF8.GetString(scrambled), StringComparison.Ordinal);
        Assert.DoesNotContain("GOCSPX", Encoding.Latin1.GetString(scrambled), StringComparison.Ordinal);
    }
}
