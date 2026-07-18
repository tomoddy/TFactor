using System.Text;
using System.Web;
using TFactor.Models;

namespace TFactor.Services;

/// <summary>
/// Parses the "otpauth-migration://" payload produced by Google Authenticator's "Transfer accounts" / "Export accounts" QR code. The payload is a URL-safe base64-encoded protobuf message containing every account in the export in one go.
/// </summary>
public static class GoogleAuthMigration
{
    /// <summary>
    /// Parses an "otpauth-migration://offline?data=..." URI into a batch of accounts. When a Google Authenticator export has too many accounts for one QR code, it's split across several QR codes ("batches") - the returned BatchIndex/BatchSize tell the caller whether this is one of several and more screenshots are needed.
    /// </summary>
    /// <param name="migrationUri">The full URI decoded from the export QR code</param>
    /// <returns>The accounts found in this batch, plus which batch this is out of how many</returns>
    /// <exception cref="FormatException">Thrown when the URI is not a valid migration payload.</exception>
    public static MigrationBatch Parse(string migrationUri)
    {
        // Pull the "data" query parameter out of the otpauth-migration:// URI
        Uri uri = new(migrationUri);
        string? encodedPayload = HttpUtility.ParseQueryString(uri.Query)["data"];
        if (string.IsNullOrEmpty(encodedPayload))
        {
            throw new FormatException("Migration URI is missing its 'data' parameter.");
        }

        // The parameter is standard base64, just URL-encoded, so a plain Convert.FromBase64String works
        byte[] payload = Convert.FromBase64String(encodedPayload);
        return ParsePayload(payload);
    }

    /// <summary>
    /// Parses the top-level MigrationPayload protobuf message, extracting each repeated OtpParameters entry along with the batch_index/batch_size fields used when an export spans multiple QR codes.
    /// </summary>
    /// <param name="payload">The raw protobuf bytes</param>
    /// <returns>The accounts found in this batch, plus which batch this is out of how many</returns>
    private static MigrationBatch ParsePayload(byte[] payload)
    {
        // Loop through the payload, reading each field tag and dispatching to the appropriate handler. batch_size defaults to 1 (single QR code export) if the field is absent.
        List<Account> accounts = [];
        int batchIndex = 0;
        int batchSize = 1;
        int pos = 0;
        while (pos < payload.Length)
        {
            // Read the next field tag (field number + wire type)
            (int fieldNumber, int wireType) = ProtoReader.ReadTag(payload, ref pos);

            switch (fieldNumber)
            {
                case 1 when wireType == ProtoReader.WireTypeLengthDelimited: // repeated otp_parameters
                    byte[] entry = ProtoReader.ReadLengthDelimited(payload, ref pos);
                    accounts.Add(ParseOtpParameters(entry));
                    break;

                case 3: // batch_size - how many QR codes the full export was split across
                    batchSize = (int)ProtoReader.ReadVarint(payload, ref pos);
                    break;

                case 4: // batch_index - which of those QR codes this one is (0-based)
                    batchIndex = (int)ProtoReader.ReadVarint(payload, ref pos);
                    break;

                default:
                    ProtoReader.SkipField(payload, ref pos, wireType);
                    break;
            }
        }

        // Return the accounts found in this batch, plus the batch position
        return new MigrationBatch(accounts, batchIndex, batchSize);
    }

    /// <summary>
    /// Parses a single OtpParameters protobuf message into an Account.
    /// </summary>
    /// <param name="entry">The raw protobuf bytes for one OtpParameters message</param>
    /// <returns>The decoded account</returns>
    private static Account ParseOtpParameters(byte[] entry)
    {
        // Loop through the entry, reading each field tag and dispatching to the appropriate handler
        Account account = new();
        int pos = 0;
        while (pos < entry.Length)
        {
            // Read the next field tag (field number + wire type)
            (int fieldNumber, int wireType) = ProtoReader.ReadTag(entry, ref pos);

            // Switch on the field number to determine which property to set
            switch (fieldNumber)
            {
                case 1: // secret (bytes) - needs to be re-encoded as Base32 for our Account model
                    byte[] secretBytes = ProtoReader.ReadLengthDelimited(entry, ref pos);
                    account.Secret = Base32Encode(secretBytes);
                    break;

                case 2: // name (string) - Google's export uses this as the account label
                    account.Label = Encoding.UTF8.GetString(ProtoReader.ReadLengthDelimited(entry, ref pos));
                    break;

                case 3: // issuer (string)
                    account.Issuer = Encoding.UTF8.GetString(ProtoReader.ReadLengthDelimited(entry, ref pos));
                    break;

                case 4: // algorithm (enum)
                    account.Algorithm = MapAlgorithm((int)ProtoReader.ReadVarint(entry, ref pos));
                    break;

                case 5: // digits (enum)
                    account.Digits = MapDigitCount((int)ProtoReader.ReadVarint(entry, ref pos));
                    break;

                default:
                    ProtoReader.SkipField(entry, ref pos, wireType);
                    break;
            }
        }

        // Return the fully populated account
        return account;
    }

    /// <summary>
    /// Maps Google's Algorithm enum (field 4 of OtpParameters) to our TotpAlgorithm.
    /// </summary>
    /// <param name="value">The raw enum value from the protobuf</param>
    /// <returns>The corresponding TotpAlgorithm, defaulting to SHA1 for unrecognized values</returns>
    private static TotpAlgorithm MapAlgorithm(int value) => value switch
    {
        2 => TotpAlgorithm.SHA256,
        3 => TotpAlgorithm.SHA512,
        _ => TotpAlgorithm.SHA1
    };

    /// <summary>
    /// Maps Google's DigitCount enum (field 5 of OtpParameters) to a digit count.
    /// </summary>
    /// <param name="value">The raw enum value from the protobuf</param>
    /// <returns>The corresponding digit count, defaulting to 6 for unrecognized values</returns>
    private static int MapDigitCount(int value) => value switch
    {
        2 => 8,
        _ => 6
    };

    /// <summary>
    /// Encodes raw bytes as an RFC 4648 Base32 string, for storage in our Account model.
    /// </summary>
    /// <param name="data">The bytes to encode</param>
    /// <returns>The Base32-encoded string, without padding</returns>
    private static string Base32Encode(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        StringBuilder result = new((data.Length * 8 + 4) / 5);
        int bitBuffer = 0;
        int bitsInBuffer = 0;

        // Pack bits 5 at a time into Base32 characters
        foreach (byte b in data)
        {
            bitBuffer = (bitBuffer << 8) | b;
            bitsInBuffer += 8;
            while (bitsInBuffer >= 5)
            {
                bitsInBuffer -= 5;
                result.Append(alphabet[(bitBuffer >> bitsInBuffer) & 0x1F]);
            }
        }

        // Flush any remaining bits, padded with zeros
        if (bitsInBuffer > 0)
        {
            result.Append(alphabet[(bitBuffer << (5 - bitsInBuffer)) & 0x1F]);
        }

        // Return the final Base32 string
        return result.ToString();
    }
}