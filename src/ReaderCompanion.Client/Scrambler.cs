using System.Text;

namespace ReaderCompanion.Client;

/// <summary>Keeps text out of plain sight in the program file: each byte is mixed with a key byte. This only stops the
/// text from being read directly; together with obfuscation it hides the built-in client from casual digging.</summary>
internal static class Scrambler
{
    public static byte[] Scramble(string text, byte[] key) => Mix(Encoding.UTF8.GetBytes(text), key);

    public static string Unscramble(byte[] scrambled, byte[] key) => Encoding.UTF8.GetString(Mix(scrambled, key));

    private static byte[] Mix(byte[] bytes, byte[] key)
    {
        var mixed = new byte[bytes.Length];
        for (var i = 0; i < bytes.Length; i++)
        {
            mixed[i] = (byte)(bytes[i] ^ key[i % key.Length] ^ (byte)(i * 31)); // the position too, so repeats differ
        }

        return mixed;
    }
}
