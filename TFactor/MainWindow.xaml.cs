using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
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
            UpdateEmptyMessageVisibility();
            SaveAccounts();
        }
    }

    /// <summary>
    /// Opens the "Import from Screenshot" dialog and adds any accounts the user chose to import.
    /// </summary>
    private void ImportFromScreenshot_Click(object sender, RoutedEventArgs e)
    {
        ImportWindow window = new() { Owner = this };
        if (window.ShowDialog() == true)
        {
            foreach (Account account in window.ImportedAccounts)
            {
                _rows.Add(new AccountRowViewModel(account));
            }
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

        UpdateEmptyMessageVisibility();
        SaveAccounts();
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