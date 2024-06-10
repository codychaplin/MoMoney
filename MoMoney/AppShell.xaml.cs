using MoMoney.Views;
using MoMoney.Views.Stats;
using MoMoney.Views.Settings;
using MoMoney.Views.Settings.Edit;

namespace MoMoney;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// registered routes within app

		// accounts
		Routing.RegisterRoute(nameof(AccountsPage), typeof(AccountsPage));
		Routing.RegisterRoute(nameof(EditAccountPage), typeof(EditAccountPage));
		// categories
		Routing.RegisterRoute(nameof(CategoriesPage), typeof(CategoriesPage));
		Routing.RegisterRoute(nameof(EditCategoryPage), typeof(EditCategoryPage));
		// stocks
		Routing.RegisterRoute(nameof(StocksPage), typeof(StocksPage));
		Routing.RegisterRoute(nameof(EditStockPage), typeof(EditStockPage));
		// transactions
		Routing.RegisterRoute(nameof(EditTransactionPage), typeof(EditTransactionPage));
		// stats
		Routing.RegisterRoute(nameof(AccountSummaryPage), typeof(AccountSummaryPage));
		Routing.RegisterRoute(nameof(BreakdownPage), typeof(BreakdownPage));
		Routing.RegisterRoute(nameof(StockStatsPage), typeof(StockStatsPage));
		Routing.RegisterRoute(nameof(InsightsPage), typeof(InsightsPage));
		// settings
		Routing.RegisterRoute(nameof(AdminPage), typeof(AdminPage));
		Routing.RegisterRoute(nameof(LoggingPage), typeof(LoggingPage));
		Routing.RegisterRoute(nameof(ImportExportPage), typeof(ImportExportPage));
		Routing.RegisterRoute(nameof(BulkEditingPage), typeof(BulkEditingPage));
	}
}