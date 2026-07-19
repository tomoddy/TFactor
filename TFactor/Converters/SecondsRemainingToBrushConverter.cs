using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TFactor.Converters;

/// <summary>
/// Colors the TOTP countdown text based on how much time is left before the code rotates: red once it's about to expire, yellow as an early warning, teal otherwise.
/// </summary>
public class SecondsRemainingToBrushConverter : IValueConverter
{
    /// <summary>
    /// Converts a seconds-remaining count into the brush that should color it.
    /// </summary>
    /// <param name="value">The seconds remaining, expected to be an int</param>
    /// <param name="targetType">Unused</param>
    /// <param name="parameter">Unused</param>
    /// <param name="culture">Unused</param>
    /// <returns>A red brush below 5 seconds, yellow from 5-10 seconds, or teal above 10 seconds</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        int secondsRemaining = value is int seconds ? seconds : 0;
        string resourceKey = secondsRemaining switch
        {
            < 5 => "Brush.AccentPink",
            < 10 => "Brush.AccentYellow",
            _ => "Brush.TextTeal"
        };
        return Application.Current.Resources[resourceKey];
    }

    /// <summary>
    /// Not supported - this converter is one-way only.
    /// </summary>
    /// <param name="value">Unused</param>
    /// <param name="targetType">Unused</param>
    /// <param name="parameter">Unused</param>
    /// <param name="culture">Unused</param>
    /// <exception cref="NotSupportedException">Always thrown</exception>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}