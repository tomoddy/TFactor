using Windows.Security.Credentials.UI;

namespace TFactor.Services;

/// <summary>
/// Prompts the user to verify their identity with Windows Hello (face, fingerprint, or PIN - whatever they have enrolled) before letting them into the app.
/// </summary>
public static class WindowsHelloAuth
{
    /// <summary>
    /// Shows the Windows Hello verification prompt and waits for the result. If Windows Hello isn't set up on this device, or the platform API is unavailable, this fails open (returns true) rather than locking the user out of their own machine - DPAPI-encrypted storage is still tied to the current Windows login regardless.
    /// </summary>
    /// <param name="message">The message shown in the Windows Hello prompt</param>
    /// <returns>True if the user verified successfully or verification couldn't be enforced; false if they cancelled or failed</returns>
    public static async Task<bool> VerifyAsync(string message)
    {
        try
        {
            UserConsentVerifierAvailability availability = await UserConsentVerifier.CheckAvailabilityAsync();
            if (availability != UserConsentVerifierAvailability.Available)
            {
                // Nothing enrolled to verify against on this device - nothing to gate on
                return true;
            }

            UserConsentVerificationResult result = await UserConsentVerifier.RequestVerificationAsync(message);
            return result == UserConsentVerificationResult.Verified;
        }
        catch (Exception)
        {
            // If the Windows Hello API itself is unavailable for some reason, don't lock the user out
            return true;
        }
    }
}