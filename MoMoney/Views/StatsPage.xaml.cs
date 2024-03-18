using MoMoney.Core.ViewModels;

namespace MoMoney.Views;

public partial class StatsPage : ContentView
{
    public StatsPage()
	{
		InitializeComponent();

        HandlerChanged += (s, e) =>
        {
            BindingContext = Handler.MauiContext.Services.GetService<StatsViewModel>();
        };
    }
}