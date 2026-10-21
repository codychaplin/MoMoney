using UraniumUI.Icons.MaterialSymbols;
using UraniumUI.Material.Controls;

namespace MoMoney.Components;

public class PayeeAutocompleteField : AutoCompleteTextField
{
    public PayeeAutocompleteField()
    {
        Title = "Payee";
        Icon = new FontImageSource { FontFamily = "MaterialRoundedFilled", Glyph = MaterialOutlined.Account_circle };
    }
}
