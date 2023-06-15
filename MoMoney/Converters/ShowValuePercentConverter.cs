using MoMoney.Helpers;
using System.Globalization;

namespace MoMoney.Converters;

/// <summary>
/// Converts value to "*.**%" if Utilities.ShowValue is true
/// </summary>
class ShowValuePercentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Utilities.ShowValue ? value : "*.**%";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}