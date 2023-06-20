using MoMoney.ViewModels.Settings.Edit;

namespace MoMoney.Views.Settings.Edit;

public partial class EditStockPage : ContentPage
{
    EditStockViewModel vm;

    public EditStockPage(EditStockViewModel _vm)
    {
        InitializeComponent();
        vm = _vm;
        BindingContext = vm;
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