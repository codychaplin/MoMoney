using System.Globalization;
using MoMoney.Core.Helpers;

namespace MoMoney.Converters;

/// <summary>
/// Changes text colour based on category
/// </summary>
public class PositiveNegativeColourConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (decimal.TryParse(value?.ToString(), out decimal amount))
        {
            if (amount > 0)
                return Utilities.GetColour("Green"); // green
            else if (amount < 0)
                return Utilities.GetColour("Red"); // red
            else
                return Utilities.GetColour("Black", "White"); // black or white
        }

        return Utilities.GetColour("Gray400"); ; // error = gray
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}