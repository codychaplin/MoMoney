using System.Globalization;
using MoMoney.Core.Models.Statements;
using MoMoney.Core.Helpers;

namespace MoMoney.Converters;

public class ImportStatusToColourConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ImportStatus status)
        {
            return status switch
            {
                ImportStatus.Verified => Utilities.GetColour("Green"),
                ImportStatus.ManuallyApproved => Utilities.GetColour("Blue"),
                ImportStatus.NeedsVerification => Utilities.GetColour("Red"),
                ImportStatus.AutoSkipped => Utilities.GetColour("Purple"),
                ImportStatus.ManuallySkipped => Utilities.GetColour("Orange"),
                _ => Utilities.GetColour("Gray")
            };
        }

        return Utilities.GetColour("Gray");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}