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
                return Utilities.GetColour("primary", "primaryDark"); // green
            else if (amount < 0)
                return Utilities.GetColour("errorVariant", "errorVariantDark"); // red
            else
                return Utilities.GetColour("onSurface", "onSurfaceDark"); // black or white
        }

        return Utilities.GetColour("outline", "outlineDark"); ; // error = gray
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}