using MoMoney.Models;
using MoMoney.ViewModels;

namespace MoMoney.Views;

public partial class TransactionsPage : ContentPage
{
    TransactionsViewModel vm;

    public static EventHandler<TransactionEventArgs> TransactionsChanged;

    public TransactionsPage()
	{
		InitializeComponent();
        vm = (TransactionsViewModel)BindingContext;
        TransactionsChanged += Refresh;
    }

    private async void Refresh(object s, TransactionEventArgs e)
    {
        if (e.Type == TransactionEventArgs.CRUD.Read)
            await Task.Delay(100);
        await vm.Refresh(e);
    }
}

public class TransactionEventArgs : EventArgs
{
    public Transaction Transaction { get; set; }

    public CRUD Type { get; }

    public enum CRUD
    {
        Create,
        Read,
        Update,
        Delete
    }


    public TransactionEventArgs(Transaction transaction, CRUD type)
    {
        Transaction = transaction;
        Type = type;
    }

}