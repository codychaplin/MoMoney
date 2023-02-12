using MoMoney.Views;
using MoMoney.Views.Settings;
using MoMoney.Views.Stats;

namespace MoMoney;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// registered routes within app

		// settings
		Routing.RegisterRoute(nameof(AccountsPage), typeof(AccountsPage));
		Routing.RegisterRoute(nameof(AddAccountPage), typeof(AddAccountPage));
		Routing.RegisterRoute(nameof(EditAccountPage), typeof(EditAccountPage));
		Routing.RegisterRoute(nameof(CategoriesPage), typeof(CategoriesPage));
		Routing.RegisterRoute(nameof(AddCategoryPage), typeof(AddCategoryPage));
		Routing.RegisterRoute(nameof(EditCategoryPage), typeof(EditCategoryPage));
		Routing.RegisterRoute(nameof(StockSettingsPage), typeof(StockSettingsPage));
		Routing.RegisterRoute(nameof(AddStockPage), typeof(AddStockPage));
		Routing.RegisterRoute(nameof(EditStockPage), typeof(EditStockPage));
		// transactions
		Routing.RegisterRoute(nameof(EditTransactionPage), typeof(EditTransactionPage));
		// stats
		Routing.RegisterRoute(nameof(AccountSummaryPage), typeof(AccountSummaryPage));
		Routing.RegisterRoute(nameof(MonthBreakdownPage), typeof(MonthBreakdownPage));
		Routing.RegisterRoute(nameof(StocksPage), typeof(StocksPage));
	}
}
