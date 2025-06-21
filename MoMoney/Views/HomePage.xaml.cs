using CommunityToolkit.Mvvm.Messaging;
using MoMoney.Core.Data;
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
            IMoMoneydb? momoneyDb = Handler?.MauiContext?.Services.GetService<IMoMoneydb>();
            if (vm == null || momoneyDb == null)
                return;
            BindingContext = vm;

            Shell.Current.IsBusy = true;
            await momoneyDb.Init(false);
            await vm.Refresh();
            Shell.Current.IsBusy = false;

            // refresh when dates change, or when it is triggered by UpdateHomePageMessage
            dtFrom.DateSelected += (s, e) => WeakReferenceMessenger.Default.Send(new UpdateHomePageMessage());
            dtTo.DateSelected += (s, e) => WeakReferenceMessenger.Default.Send(new UpdateHomePageMessage());
        };
    }
}