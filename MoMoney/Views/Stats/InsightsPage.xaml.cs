using MoMoney.ViewModels.Stats;

namespace MoMoney.Views.Stats;

public partial class InsightsPage : ContentPage
{
	InsightsViewModel vm;

	public InsightsPage()
	{
		InitializeComponent();
		vm = (InsightsViewModel)BindingContext;
		Loaded += vm.Init;
		pckDate.SelectedIndexChanged += vm.Refresh;

		if (Constants.ShowValue)
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