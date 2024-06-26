﻿using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MvvmHelpers;
using MoMoney.Core.Models;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels.Settings.Edit;

public partial class StocksViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    readonly IStockService stockService;
    readonly ILoggerService<StocksViewModel> logger;

    [ObservableProperty] ObservableRangeCollection<Stock> stocks = [];

    public StocksViewModel(IStockService _stockService, ILoggerService<StocksViewModel> _logger)
    {
        stockService = _stockService;
        logger = _logger;
    }

    /// <summary>
    /// Gets updated stocks from database, orders them, and refreshes Stocks collection.
    /// </summary>
    public async void RefreshStocks(object s, EventArgs e)
    {
        try
        {
            await Task.Delay(1);
            var stocks = await stockService.GetStocks();
            Stocks.ReplaceRange(stocks);
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(RefreshStocks), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Goes to the add version of EditStockPage.xaml.
    /// </summary>
    [RelayCommand]
    async Task GoToAddStock()
    {
        await Shell.Current.GoToAsync($"EditStockPage", new ShellNavigationQueryParameters() { { "Stock", null } });
    }

    /// <summary>
    /// Goes to EditStockPage.xaml with a Stock as a parameter.
    /// </summary>
    [RelayCommand]
    async Task GoToEditStock(Stock stock)
    {
        await Shell.Current.GoToAsync($"EditStockPage", new ShellNavigationQueryParameters() { { "Stock", stock } });
    }
}