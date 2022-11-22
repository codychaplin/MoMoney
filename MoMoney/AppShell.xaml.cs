using MoMoney.Views;
using MoMoney.Views.Settings;

namespace MoMoney;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// registered routes within app
		Routing.RegisterRoute(nameof(AccountsPage), typeof(AccountsPage));
		Routing.RegisterRoute(nameof(AddAccountPage), typeof(AddAccountPage));
		Routing.RegisterRoute(nameof(EditAccountPage), typeof(EditAccountPage));
		Routing.RegisterRoute(nameof(CategoriesPage), typeof(CategoriesPage));
		Routing.RegisterRoute(nameof(AddCategoryPage), typeof(AddCategoryPage));
		Routing.RegisterRoute(nameof(EditCategoryPage), typeof(EditCategoryPage));
		Routing.RegisterRoute(nameof(EditTransactionPage), typeof(EditTransactionPage));
	}
}
