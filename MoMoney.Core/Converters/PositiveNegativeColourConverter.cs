using System.Globalization;

namespace MoMoney.Core.Converters;

/// <summary>
/// Changes text colour based on category
/// </summary>
class PositiveNegativeColourConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (decimal.TryParse(value.ToString(), out decimal amount))
        {
            if (amount > 0)
                return Color.Parse("#42ba96"); // green
            else if (amount < 0)
                return Color.Parse("#df4759"); // red
            else
                return Color.Parse("#ffffff"); // white
        }

        return "000000"; // error = black
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}