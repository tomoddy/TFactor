using System.Windows;
using System.Windows.Controls;
using TFactor.Models;
using TFactor.Properties;
using TFactor.Services;

namespace TFactor;

/// <summary>
/// Dialog for manually adding a single account by typing in its secret key and details.
/// </summary>
public partial class AddAccountWindow : Window
{
    /// <summary>
    /// The account created when the user clicks Save, or null if the dialog was cancelled.
    /// </summary>
    public Account? CreatedAccount { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AddAccountWindow"/> class.
    /// </summary>
    public AddAccountWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Validates the form, builds the Account, and closes the dialog with a successful result.
    /// </summary>
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // Get the secret key from the input field and validate it
        string secret = SecretTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(secret))
        {
            ShowError(Strings.AddAccountWindow_MissingSecretError);
            return;
        }

        // Make sure the secret is actually valid Base32 before we accept it
        try
        {
            Base32.Decode(secret);
        }
        catch (FormatException)
        {
            ShowError(Strings.AddAccountWindow_InvalidSecretError);
            return;
        }

        // Get the account information from the input fields. The combo box item content matches the enum names / raw digit counts exactly, so no custom mapping is needed.
        string algorithmText = (string)((ComboBoxItem)AlgorithmComboBox.SelectedItem).Content;
        string digitsText = (string)((ComboBoxItem)DigitsComboBox.SelectedItem).Content;

        // Build the account object
        CreatedAccount = new Account
        {
            Issuer = IssuerTextBox.Text.Trim(),
            Label = LabelTextBox.Text.Trim(),
            Tag = TagTextBox.Text.Trim(),
            Secret = secret,
            Algorithm = Enum.Parse<TotpAlgorithm>(algorithmText),
            Digits = int.Parse(digitsText)
        };
        DialogResult = true;
    }

    /// <summary>
    /// Closes the dialog without creating an account.
    /// </summary>
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    /// <summary>
    /// Shows a validation error message under the form fields.
    /// </summary>
    /// <param name="message">The message to display</param>
    private void ShowError(string message)
    {
        ErrorTextBlock.Text = message;
        ErrorTextBlock.Visibility = Visibility.Visible;
    }
}