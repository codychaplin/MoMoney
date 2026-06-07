using System.Globalization;
using MoMoney.Core.Helpers;

namespace MoMoney.Converters;

/// <summary>
/// Changes background colour based on Enabled value
/// </summary>
public class TileColourConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // if enabled, light gray, else, dark gray
        if (bool.TryParse(value?.ToString(), out bool enabled) && !enabled)
            return Utilities.GetColour("surfaceContainer", "surfaceContainerDark");

        // return enabled colour by default
        return Utilities.GetColour("surfaceContainerHighest", "surfaceContainerHighestDark") ;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}