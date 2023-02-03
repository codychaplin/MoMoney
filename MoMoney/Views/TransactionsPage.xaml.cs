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
        // first two months, show 1 year, starting March show YTD
        vm.From = (DateTime.Today.Month <= 2) ? DateTime.Today.AddYears(-1) : new(DateTime.Today.Year, 1, 1);
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
    /// <param name="s"></param>
    /// <param name="e"></param>
    async void Refresh(object s, TransactionEventArgs e)
    {
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