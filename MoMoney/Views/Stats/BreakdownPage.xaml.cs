using MoMoney.Helpers;
using MoMoney.ViewModels.Stats;

namespace MoMoney.Views.Stats;

public partial class BreakdownPage : ContentPage
{
	public BreakdownPage(BreakdownViewModel _vm)
	{
		InitializeComponent();
        BindingContext = _vm;
        Loaded += _vm.Init;
		sfTabView.SelectionChanged += _vm.Update;

		if (Utilities.ShowValue)
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