using TFactor.Models;

namespace TFactor.Services;

/// <summary>
/// The result of parsing one Google Authenticator export QR code. When an export has too many accounts for a single QR code, Google splits it across several - BatchIndex/BatchSize tell the caller which one this is.
/// </summary>
/// <param name="Accounts">The accounts found in this QR code</param>
/// <param name="BatchIndex">Which batch this QR code is (0-based)</param>
/// <param name="BatchSize">How many QR codes the full export was split across</param>
public readonly record struct MigrationBatch(List<Account> Accounts, int BatchIndex, int BatchSize)
{
    /// <summary>
    /// Whether the export spans more than one QR code.
    /// </summary>
    public bool IsMultiPart => BatchSize > 1;
}