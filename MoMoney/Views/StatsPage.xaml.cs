using MoMoney.ViewModels;

namespace MoMoney.Views;

public partial class StatsPage : ContentView
{
    StatsPageViewModel vm;

    public StatsPage()
	{
		InitializeComponent();
        vm = (StatsPageViewModel)BindingContext;
    }
}