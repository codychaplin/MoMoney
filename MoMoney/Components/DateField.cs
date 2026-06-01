using UraniumUI.Icons.MaterialSymbols;
using UraniumUI.Material.Controls;

namespace MoMoney.Components;

public class DateField : DatePickerField
{
    public DateField()
    {
        Title = "Date";
        AllowClear = false;
        Format = "MMMM d yyyy";
        Icon = new FontImageSource { FontFamily = "MaterialRoundedFilled", Glyph = MaterialOutlined.Calendar_month };
    }
}
