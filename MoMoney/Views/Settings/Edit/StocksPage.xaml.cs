using MoMoney.Core.ViewModels.Settings.Edit;

namespace MoMoney.Views.Settings.Edit;

public partial class StocksPage : ContentPage
{
    public StocksPage(StocksViewModel _vm)
    {
        InitializeComponent();
        BindingContext = _vm;
        NavigatedTo += _vm.RefreshStocks;
    }
}