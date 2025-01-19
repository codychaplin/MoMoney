using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Models;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels.Settings.Edit;

public partial class StocksViewModel : ObservableObject
{
    readonly IStockService stockService;
    readonly ILoggerService<StocksViewModel> logger;

    [ObservableProperty] ObservableCollection<Stock> stocks = [];

    public StocksViewModel(IStockService _stockService, ILoggerService<StocksViewModel> _logger)
    {
        stockService = _stockService;
        logger = _logger;
    }

    /// <summary>
    /// Gets updated stocks from database, orders them, and refreshes Stocks collection.
    /// </summary>
    public async Task LoadStocks()
    {
        try
        {
            var stocks = await stockService.GetStocks();
            Stocks.Clear();
            foreach (var stock in stocks)
                Stocks.Add(stock);
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(LoadStocks), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Goes to the add version of EditStockPage.xaml.
    /// </summary>
    [RelayCommand]
    async Task GoToAddStock()
    {
        await Shell.Current.GoToAsync($"EditStockPage", new ShellNavigationQueryParameters() { { "Stock", null! } });
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