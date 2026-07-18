namespace TFactor.Models
{
    /// <summary>
    /// Type of HMAC algorithm used to generate TOTP codes. Google Authenticator almost always uses SHA1.
    /// </summary>
    public enum TotpAlgorithm
    {
        SHA1,
        SHA256,
        SHA512
    }
}