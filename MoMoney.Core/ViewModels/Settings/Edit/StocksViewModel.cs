using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Models;
using MoMoney.Core.Services;

namespace MoMoney.Core.ViewModels.Settings.Edit;

public partial class StocksViewModel : ObservableObject
{
    readonly IStockService stockService;

    [ObservableProperty]
    public ObservableCollection<Stock> stocks = new();

    public StocksViewModel(IStockService _stockService)
    {
        stockService = _stockService;
    }

    /// <summary>
    /// Goes to AddStockPage.xaml.
    /// </summary>
    [RelayCommand]
    async Task GoToAddStock()
    {
        await Shell.Current.GoToAsync("AddStockPage");
    }

    /// <summary>
    /// Goes to EditStockPage.xaml with a Symbol as a parameter.
    /// </summary>
    [RelayCommand]
    async Task GoToEditStock(string symbol)
    {
        await Shell.Current.GoToAsync($"EditStockPage?Symbol={symbol}");
    }

    /// <summary>
    /// Gets updated stocks from database, orders them, and refreshes Stocks collection.
    /// </summary>
    public async void Refresh(object s, EventArgs e)
    {
        var stocks = await stockService.GetStocks();
        Stocks.Clear();
        foreach (var stock in stocks)
            Stocks.Add(stock);
    }
}