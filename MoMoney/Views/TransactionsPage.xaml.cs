using CommunityToolkit.Mvvm.Messaging;
using Syncfusion.Maui.ListView.Helpers;
using MoMoney.Core.Helpers;
using MoMoney.Core.ViewModels;

namespace MoMoney.Views;

public partial class TransactionsPage : ContentView
{
    TransactionsViewModel vm;

    public static EventHandler<TransactionEventArgs> TransactionsChanged { get; set; }

    public TransactionsPage(TransactionsViewModel _vm)
	{
		InitializeComponent();
        vm = _vm;
        BindingContext = vm;

        // add padding to bottom of sfListView
        var scrollview = listView.GetScrollView();
        scrollview.Padding = new Thickness(0, 0, 0, 10);

        vm.ListView = listView;

        Loaded += vm.Loaded;
        TransactionsChanged += vm.Refresh;

        dtFrom.DateSelected += InvokeTransactionsChanged;
        dtTo.DateSelected += InvokeTransactionsChanged;

        pckAccount.SelectedIndexChanged += vm.UpdateFilter;
        pckCategory.SelectedIndexChanged += vm.CategoryChanged;
        pckSubcategory.SelectedIndexChanged += vm.UpdateFilter;

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

    /// <summary>
    /// Updates Payee then calls UpdateFilter().
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    void entPayee_SelectionChanged(object sender, Syncfusion.Maui.Inputs.SelectionChangedEventArgs e)
    {
        // doesn't work if entPayee is bound to vm.Payee, has to be done manually like this
        vm.Payee = entPayee.SelectedValue?.ToString() ?? "";
        vm.UpdateFilter(sender, e);
    }
}