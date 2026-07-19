namespace TFactor.Models;

/// <summary>
/// A single 2FA account: the details needed to generate a rotating TOTP code.
/// </summary>
public class Account
{
    /// <summary>
    /// Stable identifier so we can edit/delete a specific account.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// e.g. "Google", "GitHub". Shown as the primary label in the UI.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// e.g. the email/username tied to the account.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Optional free-text tag used to group accounts in the main list. Empty means untagged.
    /// </summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// Base32-encoded shared secret (as given by the service / QR code).
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// HMAC algorithm used to generate codes. Google Authenticator almost always uses SHA1.
    /// </summary>
    public TotpAlgorithm Algorithm { get; set; } = TotpAlgorithm.SHA1;

    /// <summary>
    /// Number of digits in the generated code (usually 6, sometimes 8).
    /// </summary>
    public int Digits { get; set; } = 6;

    /// <summary>
    /// How often the code rotates, in seconds (almost always 30).
    /// </summary>
    public int Period { get; set; } = 30;
}