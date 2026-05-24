using CommunityToolkit.Mvvm.Messaging;
using MoMoney.Core.Helpers;
using MoMoney.Core.ViewModels;

namespace MoMoney.Views;

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

            // refresh when dates change, or when it is triggered by UpdateHomePageMessage
            dtFrom.DateSelected += (s, e) => WeakReferenceMessenger.Default.Send(new UpdateHomePageMessage());
            dtTo.DateSelected += (s, e) => WeakReferenceMessenger.Default.Send(new UpdateHomePageMessage());
        };
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