using UraniumUI.Icons.MaterialSymbols;
using UraniumUI.Material.Controls;

namespace MoMoney.Components;

public class AccountField : PickerField
{
    public AccountField()
    {
        Title = "Account";
        Icon = new FontImageSource { FontFamily = "MaterialRoundedFilled", Glyph = MaterialOutlined.Credit_card };
        ItemDisplayBinding = new Binding("AccountName");
    }
}
