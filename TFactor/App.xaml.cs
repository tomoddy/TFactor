using System.Windows;
using System.Windows.Media;
using TFactor.Properties;
using TFactor.Services;

namespace TFactor;

/// <summary>
/// The application entry point. Gates access behind a Windows Hello prompt before showing MainWindow.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Verifies the user with Windows Hello before creating and showing MainWindow. If verification fails or is
    /// cancelled, offers a retry, and shuts the app down if the user declines.
    /// </summary>
    /// <param name="e">The startup event arguments</param>
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Finds the embedded Roboto Mono font by asking WPF to enumerate what it can actually see under Fonts/, rather than hand-building a pack URI string and hoping it resolves - confirmed via a diagnostic that the font embeds fine and is the only family found there, so just take it directly instead of filtering by name. Published as a resource so DarkTheme.xaml's control styles can pick it up via DynamicResource - it has to be Dynamic, since this runs after that dictionary has already been parsed.
        Uri fontsFolderUri = new("pack://application:,,,/TFactor;component/Fonts/");
        Resources["AppFontFamily"] = Fonts.GetFontFamilies(fontsFolderUri).First();

        // Applies to every window the app creates (MainWindow and all dialogs), so none of them show up with a native white title bar. SourceInitialized isn't a routed event, so we hook Loaded instead - the window's native handle already exists by then.
        EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent, new RoutedEventHandler(OnWindowLoaded));

        // No window is open yet, so keep the app alive ourselves while we wait on the async Windows Hello prompt
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Keep prompting the user until they either verify successfully or decline to retry
        while (!await WindowsHelloAuth.VerifyAsync(Strings.App_WindowsHelloPrompt))
        {
            MessageBoxResult retry = MessageBox.Show(Strings.App_VerificationFailedMessage, Strings.Common_AppTitle, MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (retry != MessageBoxResult.Yes)
            {
                Shutdown();
                return;
            }
        }

        // Verified - hand control back to the normal window-driven shutdown behavior and show the app
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        MainWindow window = new();
        MainWindow = window;
        window.Show();
    }

    /// <summary>
    /// Switches a window's title bar to dark mode once it's loaded, by which point it already has a native handle to apply that to.
    /// </summary>
    /// <param name="sender">The window that was just loaded</param>
    /// <param name="e">Unused</param>
    private static void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is Window window)
        {
            DarkTitleBar.Apply(window);
        }
    }
}