using System.Globalization;
using MoMoney.Core.Helpers;

namespace MoMoney.Converters;

/// <summary>
/// Converts value to "*.**%" if Utilities.ShowValue is true
/// </summary>
public class ShowValuePercentConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
            return string.Empty;

        return Utilities.ShowValue ? value : "*.**%";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}