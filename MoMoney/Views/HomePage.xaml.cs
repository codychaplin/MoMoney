using MoMoney.ViewModels;

namespace MoMoney.Views;

public partial class HomePage : ContentView
{
    HomePageViewModel vm;

    public static EventHandler<EventArgs> UpdatePage { get; set; }

    public HomePage()
	{
		InitializeComponent();
        vm = (HomePageViewModel)BindingContext;
        // first two months, show 1 year, starting March show YTD
        vm.From = (DateTime.Today.Month <= 2) ? DateTime.Today.AddYears(-1) : new(DateTime.Today.Year, 1, 1);
        vm.To = DateTime.Today;
        UpdatePage += Refresh;
        Loaded += Refresh;
        dtFrom.DateSelected += Refresh;
        dtTo.DateSelected += Refresh;
    }

    /// <summary>
    /// Refreshes data on page. 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    async void Refresh(object sender, EventArgs e)
    {
        if (sender is ContentPage)
        {
            dtFrom.Date = vm.From;
            dtTo.Date = vm.To;
        }
        await vm.Refresh();
    }

    /// <summary>
    /// On click, change to StatsPage tab.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnViewAllStats_Clicked(object sender, EventArgs e)
    {
        MainPage.TabView.SelectedIndex = 3;
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
    /// Changes visibility of date range picker and resizes screen to fit.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ImageButton_Clicked(object sender, EventArgs e)
	{
        // change date picker visibility and change height of row
        if (frDates.IsVisible)
        {
            frDates.IsVisible = false;
            grdMain.RowDefinitions[0].Height = 40;
        }
        else
        {
            frDates.IsVisible = true;
            grdMain.RowDefinitions[0].Height = 90;
        }
    }
}