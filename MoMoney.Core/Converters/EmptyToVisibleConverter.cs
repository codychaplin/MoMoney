using System.Globalization;

namespace MoMoney.Core.Converters;

/// <summary>
/// Changes visibility of button based on whether the corresponding value is empty.
/// </summary>
class EmptyToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return !string.IsNullOrEmpty(value.ToString());
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}