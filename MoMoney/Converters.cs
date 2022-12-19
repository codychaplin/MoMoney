using MoMoney.Services;
using System.Globalization;

namespace MoMoney
{
    /// <summary>
    /// Gets Category name from Category ID
    /// </summary>
    class IdToCategoryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (int.TryParse(value.ToString(), out int ID))
            {
                var task = Task.Run(async () => { return await CategoryService.GetCategory(ID); });
                if (task.Result is null)
                    return "";
                else
                    return task.Result.CategoryName;
            }
            
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Gets Account name from Account ID
    /// </summary>
    class IdToAccountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (int.TryParse(value.ToString(), out int ID))
            {
                var task = Task.Run(async () => { return await AccountService.GetAccount(ID); });
                return task.Result.AccountName;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

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
}
