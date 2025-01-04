using System.Globalization;
using Microsoft.Extensions.Logging;

namespace MoMoney.Converters;

/// <summary>
/// Changes visibility of button based on whether the corresponding value is None.
/// </summary>
public class NoneToVisibleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is LogLevel logLevel)
            return logLevel != LogLevel.None;

        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}