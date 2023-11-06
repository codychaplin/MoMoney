using System.Globalization;
using MoMoney.Core.Helpers;

namespace MoMoney.Core.Converters;

/// <summary>
/// Changes text colour based on category
/// </summary>
class AmountColourConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (int.TryParse(value.ToString(), out int ID))
        {
            return ID switch
            {
                Constants.INCOME_ID => Color.Parse("#42ba96"), // income = green
                Constants.TRANSFER_ID => Color.Parse("#ffffff"), // transfer = white
                _ => Color.Parse("#df4759")  // expense = red
            };
        }

        return "000000"; // error = black
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}