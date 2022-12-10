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

    /// <summary>
    /// Refreshes transactions on page. 
    /// </summary>
    /// <param name="s"></param>
    /// <param name="e"></param>
    private async void Refresh(object s, TransactionEventArgs e)
    {
        if (e.Type == TransactionEventArgs.CRUD.Read)
            await Task.Delay(100);
        await vm.Refresh(e);
    }

    /// <summary>
    /// Changes visibility of date range picker
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ImageButton_Clicked(object sender, EventArgs e)
    {
        frDates.IsVisible = !frDates.IsVisible;
    }

    /// <summary>
    /// Invokes TransactionsChanged when From date changes.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void dtFrom_DateSelected(object sender, DateChangedEventArgs e)
    {
        InvokeTransactionsChanged();
    }

    /// <summary>
    /// Invokes TransactionsChanged when To date changes.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void dtTo_DateSelected(object sender, DateChangedEventArgs e)
    {
        InvokeTransactionsChanged();
    }

    /// <summary>
    /// Invokes TransactionsChanged
    /// </summary>
    void InvokeTransactionsChanged()
    {
        var args = new TransactionEventArgs(null, TransactionEventArgs.CRUD.Read);
        TransactionsChanged?.Invoke(this, args);
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