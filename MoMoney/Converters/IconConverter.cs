using System.Globalization;
using MoMoney.Core.Helpers;

namespace MoMoney.Converters;

/// <summary>
/// Changes icon based on category
/// </summary>
public class IconConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] != null && values[1] != null)
        {
            var cat = int.Parse(values[0].ToString() ?? string.Empty);
            if (cat == Constants.TRANSFER_ID)
            {
                var subcat = int.Parse(values[1].ToString() ?? string.Empty);

                return subcat == Constants.DEBIT_ID ? "grey_arrow_right.svg" : "grey_arrow_left.svg";
            }
            else if (cat == Constants.INCOME_ID)
            {
                return "green_arrow_up.svg";
            }
            else if (cat >= Constants.EXPENSE_ID)
            {
                return "red_arrow_down.svg";
            }
        }

        return "error.svg";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}