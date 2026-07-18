using TFactor.Models;

namespace TFactor.ViewModels;

/// <summary>
/// Wraps an Account found in an imported QR code, with a checkbox state for the user to choose whether to actually import it.
/// </summary>
/// <param name="account">The account to display</param>
public class ImportRowViewModel(Account account)
{
    /// <summary>
    /// The account this row represents.
    /// </summary>
    public Account Account { get; } = account;

    /// <summary>
    /// Whether the user has this account checked for import. Defaults to selected.
    /// </summary>
    public bool IsSelected { get; set; } = true;

    /// <summary>
    /// The service/site name, e.g. "Google".
    /// </summary>
    public string Issuer => string.IsNullOrWhiteSpace(Account.Issuer) ? "(Unknown)" : Account.Issuer;

    /// <summary>
    /// The account label, e.g. an email address.
    /// </summary>
    public string Label => Account.Label;
}