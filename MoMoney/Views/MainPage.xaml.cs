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

		TabView = tvTabBar;
    }

	private void tvTabBar_SelectionChanged(object sender, TabSelectionChangedEventArgs e)
	{
		if (tvTabBar.SelectedIndex == 0) // Home Page
		{
			HomePage.UpdatePage?.Invoke(this, new EventArgs());
		}
		else if (tvTabBar.SelectedIndex == 1) // TransactionPage
		{
			var args = new TransactionEventArgs(null, TransactionEventArgs.CRUD.Read);
			TransactionsPage.TransactionsChanged?.Invoke(this, args);
		}
	}
}