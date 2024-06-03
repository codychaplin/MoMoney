using MoMoney.Core.Helpers;
using MoMoney.Core.ViewModels.Stats;

namespace MoMoney.Views.Stats;

public partial class InsightsPage : ContentPage
{
	public InsightsPage(InsightsViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
		Loaded += async (s, e) => await vm.Init();

		if (Utilities.ShowValue)
		{
            IncExpChartIncLbl.LabelFormat = "$0";
            IncExpChartExpLbl.LabelFormat = "$0";
        }
		else
		{
            IncExpChartIncLbl.LabelFormat = "$?";
            IncExpChartExpLbl.LabelFormat = "$?";
            
        }
	}
}