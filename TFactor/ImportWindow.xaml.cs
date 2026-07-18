using System.Windows;
using Microsoft.Win32;
using TFactor.Models;
using TFactor.Services;
using TFactor.ViewModels;

namespace TFactor;

/// <summary>
/// Dialog for importing accounts from a screenshot of a QR code - either a single account's "otpauth://" code, or Google Authenticator's bulk "otpauth-migration://" export code.
/// </summary>
public partial class ImportWindow : Window
{
    /// <summary>
    /// List of possible accounts found in the screenshot, to be displayed in the UI for the user to choose which ones to import.
    /// </summary>
    private List<ImportRowViewModel> _candidates = [];

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
    }

    /// <summary>
    /// Lets the user pick an image file, decodes any QR code in it, and shows the accounts found for the user to choose from.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The event arguments.</param>
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

        // Parse the decoded text into accounts
        try
        {
            List<Account> accounts = ParseDecodedText(decodedText);
            if (accounts.Count == 0)
            {
                ShowError("No accounts were found in that QR code.");
                return;
            }

            // Add the accounts to the list of candidates for the user to choose from
            _candidates = [.. accounts.Select(a => new ImportRowViewModel(a))];
            CandidateList.ItemsSource = _candidates;
            ImportButton.IsEnabled = true;
        }
        catch (FormatException)
        {
            ShowError("That QR code isn't a recognized authenticator export or account code.");
        }
    }

    /// <summary>
    /// Parses the QR code's decoded text as either a Google Authenticator migration export (many accounts) or a single otpauth:// account.
    /// </summary>
    /// <param name="decodedText">The raw text decoded from the QR code</param>
    /// <returns>The accounts found</returns>
    /// <exception cref="FormatException">Thrown when the text is neither a recognized migration URI nor an otpauth URI.</exception>
    private static List<Account> ParseDecodedText(string decodedText)
    {
        // Try to parse as a Google Authenticator migration export
        if (!Uri.TryCreate(decodedText, UriKind.Absolute, out Uri? uri))
        {
            throw new FormatException("Decoded QR text is not a URI.");
        }

        // Parse depending on the scheme
        return uri.Scheme switch
        {
            "otpauth-migration" => GoogleAuthMigration.Parse(decodedText),
            "otpauth" => [OtpAuthUri.Parse(decodedText)],
            _ => throw new FormatException($"Unrecognized URI scheme: {uri.Scheme}")
        };
    }

    /// <summary>
    /// Adds the checked accounts to ImportedAccounts and closes the dialog with a successful result.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The event arguments.</param>
    private void Import_Click(object sender, RoutedEventArgs e)
    {
        ImportedAccounts.AddRange(_candidates.Where(c => c.IsSelected).Select(c => c.Account));
        DialogResult = true;
    }

    /// <summary>
    /// Closes the dialog without importing anything.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The event arguments.</param>
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
}