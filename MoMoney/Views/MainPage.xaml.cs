using MoMoney.ViewModels;
using Syncfusion.Maui.TabView;

namespace MoMoney.Views;

public partial class MainPage : ContentPage
{
	public static SfTabView TabView { get; private set; }

    public MainPage(HomeViewModel homeViewModel, TransactionsViewModel transactionsViewModel, AddTransactionViewModel addTransactionViewModel,
		StatsViewModel statsViewModel, SettingsViewModel settingsViewModel)
	{
		InitializeComponent();

		HomePageTab.Content = new HomePage(homeViewModel);
        TransactionsPageTab.Content = new TransactionsPage(transactionsViewModel);
        AddTransactionPageTab.Content = new AddTransactionPage(addTransactionViewModel);
        StatsPageTab.Content = new StatsPage(statsViewModel);
        SettingsPageTab.Content = new SettingsPage(settingsViewModel);

		TabView = TabBar;
		TabBar.SelectedIndex = 0;
    }

	private void TabBar_SelectionChanged(object sender, TabSelectionChangedEventArgs e)
	{
        var previousItem = e.OldIndex >= 0 ? TabBar.Items[(int)e.OldIndex] : null;
        var currentItem = e.NewIndex >= 0 ? TabBar.Items[(int)e.NewIndex] : null;
        string colour = (AppInfo.Current.RequestedTheme == AppTheme.Dark) ? "white" : "black";

        if (previousItem != null)
		{
			var imageSource = (FileImageSource)previousItem.ImageSource;
			string name = imageSource.File.Replace("green", colour);
			previousItem.ImageSource = name;
		}

        if (currentItem != null)
        {
            var imageSource = (FileImageSource)currentItem.ImageSource;
            string name = imageSource.File.Replace(colour, "green");
            currentItem.ImageSource = name;
        }

        if (TabBar.SelectedIndex == 0) // Home Page
		{
			HomePage.UpdatePage?.Invoke(this, new EventArgs());
		}
		else if (TabBar.SelectedIndex == 1) // TransactionPage
		{
			var args = new TransactionEventArgs(null, TransactionEventArgs.CRUD.Read);
			TransactionsPage.TransactionsChanged?.Invoke(this, args);
		}
	}
}