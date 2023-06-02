using System.Globalization;

namespace MoMoney.Converters;

/// <summary>
/// Changes background colour based on Enabled value
/// </summary>
class TileColourConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // if enabled, light gray, else, dark gray
        if (bool.TryParse(value.ToString(), out bool enabled))
            return (enabled) ? Color.Parse("#303030") : Color.Parse("#212121");

        // return light gray by default
        return Color.Parse("#303030");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}