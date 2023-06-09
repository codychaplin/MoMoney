﻿using CommunityToolkit.Maui;
using Syncfusion.Maui.Core.Hosting;
using MoMoney.Data;
using MoMoney.Views;
using MoMoney.Services;
using MoMoney.ViewModels;
using MoMoney.ViewModels.Stats;
using MoMoney.ViewModels.Settings;
using MoMoney.Views.Settings;
using MoMoney.Views.Stats;

namespace MoMoney;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
            .ConfigureSyncfusionCore()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("Metropolis-Regular.otf", "Metropolis");
				fonts.AddFont("Metropolis-Bold.otf", "MetropolisBold");
			});

		// db
		builder.Services.AddSingleton<MoMoneydb>();

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

		builder.Services.AddSingleton<StockSettingsPage>();
		builder.Services.AddTransient<StockSettingsViewModel>();

		builder.Services.AddTransient<AccountSummaryPage>();
		builder.Services.AddTransient<AccountSummaryViewModel>();

		builder.Services.AddTransient<BreakdownPage>();
		builder.Services.AddTransient<BreakdownViewModel>();

		builder.Services.AddTransient<InsightsPage>();
		builder.Services.AddTransient<InsightsViewModel>();

		builder.Services.AddTransient<StocksPage>();
		builder.Services.AddTransient<StocksViewModel>();

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

        return builder.Build();
	}
}