using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TFactor.Models;

namespace TFactor.Services;

/// <summary>
/// Persists accounts to disk, encrypted at rest with Windows DPAPI. Because DPAPI keys are scoped to the current Windows user, only whoever is logged into this Windows account can decrypt the file - there is no separate password to manage.
/// </summary>
public static class SecureStorage
{
    /// <summary>
    /// Storage location for the encrypted accounts folder. This is a hidden file in the user's AppData folder.
    /// </summary>
    private static readonly string StorageDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TFactor");

    /// <summary>
    /// Storage location for the encrypted accounts file. This is a hidden file in the user's AppData folder.
    /// </summary>
    private static readonly string StorageFilePath = Path.Combine(StorageDirectory, "accounts.dat");

    /// <summary>
    /// Loads the saved accounts from disk. Returns an empty list if no accounts have been saved yet.
    /// </summary>
    /// <returns>List of accounts</returns>
    public static List<Account> Load()
    {
        // If the file doesn't exist, return an empty list
        if (!File.Exists(StorageFilePath))
        {
            return [];
        }

        // Read the encrypted bytes and decrypt them with DPAPI, scoped to the current user
        byte[] encrypted = File.ReadAllBytes(StorageFilePath);
        byte[] decrypted = ProtectedData.Unprotect(encrypted, optionalEntropy: null, DataProtectionScope.CurrentUser);

        // Deserialize the decrypted JSON into a list of accounts
        string json = Encoding.UTF8.GetString(decrypted);
        return JsonSerializer.Deserialize<List<Account>>(json) ?? [];
    }

    /// <summary>
    /// Saves the given accounts to disk, encrypted with DPAPI. Overwrites any previously saved accounts.
    /// </summary>
    /// <param name="accounts">The accounts to save</param>
    public static void Save(IEnumerable<Account> accounts)
    {
        // Ensure the storage directory exists
        Directory.CreateDirectory(StorageDirectory);

        // Serialize to JSON, then encrypt with DPAPI, scoped to the current user
        string json = JsonSerializer.Serialize(accounts.ToList());
        byte[] plaintext = Encoding.UTF8.GetBytes(json);
        byte[] encrypted = ProtectedData.Protect(plaintext, optionalEntropy: null, DataProtectionScope.CurrentUser);

        // Write the encrypted bytes to disk
        File.WriteAllBytes(StorageFilePath, encrypted);
    }
}