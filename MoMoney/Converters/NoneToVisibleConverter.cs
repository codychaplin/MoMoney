using Microsoft.Extensions.Logging;
using System.Globalization;

namespace MoMoney.Converters;

/// <summary>
/// Changes visibility of button based on whether the corresponding value is None.
/// </summary>
class NoneToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (LogLevel)value != LogLevel.None;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}