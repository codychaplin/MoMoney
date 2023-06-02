using MoMoney.ViewModels;

namespace MoMoney.Views;

public partial class StatsPage : ContentView
{
    public StatsPage( StatsViewModel _vm)
	{
		InitializeComponent();
        BindingContext = _vm;
    }
}