﻿using System.Globalization;

namespace MoMoney.Core.Converters;

/// <summary>
/// Changes background colour based on Enabled value
/// </summary>
public class TileColourConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // if enabled, light gray, else, dark gray
        if (bool.TryParse(value.ToString(), out bool enabled))
        {
            if (AppInfo.RequestedTheme == AppTheme.Dark)
                return (enabled) ? Color.Parse("#313131") : Color.Parse("#212121");
            else
                return (enabled) ? Color.Parse("#F1F1F1") : Color.Parse("#C8C8C8");
        }

        // return light gray by default
        return Color.Parse("#313131");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}