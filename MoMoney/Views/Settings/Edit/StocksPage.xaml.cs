using MoMoney.Core.Helpers;
using MoMoney.Core.ViewModels.Settings.Edit;

namespace MoMoney.Views.Settings.Edit;

public partial class StocksPage : ContentPage
{
    StocksViewModel vm;
    public StocksPage(StocksViewModel _vm)
    {
        InitializeComponent();
        vm = _vm;
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await PageLoader.Load(vm.LoadStocks);
    }
}