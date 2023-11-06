using MoMoney.Core.Helpers;
using MoMoney.Core.ViewModels.Stats;

namespace MoMoney.Views.Stats;

public partial class InsightsPage : ContentPage
{
	public InsightsPage(InsightsViewModel _vm)
	{
		InitializeComponent();
		BindingContext = _vm;
		Loaded += _vm.Init;
		pckDate.SelectedIndexChanged += _vm.Refresh;

		if (Utilities.ShowValue)
		{
            IncExpChartIncLbl.LabelFormat = "$0";
            IncExpChartExpLbl.LabelFormat = "$0;$0";
        }
		else
		{
            IncExpChartIncLbl.LabelFormat = "$?";
            IncExpChartExpLbl.LabelFormat = "$?;$?";
            
        }
	}
}