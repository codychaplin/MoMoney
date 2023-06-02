using System.Globalization;

namespace MoMoney.Converters;

/// <summary>
/// Converts value to "$****.**" if Constants.ShowValue is true
/// </summary>
class ShowValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Constants.ShowValue ? value : "$****.**";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}