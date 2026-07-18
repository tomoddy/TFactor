using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Win32;
using TFactor.Models;
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
    /// The accounts the user selected to import, populated once the dialog closes successfully.
    /// </summary>
    public List<Account> ImportedAccounts { get; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportWindow"/> class.
    /// </summary>
    public ImportWindow()
    {
        InitializeComponent();
        CandidateList.ItemsSource = _candidates;
    }

    /// <summary>
    /// Lets the user pick an image file, decodes any QR code in it, and adds any accounts found to the candidate list. Can be called more than once to add accounts from multiple screenshots (e.g. a multi-part Google Authenticator export).
    /// </summary>
    private void ChooseFile_Click(object sender, RoutedEventArgs e)
    {
        // Let the user pick a file
        OpenFileDialog dialog = new()
        {
            Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*",
            Title = "Choose a QR code screenshot"
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
            ShowError("No QR code was found in that image.");
            return;
        }

        try
        {
            AddCandidates(decodedText);
        }
        catch (FormatException)
        {
            ShowError("That QR code isn't a recognized authenticator export or account code.");
        }
    }

    /// <summary>
    /// Parses the QR code's decoded text and merges any newly found accounts into the candidate list, skipping ones already present (matched by secret). Also surfaces a status message when a Google Authenticator export spans multiple QR codes.
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

        if (uri.Scheme == "otpauth-migration")
        {
            // Google's bulk export - may be one of several QR codes if the export was large
            MigrationBatch batch = GoogleAuthMigration.Parse(decodedText);
            accounts = batch.Accounts;

            if (batch.IsMultiPart)
            {
                ShowStatus($"This export is split across {batch.BatchSize} QR codes (this is part {batch.BatchIndex + 1}). Choose the next screenshot to add the rest, or click Import to finish with what you have so far.");
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
            ShowError("No accounts were found in that QR code.");
            return;
        }

        // Skip any accounts already in the candidate list (e.g. if the same screenshot is chosen twice)
        HashSet<string> existingSecrets = [.. _candidates.Select(c => c.Account.Secret)];
        foreach (Account account in accounts.Where(a => !existingSecrets.Contains(a.Secret)))
        {
            _candidates.Add(new ImportRowViewModel(account));
        }

        ImportButton.IsEnabled = _candidates.Count > 0;
        ChooseFileButton.ToolTip = "Choose Another Screenshot...";
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