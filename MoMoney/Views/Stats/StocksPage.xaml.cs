using MoMoney.ViewModels.Stats;

namespace MoMoney.Views.Stats;

public partial class StocksPage : ContentPage
{
	StocksViewModel vm;

	public StocksPage()
	{
		InitializeComponent();
		vm = (StocksViewModel)BindingContext;
		Loaded += vm.Init;
	}
}