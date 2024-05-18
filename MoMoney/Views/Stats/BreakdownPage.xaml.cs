using MoMoney.Core.ViewModels.Stats;

namespace MoMoney.Views.Stats;

public partial class BreakdownPage : ContentPage
{
	public BreakdownPage(BreakdownViewModel _vm)
	{
		InitializeComponent();
        BindingContext = _vm;
        Loaded += _vm.Init;
    }
}