using MoMoney.ViewModels.Stats;

namespace MoMoney.Views.Stats;

public partial class InsightsPage : ContentPage
{
	InsightsViewModel vm;

	public InsightsPage()
	{
		InitializeComponent();
		vm = (InsightsViewModel)BindingContext;
	}
}