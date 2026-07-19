using System.Windows;
using TFactor.Models;
using TFactor.Properties;

namespace TFactor;

/// <summary>
/// Dialog for editing an account's display details (issuer/label), or removing it entirely. Edits are applied directly to the given Account instance when saved.
/// </summary>
public partial class EditAccountWindow : Window
{
    /// <summary>
    /// Account instance to edit. Edits are applied directly to this instance when saved.
    /// </summary>
    private readonly Account _account;

    /// <summary>
    /// Whether the user chose to remove the account rather than save changes to it.
    /// </summary>
    public bool WasRemoved { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EditAccountWindow"/> class.
    /// </summary>
    /// <param name="account">The account being edited</param>
    public EditAccountWindow(Account account)
    {
        InitializeComponent();
        _account = account;
        IssuerTextBox.Text = account.Issuer;
        LabelTextBox.Text = account.Label;
        TagTextBox.Text = account.Tag;
    }

    /// <summary>
    /// Applies the edited issuer/label to the account and closes the dialog with a successful result.
    /// </summary>
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        string issuer = IssuerTextBox.Text.Trim();
        string label = LabelTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(issuer) && string.IsNullOrWhiteSpace(label))
        {
            ShowError(Strings.EditAccountWindow_MissingIssuerOrLabelError);
            return;
        }
        _account.Issuer = issuer;
        _account.Label = label;
        _account.Tag = TagTextBox.Text.Trim();
        DialogResult = true;
    }

    /// <summary>
    /// Confirms with the user, then marks the account for removal and closes the dialog with a successful result.
    /// </summary>
    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        string name = string.IsNullOrWhiteSpace(_account.Issuer) ? _account.Label : _account.Issuer;
        MessageBoxResult confirmation = MessageBox.Show(this, string.Format(Strings.EditAccountWindow_RemoveConfirmMessage, name), Strings.EditAccountWindow_RemoveConfirmTitle, MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirmation != MessageBoxResult.Yes)
        {
            return;
        }
        WasRemoved = true;
        DialogResult = true;
    }

    /// <summary>
    /// Closes the dialog without saving changes or removing the account.
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