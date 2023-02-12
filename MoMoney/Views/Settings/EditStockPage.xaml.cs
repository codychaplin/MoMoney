using MoMoney.ViewModels.Settings;

namespace MoMoney.Views.Settings;

public partial class EditStockPage : ContentPage
{
	EditStockViewModel vm;

	public EditStockPage()
	{
		InitializeComponent();
		vm = (EditStockViewModel)BindingContext;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await vm.GetStock();
    }

    private void btnClear_Clicked(object sender, EventArgs e)
    {
		txtSymbol.Text = "";
		txtQuantity.Text = "";
		txtCost.Text = "";
		txtMarketPrice.Text = "";
		txtBookValue.Text = "";
    }
}