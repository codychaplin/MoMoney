using Microsoft.Maui.LifecycleEvents;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
using Syncfusion.Maui.Core.Hosting;
using UraniumUI;
using MoMoney.Views;
using MoMoney.Views.Stats;
using MoMoney.Views.Settings;
using MoMoney.Views.Settings.Edit;
using MoMoney.Core.Data;
using MoMoney.Core.Services;
using MoMoney.Core.Services.Interfaces;
using MoMoney.Core.Platforms.Android;
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
               .RegisterFirebase()
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
        builder.Services.AddSingleton<IRecordAudioService, RecordAudioService>();

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
        builder.Services.AddTransient<EditAccountPage>();
        builder.Services.AddTransient<CategoriesPage>();
        builder.Services.AddTransient<EditCategoryPage>();
        builder.Services.AddTransient<StocksPage>();
        builder.Services.AddTransient<EditStockPage>();

        return builder;
    }

    static MauiAppBuilder RegisterViewModels(this MauiAppBuilder builder)
    {
        // tabs
        builder.Services.AddSingleton<HomeViewModel>();
        builder.Services.AddSingleton<TransactionsViewModel>();
        builder.Services.AddSingleton<AddTransactionViewModel>();
        builder.Services.AddSingleton<StatsViewModel>();
        builder.Services.AddSingleton<SettingsViewModel>();

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
        builder.Services.AddTransient<EditAccountViewModel>();
        builder.Services.AddTransient<CategoriesViewModel>();
        builder.Services.AddTransient<EditCategoryViewModel>();
        builder.Services.AddTransient<StocksViewModel>();
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

        // file saver
        builder.Services.AddSingleton(FileSaver.Default);

        return builder;
    }

    static MauiAppBuilder RegisterFirebase(this MauiAppBuilder builder)
    {
        builder.ConfigureLifecycleEvents(events =>
        {
#if ANDROID
            events.AddAndroid(android => android.OnCreate((activity, bundle) =>
            {
                Firebase.FirebaseApp.InitializeApp(activity);
            }));
#endif
        });

        return builder;
    }
}