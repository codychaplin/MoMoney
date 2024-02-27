using CommunityToolkit.Maui;
using Syncfusion.Maui.Core.Hosting;
using UraniumUI;
using Plugin.Maui.Audio;
using MoMoney.Views;
using MoMoney.Views.Stats;
using MoMoney.Views.Settings;
using MoMoney.Views.Settings.Edit;
using MoMoney.Core.Data;
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
               .RegisterPages()
               .RegisterViewModels()
               .RegisterServices()
               .RegisterOther()
               .UseUraniumUI()
               .UseUraniumUIMaterial()
               .UseMauiCommunityToolkit()
               .ConfigureFonts(fonts =>
               {
                   fonts.AddFont("MaterialIcons-Regular.ttf", "Material");
                   fonts.AddMaterialIconFonts();
               });

        return builder.Build();
	}

    static MauiAppBuilder RegisterServices(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<IAccountService, AccountService>();
        builder.Services.AddSingleton<ICategoryService, CategoryService>();
        builder.Services.AddSingleton<IStockService, StockService>();
        builder.Services.AddSingleton<ITransactionService, TransactionService>();

        return builder;
    }

    static MauiAppBuilder RegisterPages(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<MainPage>();

        // tabs
        builder.Services.AddSingleton<HomePage>();
        builder.Services.AddSingleton<TransactionsPage>();
        builder.Services.AddSingleton<AddTransactionPage>();
        builder.Services.AddSingleton<StatsPage>();
        builder.Services.AddSingleton<SettingsPage>();

        // stats
        builder.Services.AddTransient<AccountSummaryPage>();
        builder.Services.AddTransient<BreakdownPage>();
        builder.Services.AddTransient<InsightsPage>();
        builder.Services.AddTransient<StockStatsPage>();

        // settings
        builder.Services.AddTransient<AdminPage>();
        builder.Services.AddTransient<BulkEditingPage>();
        builder.Services.AddTransient<ImportExportPage>();
        builder.Services.AddTransient<LoggingPage>();

        // models
        builder.Services.AddTransient<EditTransactionPage>();
        builder.Services.AddTransient<AccountsPage>();
        builder.Services.AddTransient<AddAccountPage>();
        builder.Services.AddTransient<EditAccountPage>();
        builder.Services.AddTransient<CategoriesPage>();
        builder.Services.AddTransient<AddCategoryPage>();
        builder.Services.AddTransient<EditCategoryPage>();
        builder.Services.AddTransient<StocksPage>();
        builder.Services.AddTransient<AddStockPage>();
        builder.Services.AddTransient<EditStockPage>();

        return builder;
    }

    static MauiAppBuilder RegisterViewModels(this MauiAppBuilder builder)
    {
        // tabs
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<TransactionsViewModel>();
        builder.Services.AddTransient<AddTransactionViewModel>();
        builder.Services.AddTransient<StatsViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

        // stats
        builder.Services.AddTransient<AccountSummaryViewModel>();
        builder.Services.AddTransient<BreakdownViewModel>();
        builder.Services.AddTransient<InsightsViewModel>();
        builder.Services.AddTransient<StockStatsViewModel>();

        // settings
        builder.Services.AddTransient<AdminViewModel>();
        builder.Services.AddTransient<BulkEditingViewModel>();
        builder.Services.AddTransient<ImportExportViewModel>();
        builder.Services.AddTransient<LoggingViewModel>();

        // models
        builder.Services.AddTransient<EditTransactionViewModel>();
        builder.Services.AddTransient<AccountsViewModel>();
        builder.Services.AddTransient<AddAccountViewModel>();
        builder.Services.AddTransient<EditAccountViewModel>();
        builder.Services.AddTransient<CategoriesViewModel>();
        builder.Services.AddTransient<AddCategoryViewModel>();
        builder.Services.AddTransient<EditCategoryViewModel>();
        builder.Services.AddTransient<StocksViewModel>();
        builder.Services.AddTransient<AddStockViewModel>();
        builder.Services.AddTransient<EditStockViewModel>();

        return builder;
    }

    static MauiAppBuilder RegisterOther(this MauiAppBuilder builder)
    {
         // db
        builder.Services.AddSingleton<MoMoneydb>();

        // logging
        builder.Services.AddTransient(typeof(ILoggerService<>), typeof(LoggerService<>));

        // openai
        builder.Services.AddSingleton<IOpenAIService, OpenAIService>();

        // audio
        builder.Services.AddSingleton(AudioManager.Current);

        return builder;
    }
}