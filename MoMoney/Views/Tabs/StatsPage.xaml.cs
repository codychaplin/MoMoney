using MoMoney.Core.ViewModels;

namespace MoMoney.Views.Tabs;

public partial class StatsPage : ContentView
{
    public StatsPage()
	{
		InitializeComponent();

        HandlerChanged += (s, e) =>
        {
            BindingContext = Handler?.MauiContext?.Services.GetService<StatsViewModel>();
        };
    }
}