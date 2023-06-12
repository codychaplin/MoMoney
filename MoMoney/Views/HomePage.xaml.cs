using MoMoney.ViewModels;

namespace MoMoney.Views;

public partial class HomePage : ContentView
{
    HomeViewModel vm;

    public static EventHandler<EventArgs> UpdatePage { get; set; }

    public HomePage(HomeViewModel _vm)
	{
		InitializeComponent();
        vm = _vm;
        BindingContext = vm;

        // UpdatePage can be triggered by any page
        UpdatePage += Refresh;

        // Loaded is just called on start
        Loaded += Refresh;

        // DateSelected is for DatePicker changes
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
        // if triggered by tab bar (on MainPage), update dates
        if (sender is ContentPage)
        {
            dtFrom.Date = vm.From;
            dtTo.Date = vm.To;
        }

        // show/hide YAxis label amount
        if (Constants.ShowValue)
            YAxisLbl.LabelFormat = "$0,k";
        else
            YAxisLbl.LabelFormat = "$?";


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
}