using CommunityToolkit.Mvvm.Messaging;
using Syncfusion.Maui.ListView.Helpers;
using MoMoney.Core.Helpers;
using MoMoney.Core.ViewModels;

namespace MoMoney.Views;

public partial class TransactionsPage : ContentView
{
    public static EventHandler<TransactionEventArgs> TransactionsChanged { get; set; }

    public TransactionsPage()
	{
		InitializeComponent();

        HandlerChanged += (s, e) =>
        {
            TransactionsViewModel vm = Handler.MauiContext.Services.GetService<TransactionsViewModel>();
            BindingContext = vm;

            vm.ListView = listView;
            _ = vm.Load();
            TransactionsChanged += vm.Refresh;
        };

        // add padding to bottom of sfListView
        var scrollview = listView.GetScrollView();
        scrollview.Padding = new Thickness(0, 0, 0, 10);

        dtFrom.DateSelected += InvokeTransactionsChanged;
        dtTo.DateSelected += InvokeTransactionsChanged;

        WeakReferenceMessenger.Default.Register<UpdateTransactionsMessage>(this, (r, m) =>
        {
            TransactionsChanged?.Invoke(r, m.Value);
        });
    }

    /// <summary>
    /// Invokes TransactionsChanged.
    /// </summary>
    void InvokeTransactionsChanged(object sender, EventArgs e)
    {
        var args = new TransactionEventArgs(null, TransactionEventArgs.CRUD.Read);
        TransactionsChanged?.Invoke(this, args);
    }
}