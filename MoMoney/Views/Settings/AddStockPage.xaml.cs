using MoMoney.ViewModels.Settings;

namespace MoMoney.Views.Settings;

public partial class AddStockPage : ContentPage
{
    public AddStockPage(AddStockViewModel _vm)
	{
		InitializeComponent();
        BindingContext = _vm;
        Clear();
    }

    /// <summary>
    /// Clears input fields in view.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnClear_Clicked(object sender, EventArgs e)
    {
        Clear();
    }

    void Clear()
    {
        txtSymbol.Text = "";
        txtQuantity.Text = "";
        txtCost.Text = "";
        txtMarketPrice.Text = "";
        txtBookValue.Text = "";
    }
}