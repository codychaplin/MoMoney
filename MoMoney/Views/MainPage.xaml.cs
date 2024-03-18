using MoMoney.Core.Helpers;
using UraniumUI.Material.Controls;

namespace MoMoney.Views;

public partial class MainPage : ContentPage
{
	public static TabView TabView { get; private set; }

    public MainPage()
	{
		InitializeComponent();

		TabView = MainTabView;
        TabView.SelectedTabChanged += (s, e) =>
		{
			switch (e.Data)
			{
				case "0":
					HomePage.UpdatePage?.Invoke(this, new EventArgs());
                    break;
				case "1":
                    var args = new TransactionEventArgs(null, TransactionEventArgs.CRUD.Read);
                    TransactionsPage.TransactionsChanged?.Invoke(this, args);
                    break;
				default:
                    break;
			}
		};
    }
}