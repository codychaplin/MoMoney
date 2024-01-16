using System.Globalization;

namespace MoMoney.Core.Converters;

/// <summary>
/// Changes visibility of button based on whether the corresponding value is null.
/// </summary>
public class NullToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}