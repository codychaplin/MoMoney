using CommunityToolkit.Mvvm.Messaging;
using MoMoney.Core.Helpers;

namespace MoMoney.Views;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();

		MainTabView.SelectedTabChanged += (s, e) =>
		{
			if (e.Data.ToString() == "1")
			{
				var args = new TransactionEventArgs(null, TransactionEventArgs.CRUD.Read);
				WeakReferenceMessenger.Default.Send(new UpdateTransactionsMessage(args));
			}
		};

		// Initialize the tabs so they load faster when navigated to for the first time
		TransactionsPageTab.Command.Execute(null);
		SettingsPageTab.Command.Execute(null);
		StatsPageTab.Command.Execute(null);
		AddTransactionPageTab.Command.Execute(null);
		HomePageTab.Command.Execute(null);

		WeakReferenceMessenger.Default.Register<ChangeTabMessage>(this, (r, m) =>
		{
			switch (m.Value)
			{
				case 0:
					HomePageTab.Command.Execute(null);
					break;
				case 1:
					TransactionsPageTab.Command.Execute(null);
					break;
				case 2:
					AddTransactionPageTab.Command.Execute(null);
					break;
				case 3:
					StatsPageTab.Command.Execute(null);
					break;
				case 4:
					SettingsPageTab.Command.Execute(null);
					break;
			}
		});
	}
}