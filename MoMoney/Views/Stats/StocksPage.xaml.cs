using MoMoney.Services;
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

    void ContentPage_Unloaded(object sender, EventArgs e)
    {
		StockService.Stocks.Clear();
    }
}