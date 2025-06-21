using CommunityToolkit.Mvvm.Messaging;
using MoMoney.Core.Helpers;

namespace MoMoney.Views;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();

		MainTabView.SelectionChanged += (s, e) =>
		{
			if (e.NewIndex == 1)
			{
				var args = new TransactionEventArgs(null, TransactionEventArgs.CRUD.Read);
				WeakReferenceMessenger.Default.Send(new UpdateTransactionsMessage(args));
			}
		};

		WeakReferenceMessenger.Default.Register<ChangeTabMessage>(this, (r, m) =>
		{
			MainTabView.SelectedIndex = m.Value;
		});
	}
}