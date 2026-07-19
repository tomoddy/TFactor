using System.ComponentModel;
using TFactor.Models;
using TFactor.Properties;
using TFactor.Services;

namespace TFactor.ViewModels;

/// <summary>
/// Wraps an Account for display in the main list, exposing a live TOTP code and countdown that the UI can bind to and refresh on a timer.
/// </summary>
public class AccountRowViewModel : INotifyPropertyChanged
{
    /// <summary>
    /// The current TOTP code for this account.
    /// </summary>
    private string _code = string.Empty;

    /// <summary>
    /// The number of seconds remaining until the current TOTP code expires.
    /// </summary>
    private int _secondsRemaining;

    /// <summary>
    /// The underlying account this row represents.
    /// </summary>
    public Account Account { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountRowViewModel"/> class.
    /// </summary>
    /// <param name="account">The account to display</param>
    public AccountRowViewModel(Account account)
    {
        Account = account;
        Refresh();
    }

    /// <summary>
    /// The service/site name, e.g. "Google".
    /// </summary>
    public string Issuer => Account.Issuer;

    /// <summary>
    /// The account label, e.g. an email address.
    /// </summary>
    public string Label => Account.Label;

    /// <summary>
    /// The tag this account is grouped under in the main list, or the localized "Untagged" placeholder if it has none.
    /// </summary>
    public string TagGroupHeader => string.IsNullOrWhiteSpace(Account.Tag) ? Strings.Common_UntaggedLabel : Account.Tag;

    /// <summary>
    /// The current TOTP code for this account.
    /// </summary>
    public string Code
    {
        get => _code;
        private set
        {
            _code = value;
            OnPropertyChanged(nameof(Code));
        }
    }

    /// <summary>
    /// Seconds remaining before the current code rotates.
    /// </summary>
    public int SecondsRemaining
    {
        get => _secondsRemaining;
        private set
        {
            _secondsRemaining = value;
            OnPropertyChanged(nameof(SecondsRemaining));
        }
    }

    /// <summary>
    /// Recomputes the code and countdown for the current time. Call this on a timer to keep the display live.
    /// </summary>
    public void Refresh()
    {
        Code = Totp.GenerateCode(Account);
        SecondsRemaining = Totp.GetSecondsRemaining(Account);
    }

    /// <summary>
    /// Notifies the UI that the underlying account's Issuer/Label were changed elsewhere (e.g. via the edit dialog), so bound controls refresh.
    /// </summary>
    public void NotifyDetailsChanged()
    {
        OnPropertyChanged(nameof(Issuer));
        OnPropertyChanged(nameof(Label));
        OnPropertyChanged(nameof(TagGroupHeader));
    }

    /// <summary>
    /// Occurs when a property value changes, allowing the UI to update bindings.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event for a given property name.
    /// </summary>
    /// <param name="propertyName"></param>
    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}