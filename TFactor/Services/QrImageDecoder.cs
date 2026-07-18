using System.Drawing;
using ZXing;
using ZXing.Windows.Compatibility;

namespace TFactor.Services;

/// <summary>
/// Reads the raw text encoded in a QR code from an image file, e.g. a screenshot of Google Authenticator's export QR code.
/// </summary>
public static class QRImageDecoder
{
    /// <summary>
    /// Attempts to decode a QR code from the given image file.
    /// </summary>
    /// <param name="imagePath">Path to the image file (PNG, JPG, etc.)</param>
    /// <returns>The decoded text, or null if no QR code was found in the image</returns>
    public static string? DecodeFromFile(string imagePath)
    {
        // Load the image file into a Bitmap that ZXing's reader can scan
        using Bitmap bitmap = new(imagePath);

        // Create a ZXing barcode reader configured to look for QR codes
        BarcodeReader reader = new()
        {
            AutoRotate = true,
            Options = { TryHarder = true, PossibleFormats = [BarcodeFormat.QR_CODE] }
        };

        // Attempt to decode the QR code from the bitmap and return the text if successful
        Result? result = reader.Decode(bitmap);
        return result?.Text;
    }
}
