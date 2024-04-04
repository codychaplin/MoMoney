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
            HomeViewModel vm = Handler.MauiContext.Services.GetService<HomeViewModel>();
            BindingContext = vm;

            Shell.Current.IsBusy = true;
            await vm.Refresh();
            Shell.Current.IsBusy = false;

            // refresh when dates change, or when it is triggered by UpdateHomePageMessage
            WeakReferenceMessenger.Default.Register<UpdateHomePageMessage>(this, async (r, m) => await vm.Refresh());
            dtFrom.DateSelected += (s, e) => WeakReferenceMessenger.Default.Send(new UpdateHomePageMessage());
            dtTo.DateSelected += (s, e) => WeakReferenceMessenger.Default.Send(new UpdateHomePageMessage());
        };
    }
}