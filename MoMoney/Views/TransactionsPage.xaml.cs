using CommunityToolkit.Mvvm.Messaging;
using Syncfusion.Maui.ListView.Helpers;
using MoMoney.Core.Helpers;
using MoMoney.Core.ViewModels;

namespace MoMoney.Views;

public partial class TransactionsPage : ContentView
{
    public TransactionsPage()
	{
		InitializeComponent();

        HandlerChanged += (s, e) =>
        {
            TransactionsViewModel vm = Handler.MauiContext.Services.GetService<TransactionsViewModel>();
            BindingContext = vm;

            vm.ListView = listView;
            _ = vm.Load();
            WeakReferenceMessenger.Default.Register<UpdateTransactionsMessage>(this, async (r, m) => await vm.Refresh(m.Value));
        };

        // add padding to bottom of sfListView
        var scrollview = listView.GetScrollView();
        scrollview.Padding = new Thickness(0, 0, 0, 10);

        dtFrom.DateSelected += (s,e) => WeakReferenceMessenger.Default.Send(new UpdateTransactionsMessage(new TransactionEventArgs(null, TransactionEventArgs.CRUD.Read)));
        dtTo.DateSelected += (s,e) => WeakReferenceMessenger.Default.Send(new UpdateTransactionsMessage(new TransactionEventArgs(null, TransactionEventArgs.CRUD.Read)));
    }
}