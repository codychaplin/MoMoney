using Syncfusion.Maui.TabView;

namespace MoMoney.Views;

public partial class MainPage : ContentPage
{
	public static SfTabView TabView { get; private set; }

    public MainPage()
	{
		InitializeComponent();
		TabView = tvTabBar;
	}

	private void tvTabBar_SelectionChanged(object sender, TabSelectionChangedEventArgs e)
	{
		if (tvTabBar.SelectedIndex == 1) // TransactionPage
		{
			var args = new TransactionEventArgs(null, TransactionEventArgs.CRUD.Read);
			TransactionsPage.TransactionsChanged?.Invoke(this, args);
		}
	}
}