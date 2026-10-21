using System.Globalization;
using MoMoney.Core.Helpers;

namespace MoMoney.Converters;

/// <summary>
/// Changes text colour based on category
/// </summary>
public class AmountColourConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (int.TryParse(value?.ToString(), out int ID))
        {
            return ID switch
            {
                Constants.INCOME_ID => Utilities.GetColour("Green", "LightGreen"), // income = green
                Constants.TRANSFER_ID => Utilities.GetColour("onSurface", "onSurfaceDark"), // transfer = white/black
                _ => Utilities.GetColour("errorVariant", "errorVariantDark")  // expense = red
            };
        }

        return Colors.Black; // error = black
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}