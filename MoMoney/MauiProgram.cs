using CommunityToolkit.Maui;
using Syncfusion.Maui.Core.Hosting;
using MoMoney.Core.Data;
using MoMoney.Views;
using MoMoney.Views.Stats;
using MoMoney.Views.Settings;
using MoMoney.Views.Settings.Edit;
using MoMoney.Core.Services;
using MoMoney.Core.Services.Interfaces;
using MoMoney.Core.ViewModels;
using MoMoney.Core.ViewModels.Stats;
using MoMoney.Core.ViewModels.Settings;
using MoMoney.Core.ViewModels.Settings.Edit;

namespace MoMoney;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder.UseMauiApp<App>()
			   .ConfigureSyncfusionCore()
               .UseMauiCommunityToolkit();

        InjectDependencies(ref builder);

        return builder.Build();
	}

	static void InjectDependencies(ref MauiAppBuilder builder)
	{
        // db
        builder.Services.AddSingleton<MoMoneydb>();

        // logging
        builder.Services.AddTransient(typeof(ILoggerService<>), typeof(LoggerService<>));

        // services
        builder.Services.AddSingleton<IAccountService, AccountService>();
        builder.Services.AddSingleton<ICategoryService, CategoryService>();
        builder.Services.AddSingleton<IStockService, StockService>();
        builder.Services.AddSingleton<ITransactionService, TransactionService>();

        // pages and viewmodels
        builder.Services.AddSingleton<MainPage>();

        builder.Services.AddSingleton<HomePage>();
        builder.Services.AddTransient<HomeViewModel>();

        builder.Services.AddSingleton<TransactionsPage>();
        builder.Services.AddTransient<TransactionsViewModel>();

        builder.Services.AddSingleton<AddTransactionPage>();
        builder.Services.AddTransient<AddTransactionViewModel>();

        builder.Services.AddTransient<EditTransactionPage>();
        builder.Services.AddTransient<EditTransactionViewModel>();

        builder.Services.AddSingleton<StatsPage>();
        builder.Services.AddTransient<StatsViewModel>();

        builder.Services.AddSingleton<SettingsPage>();
        builder.Services.AddTransient<SettingsViewModel>();

        builder.Services.AddSingleton<AccountsPage>();
        builder.Services.AddTransient<AccountsViewModel>();

        builder.Services.AddSingleton<CategoriesPage>();
        builder.Services.AddTransient<CategoriesViewModel>();

        builder.Services.AddSingleton<StocksPage>();
        builder.Services.AddTransient<StocksViewModel>();

        builder.Services.AddTransient<ImportExportPage>();
        builder.Services.AddTransient<ImportExportViewModel>();

        builder.Services.AddTransient<AdminPage>();
        builder.Services.AddTransient<AdminViewModel>();

        builder.Services.AddTransient<LoggingPage>();
        builder.Services.AddTransient<LoggingViewModel>();

        builder.Services.AddTransient<AccountSummaryPage>();
        builder.Services.AddTransient<AccountSummaryViewModel>();

        builder.Services.AddTransient<BreakdownPage>();
        builder.Services.AddTransient<BreakdownViewModel>();

        builder.Services.AddTransient<InsightsPage>();
        builder.Services.AddTransient<InsightsViewModel>();

        builder.Services.AddTransient<StockStatsPage>();
        builder.Services.AddTransient<StockStatsViewModel>();

        builder.Services.AddTransient<AddAccountPage>();
        builder.Services.AddTransient<AddAccountViewModel>();

        builder.Services.AddTransient<EditAccountPage>();
        builder.Services.AddTransient<EditAccountViewModel>();

        builder.Services.AddTransient<AddCategoryPage>();
        builder.Services.AddTransient<AddCategoryViewModel>();

        builder.Services.AddTransient<EditCategoryPage>();
        builder.Services.AddTransient<EditCategoryViewModel>();

        builder.Services.AddTransient<AddStockPage>();
        builder.Services.AddTransient<AddStockViewModel>();

        builder.Services.AddTransient<EditStockPage>();
        builder.Services.AddTransient<EditStockViewModel>();

        builder.Services.AddTransient<BulkEditingPage>();
        builder.Services.AddTransient<BulkEditingViewModel>();
    }
}