using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using TFactor.Models;
using TFactor.Services;
using TFactor.ViewModels;

namespace TFactor;

/// <summary>
/// The main window: shows the list of saved accounts with their live, rotating TOTP codes.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// The view model for the main window, containing the list of accounts and their TOTP codes.
    /// </summary>
    private readonly ObservableCollection<AccountRowViewModel> _rows = [];

    /// <summary>
    /// The timer that updates the TOTP codes every x seconds.
    /// </summary>
    private readonly DispatcherTimer _refreshTimer;

    /// <summary>
    /// The ScrollViewer inside AccountList's default template, lazily found the first time the user scrolls.
    /// </summary>
    private ScrollViewer? _accountListScrollViewer;

    /// <summary>
    /// The live view over the account list that SearchBox filters, layered on top of _rows without touching its actual order.
    /// </summary>
    private readonly ICollectionView _accountsView;

    /// <summary>
    /// The current tag display order (Untagged is implicit and always first, so it's not included here). Recomputed via RefreshTagOrder() whenever the set of in-use tags might have changed.
    /// </summary>
    private List<string> _tagOrder = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        // Load previously saved accounts and populate the list
        foreach (Account account in SecureStorage.Load())
        {
            _rows.Add(new AccountRowViewModel(account));
        }
        AccountList.ItemsSource = _rows;
        _accountsView = CollectionViewSource.GetDefaultView(_rows);
        _accountsView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(AccountRowViewModel.TagGroupHeader)));
        RefreshTagOrder();
        SortRows();
        UpdateEmptyMessageVisibility();

        // Refresh every account's code once a second so they rotate live in the UI
        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _refreshTimer.Tick += (_, _) => RefreshAllCodes();
        _refreshTimer.Start();
    }

    /// <summary>
    /// Recomputes the code and countdown for every visible account row.
    /// </summary>
    private void RefreshAllCodes()
    {
        foreach (AccountRowViewModel row in _rows)
        {
            row.Refresh();
        }
    }

    /// <summary>
    /// Opens the manual "Add Account" dialog and adds the result to the list.
    /// </summary>
    private void AddAccount_Click(object sender, RoutedEventArgs e)
    {
        AddAccountWindow window = new() { Owner = this };
        if (window.ShowDialog() == true && window.CreatedAccount is { } account)
        {
            _rows.Add(new AccountRowViewModel(account));
            RefreshTagOrder();
            SortRows();
            UpdateEmptyMessageVisibility();
            SaveAccounts();
        }
    }

    /// <summary>
    /// Opens the "Import from Screenshot" dialog and adds any accounts the user chose to import.
    /// </summary>
    private void ImportFromScreenshot_Click(object sender, RoutedEventArgs e)
    {
        ImportWindow window = new(_rows.Select(r => r.Account.Secret)) { Owner = this };
        if (window.ShowDialog() == true)
        {
            foreach (Account account in window.ImportedAccounts)
            {
                _rows.Add(new AccountRowViewModel(account));
            }
            RefreshTagOrder();
            SortRows();
            UpdateEmptyMessageVisibility();
            SaveAccounts();
        }
    }

    /// <summary>
    /// Opens the edit dialog for the account associated with the clicked "Edit" button, then applies whatever the dialog reports - either updated details or removal.
    /// </summary>
    private void EditAccount_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: AccountRowViewModel row })
        {
            return;
        }

        EditAccountWindow window = new(row.Account) { Owner = this };
        if (window.ShowDialog() != true)
        {
            return;
        }

        if (window.WasRemoved)
        {
            _rows.Remove(row);
        }
        else
        {
            row.NotifyDetailsChanged();
        }

        RefreshTagOrder();
        SortRows();
        UpdateEmptyMessageVisibility();
        SaveAccounts();
    }

    /// <summary>
    /// Opens the "Manage Tags" dialog and, if the user saves changes, persists the new tag order and re-sorts the list.
    /// </summary>
    private void ManageTags_Click(object sender, RoutedEventArgs e)
    {
        ManageTagsWindow window = new(_tagOrder) { Owner = this };
        if (window.ShowDialog() == true)
        {
            TagOrderStorage.Save(window.SavedOrder);
            RefreshTagOrder();
            SortRows();
        }
    }

    /// <summary>
    /// Filters the account list down to rows whose issuer or label contains the search text, live as the user types.
    /// </summary>
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        string query = SearchBox.Text.Trim();
        _accountsView.Filter = string.IsNullOrEmpty(query) ? null
            : candidate => candidate is AccountRowViewModel row
                && (row.Issuer.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                    || row.Label.Contains(query, StringComparison.CurrentCultureIgnoreCase));
    }

    /// <summary>
    /// Copies the clicked account's current code to the clipboard.
    /// </summary>
    private void Code_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: AccountRowViewModel row })
        {
            Clipboard.SetText(row.Code);
        }
    }

    /// <summary>
    /// Re-sorts the account list by tag order (Untagged always first), then by issuer, then by label, moving rows into place in the existing ObservableCollection rather than rebuilding it - so the UI just animates rows into their new positions instead of flickering. Call this whenever an account is added, imported, edited, or removed, or after the tag order changes - after calling RefreshTagOrder() if the set of in-use tags might have changed.
    /// </summary>
    private void SortRows()
    {
        List<AccountRowViewModel> sorted = [.. _rows
            .OrderBy(r => GetTagRank(r.Account.Tag))
            .ThenBy(r => r.Issuer, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(r => r.Label, StringComparer.CurrentCultureIgnoreCase)];
        for (int i = 0; i < sorted.Count; i++)
        {
            int currentIndex = _rows.IndexOf(sorted[i]);
            if (currentIndex != i)
            {
                _rows.Move(currentIndex, i);
            }
        }

        // The grouped CollectionView only assigns each group's display order once, the first time it encounters that group - it doesn't re-derive group order just because the underlying items got reordered via Move above. Refresh() forces it to recompute groups (and their order) from the collection's current, now-correctly-sorted order.
        _accountsView.Refresh();
    }

    /// <summary>
    /// Recomputes _tagOrder from the saved tag order reconciled against the tags actually in use right now. Call this whenever the set of in-use tags might have changed (accounts added, imported, edited, or removed), before SortRows().
    /// </summary>
    private void RefreshTagOrder()
    {
        List<string> tagsInUse = [.. _rows.Select(r => r.Account.Tag).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct()];
        _tagOrder = TagOrderStorage.Reconcile(TagOrderStorage.Load(), tagsInUse);
    }

    /// <summary>
    /// The sort rank for a tag - Untagged (empty) always sorts first, then real tags in _tagOrder's order, then any not-yet-reconciled tag last.
    /// </summary>
    /// <param name="tag">The account's tag</param>
    private int GetTagRank(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return -1;
        }

        int index = _tagOrder.IndexOf(tag);
        return index >= 0 ? index : _tagOrder.Count;
    }

    /// <summary>
    /// Shows or hides the "no accounts yet" placeholder message based on the current list.
    /// </summary>
    private void UpdateEmptyMessageVisibility()
    {
        EmptyMessage.Visibility = _rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// Scrolls the account list exactly one row per mouse wheel notch, instead of WPF's default of a few rows at a time.
    /// </summary>
    /// <param name="sender">The account list</param>
    /// <param name="e">The mouse wheel event arguments</param>
    private void AccountList_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        _accountListScrollViewer ??= FindVisualChild<ScrollViewer>(AccountList);
        if (_accountListScrollViewer is null)
        {
            return;
        }

        if (e.Delta > 0)
        {
            _accountListScrollViewer.LineUp();
        }
        else
        {
            _accountListScrollViewer.LineDown();
        }

        e.Handled = true;
    }

    /// <summary>
    /// Searches the visual tree under the given element for the first descendant of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of descendant to find</typeparam>
    /// <param name="parent">The element to search under</param>
    /// <returns>The first matching descendant, or null if none was found</returns>
    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
            {
                return typedChild;
            }

            T? descendant = FindVisualChild<T>(child);
            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }

    /// <summary>
    /// Persists the current account list to disk.
    /// </summary>
    private void SaveAccounts()
    {
        SecureStorage.Save(_rows.Select(r => r.Account));
    }
}