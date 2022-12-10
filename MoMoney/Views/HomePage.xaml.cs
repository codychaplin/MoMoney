using MoMoney.ViewModels;

namespace MoMoney.Views;

public partial class HomePage : ContentPage
{
    HomePageViewModel vm;

    public HomePage()
	{
		InitializeComponent();
        vm = (HomePageViewModel)BindingContext;
    }

    /// <summary>
    /// On click, change to TransactionsPage tab.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
	private void btnViewAllTransactions_Clicked(object sender, EventArgs e)
	{
		MainPage.TabView.SelectedIndex = 1;
    }

    /// <summary>
    /// On load, get the user's networth, get data for chart, and get 5 most recent Transactions from the database.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
	private async void ContentPage_Loaded(object sender, EventArgs e)
	{
		await vm.GetTotalBalance();
		await vm.GetRecentTransactions();
	}

    /// <summary>
    /// Changes visibility of date range picker and resizes screen to fit.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ImageButton_Clicked(object sender, EventArgs e)
	{
        // change date picker visibility, change height of row, and change top margin to fit date picker
        if (frDates.IsVisible)
        {
            frDates.IsVisible = false;
            grdMain.RowDefinitions[1].Height = 200;
            chrtBalance.Margin = new Thickness(0, 0, 10, 10);
        }
        else
        {
            frDates.IsVisible = true;
            grdMain.RowDefinitions[1].Height = 250;
            chrtBalance.Margin = new Thickness(0, 50, 10, 10);
        }
    }

    /// <summary>
    /// Update chart data when From date is changed
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void dtFrom_DateSelected(object sender, DateChangedEventArgs e)
    {
        await Task.Delay(500);
        await vm.GetChartData();
    }

    /// <summary>
    /// Update chart data when To date is changed
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void dtTo_DateSelected(object sender, DateChangedEventArgs e)
    {
        await Task.Delay(500);
        await vm.GetChartData();
    }
}