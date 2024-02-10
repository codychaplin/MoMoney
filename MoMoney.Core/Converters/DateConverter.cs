using System.Globalization;

namespace MoMoney.Core.Converters;

/// <summary>
/// Changes string format based on type
/// </summary>
public class DateConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] == null || values[1] == null)
            return "";
        if (DateTime.TryParse(values[0].ToString(), out var date))
            return values[1].ToString() == "Month" ? date.ToString("MMM yyyy") : date.ToString("yyyy");
        else
            return values[0];
    }

    public object[] ConvertBack(object value, Type[] targetTypea, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}