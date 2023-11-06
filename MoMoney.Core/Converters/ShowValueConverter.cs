using System.Globalization;
using MoMoney.Core.Helpers;

namespace MoMoney.Core.Converters;

/// <summary>
/// Converts value to "$****.**" if Utilities.ShowValue is true
/// </summary>
class ShowValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Utilities.ShowValue ? value : "$****.**";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}