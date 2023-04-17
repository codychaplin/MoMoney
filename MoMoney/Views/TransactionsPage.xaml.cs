using MoMoney.Models;
using MoMoney.Services;
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
        frAccounts.ZIndex = 2;

        TransactionsChanged += Refresh;
        dtFrom.DateSelected += InvokeTransactionsChanged;
        dtTo.DateSelected += InvokeTransactionsChanged;
        pckAccount.SelectedIndexChanged += vm.UpdateFilter;
        pckCategory.SelectedIndexChanged += vm.CategoryChanged;
        pckSubcategory.SelectedIndexChanged += vm.UpdateFilter;
        
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
        grid.RowDefinitions[1].Height = (grdFilters.IsVisible) ? 0 : 240;
        grdFilters.IsVisible = !grdFilters.IsVisible;
        rsAmountFrame.IsVisible = !rsAmountFrame.IsVisible;
        rsAmount.IsVisible = !rsAmount.IsVisible;
    }

    /// <summary>
    /// Invokes TransactionsChanged
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