using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TFactor.Services;

/// <summary>
/// Persists the user's chosen display order for tags, encrypted at rest with Windows DPAPI like the accounts
/// themselves. "Untagged" is never stored here - it always sorts first and isn't reorderable.
/// </summary>
public static class TagOrderStorage
{
    /// <summary>
    /// Storage location for the encrypted tag order folder. This is a hidden file in the user's AppData folder.
    /// </summary>
    private static readonly string StorageDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TFactor");

    /// <summary>
    /// Storage location for the encrypted tag order file. This is a hidden file in the user's AppData folder.
    /// </summary>
    private static readonly string StorageFilePath = Path.Combine(StorageDirectory, "tags.dat");

    /// <summary>
    /// Loads the saved tag order from disk. Returns an empty list if no order has been saved yet.
    /// </summary>
    /// <returns>The saved tag order</returns>
    public static List<string> Load()
    {
        if (!File.Exists(StorageFilePath))
        {
            return [];
        }

        byte[] encrypted = File.ReadAllBytes(StorageFilePath);
        byte[] decrypted = ProtectedData.Unprotect(encrypted, optionalEntropy: null, DataProtectionScope.CurrentUser);

        string json = Encoding.UTF8.GetString(decrypted);
        return JsonSerializer.Deserialize<List<string>>(json) ?? [];
    }

    /// <summary>
    /// Saves the given tag order to disk, encrypted with DPAPI. Overwrites any previously saved order.
    /// </summary>
    /// <param name="tagOrder">The tag order to save</param>
    public static void Save(IEnumerable<string> tagOrder)
    {
        Directory.CreateDirectory(StorageDirectory);

        string json = JsonSerializer.Serialize(tagOrder.ToList());
        byte[] plaintext = Encoding.UTF8.GetBytes(json);
        byte[] encrypted = ProtectedData.Protect(plaintext, optionalEntropy: null, DataProtectionScope.CurrentUser);

        File.WriteAllBytes(StorageFilePath, encrypted);
    }

    /// <summary>
    /// Combines a saved tag order with the tags actually in use right now: saved tags no longer used by any account are dropped, and any in-use tags missing from the saved order are appended at the end (alphabetically among themselves) so newly-typed tags show up somewhere sensible until reordered.
    /// </summary>
    /// <param name="savedOrder">The previously saved tag order</param>
    /// <param name="tagsInUse">The distinct, non-empty tags currently assigned to at least one account</param>
    /// <returns>The reconciled tag order</returns>
    public static List<string> Reconcile(IEnumerable<string> savedOrder, IEnumerable<string> tagsInUse)
    {
        HashSet<string> inUse = [.. tagsInUse];
        List<string> reconciled = [.. savedOrder.Where(inUse.Contains)];
        IEnumerable<string> missing = inUse.Except(reconciled).OrderBy(t => t, StringComparer.CurrentCultureIgnoreCase);
        reconciled.AddRange(missing);
        return reconciled;
    }
}