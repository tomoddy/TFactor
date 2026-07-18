using System.Collections.Specialized;
using System.Web;
using TFactor.Models;

namespace TFactor.Services;

/// <summary>
/// Parses a standard "otpauth://" URI, the format most services use for a single-account QR code (as opposed to Google's bulk "otpauth-migration://" export).
/// </summary>
public static class OtpAuthUri
{
    /// <summary>
    /// Parses an "otpauth://totp/Label?secret=...&amp;issuer=...&amp;algorithm=...&amp;digits=...&amp;period=..." URI into an Account.
    /// </summary>
    /// <param name="otpAuthUri">The URI decoded from a QR code</param>
    /// <returns>The decoded account</returns>
    /// <exception cref="FormatException">Thrown when the URI is not a valid otpauth URI, or is missing its secret.</exception>
    public static Account Parse(string otpAuthUri)
    {
        // Parse the URI
        Uri uri = new(otpAuthUri);
        if (uri.Scheme != "otpauth")
        {
            throw new FormatException($"'{otpAuthUri}' is not an otpauth:// URI.");
        }

        // The query string carries the secret and the optional algorithm/digits/period/issuer overrides
        NameValueCollection query = HttpUtility.ParseQueryString(uri.Query);
        string? secret = query["secret"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new FormatException("otpauth:// URI is missing its 'secret' parameter.");
        }

        // The label is the URI path, conventionally formatted as "Issuer:account" or just "account"
        string label = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
        string issuer = query["issuer"] ?? string.Empty;

        // Split the label into issuer and account if it contains a colon
        if (label.Contains(':'))
        {
            string[] parts = label.Split(':', 2);
            issuer = string.IsNullOrEmpty(issuer) ? parts[0] : issuer;
            label = parts[1];
        }

        // The "algorithm" parameter's values match the TotpAlgorithm names exactly, so Enum.TryParse handles it directly - defaulting to SHA1 (the otpauth spec's default) when absent or unrecognized
        TotpAlgorithm algorithm = Enum.TryParse(query["algorithm"], ignoreCase: true, out TotpAlgorithm parsedAlgorithm) ? parsedAlgorithm : TotpAlgorithm.SHA1;

        // Return the account object
        return new Account
        {
            Issuer = issuer,
            Label = label,
            Secret = secret,
            Algorithm = algorithm,
            Digits = int.TryParse(query["digits"], out int digits) ? digits : 6,
            Period = int.TryParse(query["period"], out int period) ? period : 30
        };
    }
}