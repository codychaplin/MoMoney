using MoMoney.Core.ViewModels.Stats;

namespace MoMoney.Views.Stats;

public partial class StockStatsPage : ContentPage
{
    StockStatsViewModel vm;

	public StockStatsPage(StockStatsViewModel _vm)
	{
		InitializeComponent();
		vm = _vm;
		BindingContext = vm;
		Loaded += vm.Init;
	}

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        vm.cts?.Cancel(); // cancel webscraping, if in progress
    }
}