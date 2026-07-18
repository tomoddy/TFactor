namespace TFactor.Services;

/// <summary>
/// Minimal Base32 (RFC 4648) decoder. TOTP secrets are conventionally shared as Base32 strings (e.g. "JBSWY3DPEHPK3PXP") rather than raw bytes.
/// </summary>
public static class Base32
{
    /// <summary>
    /// The Base32 alphabet, as defined in RFC 4648. Note that this is not the same as the Base32hex alphabet.
    /// </summary>
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    /// <summary>
    /// Decodes a Base32 string into raw bytes. Ignores padding ('='), whitespace, and hyphens, and is case-insensitive, since secrets are often copy/pasted with inconsistent formatting.
    /// </summary>
    /// <param name="input">The Base32 string to decode</param>
    /// <returns>The decoded bytes</returns>
    /// <exception cref="FormatException">Thrown when the input string contains invalid Base32 characters.</exception>
    public static byte[] Decode(string input)
    {
        // Remove padding, whitespace, and hyphens, and convert to uppercase
        string cleaned = new string([.. input.Where(c => c != '=' && !char.IsWhiteSpace(c) && c != '-')]).ToUpperInvariant();
        if (cleaned.Length == 0)
        {
            return [];
        }

        // Calculate the number of bytes in the output
        List<byte> output = new(cleaned.Length * 5 / 8);
        int bitBuffer = 0;
        int bitsInBuffer = 0;

        // Decode each character
        foreach (char c in cleaned)
        {
            // Find the index of the character in the Base32 alphabet
            int value = Alphabet.IndexOf(c);
            if (value < 0)
            {
                throw new FormatException($"'{c}' is not a valid Base32 character.");
            }

            // Add the value to the bit buffer
            bitBuffer = (bitBuffer << 5) | value;
            bitsInBuffer += 5;

            // If we have 8 or more bits in the buffer, extract a byte
            if (bitsInBuffer >= 8)
            {
                bitsInBuffer -= 8;
                output.Add((byte)((bitBuffer >> bitsInBuffer) & 0xFF));
            }
        }

        // Return the output as a byte array
        return [.. output];
    }
}