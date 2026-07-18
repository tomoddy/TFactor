using System.Windows;
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

        // No window is open yet, so keep the app alive ourselves while we wait on the async Windows Hello prompt
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Keep prompting the user until they either verify successfully or decline to retry
        while (!await WindowsHelloAuth.VerifyAsync("Unlock TFactor to view your authentication codes."))
        {
            MessageBoxResult retry = MessageBox.Show("Verification was cancelled or didn't succeed. Try again?", "TFactor",MessageBoxButton.YesNo, MessageBoxImage.Warning);
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
}