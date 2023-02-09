using MoMoney.ViewModels.Stats;

namespace MoMoney.Views.Stats;

public partial class MonthBreakdownPage : ContentPage
{
	MonthBreakdownViewModel vm;

	public MonthBreakdownPage()
	{
		InitializeComponent();
        vm = (MonthBreakdownViewModel)BindingContext;
        Loaded += vm.Init;
		sfTabView.SelectionChanged += vm.Update;
    }
}