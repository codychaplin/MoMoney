using MoMoney.Models;
using MoMoney.ViewModels;

namespace MoMoney.Views;

public partial class TransactionsPage : ContentView
{
    TransactionsViewModel vm;

    public static EventHandler<TransactionEventArgs> TransactionsChanged { get; set; }

    public TransactionsPage()
	{
		InitializeComponent();
        vm = (TransactionsViewModel)BindingContext;
        vm.ListView = listView;
        TransactionsChanged += Refresh;
    }

    /// <summary>
    /// Loads data into filter pickers
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    async void ContentView_Loaded(object sender, EventArgs e)
    {
        await vm.GetAccounts();
        await vm.GetParentCategories();
    }

    /// <summary>
    /// Refreshes transactions on page. 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    async void Refresh(object sender, TransactionEventArgs e)
    {
        // if triggered by tab bar (on MainPage), update dates
        if (sender is ContentPage)
        {
            dtFrom.Date = vm.From;
            dtTo.Date = vm.To;
        }

        // if read, delay to avoid lag when switching to TransactionsPage
        if (e.Type == TransactionEventArgs.CRUD.Read)
            await Task.Delay(100);
        await vm.Refresh(e);
    }

    /// <summary>
    /// Changes visibility of filter pickers
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    void ImageButton_Clicked(object sender, EventArgs e)
    {
        // date picker
        grid.RowDefinitions[1].Height = (frDates.IsVisible) ? 0 : 40;
        frDates.IsVisible = !frDates.IsVisible;

        // account picker
        grid.RowDefinitions[2].Height = (frAccounts.IsVisible) ? 0 : 40;
        frAccounts.IsVisible = !frAccounts.IsVisible;

        // amount slider
        grid.RowDefinitions[3].Height = (frAmount.IsVisible) ? 0 : 40;
        frAmount.IsVisible = !frAmount.IsVisible;
        rsAmountFrame.IsVisible = !rsAmountFrame.IsVisible;
        rsAmount.IsVisible = !rsAmount.IsVisible;

        // category picker
        grid.RowDefinitions[4].Height = (frCategories.IsVisible) ? 0 : 40;
        frCategories.IsVisible = !frCategories.IsVisible;

        // subcategory picker
        grid.RowDefinitions[5].Height = (frSubcategories.IsVisible) ? 0 : 40;
        frSubcategories.IsVisible = !frSubcategories.IsVisible;

        // payee search bar
        grid.RowDefinitions[6].Height = (frPayee.IsVisible) ? 0 : 40;
        frPayee.IsVisible = !frPayee.IsVisible;
    }

    /// <summary>
    /// Invokes TransactionsChanged when From date changes.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    void dtFrom_DateSelected(object sender, DateChangedEventArgs e)
    {
        InvokeTransactionsChanged();
    }

    /// <summary>
    /// Invokes TransactionsChanged when To date changes.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    void dtTo_DateSelected(object sender, DateChangedEventArgs e)
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

    async void pckCategory_SelectedIndexChanged(object sender, EventArgs e)
    {
        var parentCategory = (Category)pckCategory.SelectedItem;
        if (parentCategory != null)
        {
            await vm.GetSubcategories(parentCategory);
            vm.UpdateFilter();
        }
    }

    private void pckAccount_SelectedIndexChanged(object sender, EventArgs e)
    {
        vm.UpdateFilter();
    }

    private void pckSubcategory_SelectedIndexChanged(object sender, EventArgs e)
    {
        vm.UpdateFilter();
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