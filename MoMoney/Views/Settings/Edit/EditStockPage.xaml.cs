using MoMoney.Core.ViewModels.Settings.Edit;

namespace MoMoney.Views.Settings.Edit;

public partial class EditStockPage : ContentPage
{
    public EditStockPage(EditStockViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        Loaded += async (s, e) => await vm.GetStock();
    }

    private void btnClear_Clicked(object sender, EventArgs e)
    {
        txtSymbol.Text = "";
        txtQuantity.Text = "";
        txtCost.Text = "";
    }
}