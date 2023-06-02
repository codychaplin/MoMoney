﻿using System.Globalization;

namespace MoMoney.Converters;

/// <summary>
/// Changes icon based on category
/// </summary>
class IconConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] != null && values[1] != null)
        {
            var cat = int.Parse(values[0].ToString());
            if (cat == Constants.TRANSFER_ID)
            {
                var subcat = int.Parse(values[1].ToString());

                return subcat == Constants.DEBIT_ID ? "grey_arrow_right.png" : "grey_arrow_left.png";
            }
            else if (cat == Constants.INCOME_ID)
            {
                return "green_arrow_up.png";
            }
            else if (cat >= Constants.EXPENSE_ID)
            {
                return "red_arrow_down.png";
            }
        }

        return "error.png";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}