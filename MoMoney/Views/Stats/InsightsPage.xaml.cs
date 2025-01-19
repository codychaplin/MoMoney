using MoMoney.Core.Helpers;
using MoMoney.Core.ViewModels.Stats;

namespace MoMoney.Views.Stats;

public partial class InsightsPage : ContentPage
{
    InsightsViewModel vm;
    public InsightsPage(InsightsViewModel _vm)
	{
		InitializeComponent();
        vm = _vm;
        BindingContext = vm;

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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await PageLoader.Load(vm.LoadInsights);
    }
}