﻿using CommunityToolkit.Mvvm.Input;
using MoMoney.Views.Stats;

namespace MoMoney.ViewModels;

public partial class StatsViewModel
{
    /// <summary>
    /// Goes to AccountSummaryPage.xaml.
    /// </summary>
    [RelayCommand]
    async Task GoToAccountSummary()
    {
        await Shell.Current.GoToAsync(nameof(AccountSummaryPage));
    }

    /// <summary>
    /// Goes to MonthBreakdownPage.xaml.
    /// </summary>
    [RelayCommand]
    async Task GoToBreakdown()
    {
        await Shell.Current.GoToAsync(nameof(BreakdownPage));
    }

    /// <summary>
    /// Goes to StocksPage.xaml.
    /// </summary>
    [RelayCommand]
    async Task GoToStocks()
    {
        await Shell.Current.GoToAsync(nameof(StockStatsPage));
    }

    /// <summary>
    /// Goes to InsightsPage.xaml.
    /// </summary>
    [RelayCommand]
    async Task GoToInsights()
    {
        await Shell.Current.GoToAsync(nameof(InsightsPage));
    }
}