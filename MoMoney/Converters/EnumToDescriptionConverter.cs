using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace MoMoney.Converters;

public class EnumToDescriptionConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
            return string.Empty;
        
        string valueToString = value?.ToString() ?? string.Empty;

        var type = value!.GetType();
        var name = Enum.GetName(type, value);
        if (name == null)
            return valueToString;
        
        var field = type.GetField(name);
        if (field == null)
            return valueToString;
        
        var attr = field.GetCustomAttribute<DescriptionAttribute>();
        if (attr == null)
            return valueToString;
        
        return attr.Description;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}