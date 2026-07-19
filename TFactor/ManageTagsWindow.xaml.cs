using System.Collections.ObjectModel;
using System.Windows;

namespace TFactor;

/// <summary>
/// Dialog for reordering the tags used to group accounts in the main list. "Untagged" is always shown first and can't be reordered - only real tags can be moved relative to each other.
/// </summary>
public partial class ManageTagsWindow : Window
{
    /// <summary>
    /// The tags being reordered, in their current order. Does not include "Untagged".
    /// </summary>
    private readonly ObservableCollection<string> _tags;

    /// <summary>
    /// The tag order to save, populated once the dialog closes successfully.
    /// </summary>
    public List<string> SavedOrder { get; private set; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ManageTagsWindow"/> class.
    /// </summary>
    /// <param name="currentOrder">The tags currently in use, in their current display order - not including "Untagged", which is always shown first and isn't reorderable</param>
    public ManageTagsWindow(IEnumerable<string> currentOrder)
    {
        InitializeComponent();
        _tags = [.. currentOrder];
        TagList.ItemsSource = _tags;
    }

    /// <summary>
    /// Moves the clicked tag one position earlier in the order.
    /// </summary>
    private void MoveUp_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string tagName })
        {
            return;
        }

        int index = _tags.IndexOf(tagName);
        if (index > 0)
        {
            _tags.Move(index, index - 1);
        }
    }

    /// <summary>
    /// Moves the clicked tag one position later in the order.
    /// </summary>
    private void MoveDown_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string tagName })
        {
            return;
        }

        int index = _tags.IndexOf(tagName);
        if (index >= 0 && index < _tags.Count - 1)
        {
            _tags.Move(index, index + 1);
        }
    }

    /// <summary>
    /// Saves the current order and closes the dialog with a successful result.
    /// </summary>
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        SavedOrder = [.. _tags];
        DialogResult = true;
    }

    /// <summary>
    /// Closes the dialog without saving any reordering.
    /// </summary>
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}