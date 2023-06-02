using MoMoney.ViewModels.Stats;

namespace MoMoney.Views.Stats;

public partial class StocksPage : ContentPage
{
	StocksViewModel vm;

	public StocksPage(StocksViewModel _vm)
	{
		InitializeComponent();
		vm = _vm;
		BindingContext = vm;
		Loaded += vm.Init;
	}

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        vm.cts?.Cancel();
    }
}