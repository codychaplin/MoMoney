using MoMoney.Core.ViewModels.Settings.Edit;

namespace MoMoney.Views.Settings.Edit;

public partial class EditStockPage : ContentPage
{
    public EditStockPage(EditStockViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        Clear();
    }

    private void BtnClear_Clicked(object sender, EventArgs e)
    {
        Clear();
    }

    void Clear()
    {
        txtSymbol.Text = "";
        txtQuantity.Text = "";
        txtCost.Text = "";
    }
}