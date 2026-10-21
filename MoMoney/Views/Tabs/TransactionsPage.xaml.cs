using CommunityToolkit.Mvvm.Messaging;
using MoMoney.Core.Helpers;
using MoMoney.Core.ViewModels.Tabs;

namespace MoMoney.Views.Tabs;

public partial class TransactionsPage : ContentView
{
    TransactionsViewModel? vm;

    public TransactionsPage()
	{
		InitializeComponent();

        HandlerChanged += (s, e) =>
        {
            TransactionsViewModel? _vm = Handler?.MauiContext?.Services.GetService<TransactionsViewModel>();
            if (_vm == null)
                return;
            vm = _vm;
            BindingContext = vm;

            _ = vm.Load();
            WeakReferenceMessenger.Default.Register<UpdateTransactionsMessage>(this, async (r, m) => await vm.Refresh(m.Value));
        };
    }

    // workaround for clearing value
    void OnAmountRangeStartChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.NewTextValue) && vm is not null)
            vm.AmountRangeStart = null;
    }

    void OnAmountRangeEndChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.NewTextValue) && vm is not null)
            vm.AmountRangeEnd = null;
    }
}