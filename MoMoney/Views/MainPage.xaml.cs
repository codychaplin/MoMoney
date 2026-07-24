using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
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

		Application.Current!.RequestedThemeChanged += (s, e) => UpdateStatusBar();
		UpdateStatusBar();
	}

	/// <summary>
	/// Update Status bar colour to match requested theme
	/// (Doesn't work in xaml, has to be here)
	/// </summary>
	void UpdateStatusBar()
	{
		var isDark = Application.Current!.UserAppTheme switch
		{
			AppTheme.Dark => true,
			AppTheme.Light => false,
			_ => Application.Current.RequestedTheme == AppTheme.Dark
		};

		var statusBarColor = Utilities.GetColour("secondaryContainer", "secondaryContainerDark");
		var statusBarStyle = isDark
			? StatusBarStyle.LightContent
			: StatusBarStyle.DarkContent;

		Behaviors.Clear();
		Behaviors.Add(new StatusBarBehavior
		{
			StatusBarColor = statusBarColor,
			StatusBarStyle = statusBarStyle
		});
	}
}