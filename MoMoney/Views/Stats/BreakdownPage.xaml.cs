using MoMoney.ViewModels.Stats;

namespace MoMoney.Views.Stats;

public partial class BreakdownPage : ContentPage
{
	BreakdownViewModel vm;

	public BreakdownPage()
	{
		InitializeComponent();
        vm = (BreakdownViewModel)BindingContext;
        Loaded += vm.Init;
		sfTabView.SelectionChanged += vm.Update;

		if (Constants.ShowValue)
		{
			ExpChartLbl.LabelFormat = "$0";
			IncChartLbl.LabelFormat = "$0";
		}
		else
        {
            ExpChartLbl.LabelFormat = "$?";
            IncChartLbl.LabelFormat = "$?";
        }
    }
}