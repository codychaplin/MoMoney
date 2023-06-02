using MoMoney.ViewModels.Settings;

namespace MoMoney.Views.Settings;

public partial class StockSettingsPage : ContentPage
{
    public StockSettingsPage(StockSettingsViewModel _vm)
	{
		InitializeComponent();
        BindingContext = _vm;
        NavigatedTo += _vm.Refresh;
    }
}