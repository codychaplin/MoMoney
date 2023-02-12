using MoMoney.ViewModels.Settings;

namespace MoMoney.Views.Settings;

public partial class StockSettingsPage : ContentPage
{
    StockSettingsViewModel vm;

    public StockSettingsPage()
	{
		InitializeComponent();
        vm = (StockSettingsViewModel)BindingContext;
        NavigatedTo += vm.Refresh;
    }
}