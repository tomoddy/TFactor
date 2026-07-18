using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace TFactor.Services;

/// <summary>
/// Switches a window's native title bar to Windows' dark mode chrome, so it doesn't show up as a plain white bar above our dark-themed content.
/// </summary>
internal static class DarkTitleBar
{
    /// <summary>
    /// The DWM window attribute that toggles immersive dark mode for the non-client area (title bar, borders).
    /// </summary>
    private const int DwmwaUseImmersiveDarkMode = 20;

    /// <summary>
    /// Sets the DWM window attribute at the given handle.
    /// </summary>
    /// <param name="hwnd">The window handle to modify</param>
    /// <param name="attribute">The DWM attribute to set</param>
    /// <param name="value">A pointer to the attribute value</param>
    /// <param name="valueSize">The size, in bytes, of the value pointed to</param>
    /// <returns>Zero on success, or a non-zero HRESULT on failure</returns>
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int valueSize);

    /// <summary>
    /// Applies the dark title bar to the given window. Safe to call for any window on any Windows version - it's a no-op (not an exception) if the DWM API or attribute isn't supported.
    /// </summary>
    /// <param name="window">The window whose title bar should be switched to dark mode</param>
    public static void Apply(Window window)
    {
        try
        {
            IntPtr hwnd = new WindowInteropHelper(window).Handle;
            int useDarkMode = 1;
            int hresult = DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref useDarkMode, sizeof(int));
            if (hresult != 0)
            {
                // Older Windows builds without this DWM attribute return a failure HRESULT here - just leave the title bar as-is
            }
        }
        catch (Exception)
        {
            // Any other failure here (e.g. the DWM API itself being unavailable) - just leave the title bar as-is
        }
    }
}
