using MoMoney.Core.ViewModels.Settings.Edit;

namespace MoMoney.Views.Settings.Edit;

public partial class EditStockPage : ContentPage
{
    public EditStockPage(EditStockViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}