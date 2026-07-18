using System.Security.Cryptography;
using TFactor.Models;

namespace TFactor.Services;

/// <summary>
/// Generates Time-based One-Time Passwords per RFC 6238, built on top of the
/// HOTP algorithm from RFC 4226.
/// </summary>
public static class Totp
{
    /// <summary>
    /// Generates the current TOTP code for the given account.
    /// </summary>
    public static string GenerateCode(Account account, DateTimeOffset? at = null)
    {
        DateTimeOffset timestamp = at ?? DateTimeOffset.UtcNow;
        long counter = timestamp.ToUnixTimeSeconds() / account.Period;
        return GenerateCode(account, counter);
    }

    /// <summary>
    /// Number of seconds remaining until the current code rotates.
    /// </summary>
    public static int GetSecondsRemaining(Account account, DateTimeOffset? at = null)
    {
        DateTimeOffset timestamp = at ?? DateTimeOffset.UtcNow;
        long secondsIntoPeriod = timestamp.ToUnixTimeSeconds() % account.Period;
        return (int)(account.Period - secondsIntoPeriod);
    }

    /// <summary>
    /// Generates the TOTP code for the given account at a specific counter value.
    /// </summary>
    /// <param name="account">The account for which to generate the code</param>
    /// <param name="counter">The counter value</param>
    /// <returns>The generated TOTP code</returns>
    private static string GenerateCode(Account account, long counter)
    {
        // Decode the Base32 secret into bytes
        byte[] key = Base32.Decode(account.Secret);

        // Convert the counter to an 8-byte array (big-endian), reverse if little-endian
        byte[] counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(counterBytes);
        }

        // Compute the HMAC hash using the specified algorithm
        byte[] hash = ComputeHmac(account.Algorithm, key, counterBytes);

        // Dynamic truncation, per RFC 4226 section 5.3
        int offset = hash[^1] & 0x0F;
        int binaryCode = ((hash[offset] & 0x7F) << 24) | ((hash[offset + 1] & 0xFF) << 16) | ((hash[offset + 2] & 0xFF) << 8) | (hash[offset + 3] & 0xFF);

        // Compute the OTP value and return it as a zero-padded string
        int truncated = binaryCode % (int)Math.Pow(10, account.Digits);
        return truncated.ToString().PadLeft(account.Digits, '0');
    }

    /// <summary>
    /// Computes the HMAC hash using the specified algorithm
    /// </summary>
    /// <param name="algorithm">The HMAC algorithm to use</param>
    /// <param name="key">The secret key</param>
    /// <param name="message">The message to hash</param>
    /// <returns>The HMAC hash</returns>
    /// <exception cref="NotSupportedException"></exception>
    private static byte[] ComputeHmac(TotpAlgorithm algorithm, byte[] key, byte[] message)
    {
        return algorithm switch
        {
            TotpAlgorithm.SHA1 => new HMACSHA1(key).ComputeHash(message),
            TotpAlgorithm.SHA256 => new HMACSHA256(key).ComputeHash(message),
            TotpAlgorithm.SHA512 => new HMACSHA512(key).ComputeHash(message),
            _ => throw new NotSupportedException($"Unsupported TOTP algorithm: {algorithm}")
        };
    }
}