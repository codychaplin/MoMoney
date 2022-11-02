using MoMoney.Views;

namespace MoMoney;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// registered routes within app
		Routing.RegisterRoute(nameof(AccountsPage), typeof(AccountsPage));
	}
}
