using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoMoney.Models;
using MoMoney.Services;
using MoMoney.Views.Settings;
using System.Collections.ObjectModel;

namespace MoMoney.ViewModels.Settings
{
    public partial class StockSettingsViewModel : ObservableObject
    {
        [ObservableProperty]
        public ObservableCollection<Stock> stocks = new();

        /// <summary>
        /// Goes to AddStockPage.xaml.
        /// </summary>
        [RelayCommand]
        async Task GoToAddStock()
        {
            await Shell.Current.GoToAsync(nameof(AddStockPage));
        }

        /// <summary>
        /// Goes to EditStockPage.xaml with a Symbol as a parameter.
        /// </summary>
        [RelayCommand]
        async Task GoToEditStock(string symbol)
        {
            await Shell.Current.GoToAsync($"{nameof(EditStockPage)}?Symbol={symbol}");
        }

        /// <summary>
        /// Gets updated stocks from database, orders them, and refreshes Stocks collection.
        /// </summary>
        public async void Refresh(object s, EventArgs e)
        {
            var stocks = await StockService.GetStocks();
            Stocks.Clear();
            foreach (var stock in stocks)
                Stocks.Add(stock);
        }
    }
}
