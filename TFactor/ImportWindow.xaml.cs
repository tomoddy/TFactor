using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Win32;
using TFactor.Models;
using TFactor.Properties;
using TFactor.Services;
using TFactor.ViewModels;

namespace TFactor;

/// <summary>
/// Dialog for importing accounts from a screenshot of a QR code - either a single account's "otpauth://" code, or Google Authenticator's bulk "otpauth-migration://" export code. Since a large export can be split across several QR codes, the user can choose multiple screenshots in turn and their accounts accumulate below.
/// </summary>
public partial class ImportWindow : Window
{
    /// <summary>
    /// Accounts found across all screenshots chosen so far, for the user to pick which ones to import.
    /// </summary>
    private readonly ObservableCollection<ImportRowViewModel> _candidates = [];

    /// <summary>
    /// Secrets of accounts already saved in the app, so accounts the user has already added can be skipped rather than imported as duplicates.
    /// </summary>
    private readonly HashSet<string> _existingSecrets;

    /// <summary>
    /// The accounts the user selected to import, populated once the dialog closes successfully.
    /// </summary>
    public List<Account> ImportedAccounts { get; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportWindow"/> class.
    /// </summary>
    /// <param name="existingSecrets">Secrets of accounts already saved in the app, used to skip accounts that have already been added.</param>
    public ImportWindow(IEnumerable<string> existingSecrets)
    {
        InitializeComponent();
        CandidateList.ItemsSource = _candidates;
        _existingSecrets = [.. existingSecrets];
    }

    /// <summary>
    /// Lets the user pick an image file, decodes any QR code in it, and adds any accounts found to the candidate list. Can be called more than once to add accounts from multiple screenshots (e.g. a multi-part Google Authenticator export).
    /// </summary>
    private void ChooseFile_Click(object sender, RoutedEventArgs e)
    {
        // Let the user pick a file
        OpenFileDialog dialog = new()
        {
            Filter = Strings.ImportWindow_FileDialogFilter,
            Title = Strings.ImportWindow_FileDialogTitle
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }
        HideError();

        // Decode the QR code from the image
        string? decodedText = QRImageDecoder.DecodeFromFile(dialog.FileName);
        if (decodedText is null)
        {
            ShowError(Strings.ImportWindow_NoQrCodeFoundError);
            return;
        }

        try
        {
            AddCandidates(decodedText);
        }
        catch (FormatException)
        {
            ShowError(Strings.ImportWindow_UnrecognizedQrCodeError);
        }
    }

    /// <summary>
    /// Parses the QR code's decoded text and merges any newly found accounts into the candidate list, skipping ones already queued in this dialog (matched by secret) and ones already saved in the app. Also surfaces a status message when a Google Authenticator export spans multiple QR codes and/or when accounts were skipped as already-saved duplicates.
    /// </summary>
    /// <param name="decodedText">The raw text decoded from the QR code</param>
    /// <exception cref="FormatException">Thrown when the text is neither a recognized migration URI nor an otpauth URI.</exception>
    private void AddCandidates(string decodedText)
    {
        // Try to parse as a Google Authenticator migration export
        if (!Uri.TryCreate(decodedText, UriKind.Absolute, out Uri? uri))
        {
            throw new FormatException("Decoded QR text is not a URI.");
        }

        List<Account> accounts;
        string? multiPartStatus = null;

        if (uri.Scheme == "otpauth-migration")
        {
            // Google's bulk export - may be one of several QR codes if the export was large
            MigrationBatch batch = GoogleAuthMigration.Parse(decodedText);
            accounts = batch.Accounts;

            if (batch.IsMultiPart)
            {
                multiPartStatus = string.Format(Strings.ImportWindow_MultiPartStatus, batch.BatchSize, batch.BatchIndex + 1);
            }
        }
        else if (uri.Scheme == "otpauth")
        {
            accounts = [OtpAuthUri.Parse(decodedText)];
        }
        else
        {
            throw new FormatException($"Unrecognized URI scheme: {uri.Scheme}");
        }

        if (accounts.Count == 0)
        {
            ShowError(Strings.ImportWindow_NoAccountsFoundError);
            return;
        }

        // Skip accounts already queued in this dialog (e.g. the same screenshot chosen twice, or overlapping QR parts), and separately count ones already saved in the app so we can tell the user why they weren't added
        HashSet<string> candidateSecrets = [.. _candidates.Select(c => c.Account.Secret)];
        int alreadySavedCount = 0;
        foreach (Account account in accounts)
        {
            if (!candidateSecrets.Add(account.Secret))
            {
                continue;
            }

            if (_existingSecrets.Contains(account.Secret))
            {
                alreadySavedCount++;
                continue;
            }

            _candidates.Add(new ImportRowViewModel(account));
        }

        ImportButton.IsEnabled = _candidates.Count > 0;
        ChooseFileButton.ToolTip = Strings.ImportWindow_ChooseAnotherScreenshotTooltip;

        // Show whichever status message(s) apply - both can happen at once (a multi-part export that also contains an already-saved account)
        string? duplicateStatus = alreadySavedCount > 0 ? string.Format(Strings.ImportWindow_DuplicateSkippedStatus, alreadySavedCount) : null;
        string? combinedStatus = (multiPartStatus, duplicateStatus) switch
        {
            (not null, not null) => $"{multiPartStatus} {duplicateStatus}",
            (not null, null) => multiPartStatus,
            (null, not null) => duplicateStatus,
            _ => null
        };
        if (combinedStatus is not null)
        {
            ShowStatus(combinedStatus);
        }
    }

    /// <summary>
    /// Adds the checked accounts to ImportedAccounts and closes the dialog with a successful result.
    /// </summary>
    private void Import_Click(object sender, RoutedEventArgs e)
    {
        ImportedAccounts.AddRange(_candidates.Where(c => c.IsSelected).Select(c => c.Account));
        DialogResult = true;
    }

    /// <summary>
    /// Closes the dialog without importing anything.
    /// </summary>
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    /// <summary>
    /// Shows an error message above the account list.
    /// </summary>
    /// <param name="message">The message to display</param>
    private void ShowError(string message)
    {
        StatusTextBlock.Visibility = Visibility.Collapsed;
        ErrorTextBlock.Text = message;
        ErrorTextBlock.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Hides the error message.
    /// </summary>
    private void HideError()
    {
        ErrorTextBlock.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Shows an informational status message above the account list, e.g. about multi-part exports.
    /// </summary>
    /// <param name="message">The message to display</param>
    private void ShowStatus(string message)
    {
        StatusTextBlock.Text = message;
        StatusTextBlock.Visibility = Visibility.Visible;
    }
}