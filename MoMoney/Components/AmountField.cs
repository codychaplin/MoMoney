using UraniumUI.Icons.MaterialSymbols;
using UraniumUI.Material.Controls;

namespace MoMoney.Components;

public class AmountField : TextField
{
    public AmountField()
    {
        Title = "Amount";
        Keyboard = Keyboard.Numeric;
        Icon = new FontImageSource { FontFamily = "MaterialRoundedFilled", Glyph = MaterialOutlined.Paid };
    }
}
