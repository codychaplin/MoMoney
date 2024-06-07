using System.Globalization;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;

namespace MoMoney.Core.Converters;

/// <summary>
/// Converts value to "$****.**" if Utilities.ShowValue is true
/// </summary>
public class StockDataLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not StockData stock)
            return "";

        string price = Utilities.ShowValue ? stock.Price.ToString("C2") : "$****.**";
        return $"{stock.Symbol}\n{price}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}