using CommunityToolkit.Mvvm.Messaging;
using MoMoney.Core.Helpers;
using MoMoney.Core.ViewModels;

namespace MoMoney.Views.Tabs;

public partial class HomePage : ContentView
{
    public HomePage()
	{
		InitializeComponent();

        HandlerChanged += async (s, e) =>
        {
            HomeViewModel? vm = Handler?.MauiContext?.Services.GetService<HomeViewModel>();
            if (vm == null)
                return;
            BindingContext = vm;

            await vm.Refresh(true);
        };

        WeakReferenceMessenger.Default.Register<UpdateThemeMessage>(this, (r, m) => UpdateGradient());
    }

    // workaround for gradient not updating on theme change
    void UpdateGradient()
    {
        var colour = Utilities.GetColour("primaryTransparent", "primaryTransparentDark");
        ChartGradientStop.Color = colour;
    }

    void OnRangeSelectionChanged(object? sender, Syncfusion.Maui.Toolkit.SegmentedControl.SelectionChangedEventArgs e)
    {
        if (BindingContext is HomeViewModel vm)
        {
            int index = e.NewIndex ?? (int)DateRange.Y1;   
            vm.SelectedRange = (DateRange)index;
        }
    }
}