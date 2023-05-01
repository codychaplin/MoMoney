using SQLite;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Models;
using MoMoney.Services;
using MoMoney.Exceptions;

namespace MoMoney.ViewModels.Settings;

[QueryProperty(nameof(Symbol), nameof(Symbol))]
public partial class EditStockViewModel : ObservableObject
{
    [ObservableProperty]
    public Stock stock = new();

    Stock initalStock;

    public string Symbol { get; set; } // Stock Symbol

    /// <summary>
    /// Gets Stock using Symbol.
    /// </summary>
    public async Task GetStock()
    {
        try
        {
            Stock = await StockService.GetStock(Symbol);
            initalStock = new Stock
            {
                Symbol = Stock.Symbol,
                Quantity = Stock.Quantity,
                Cost = Stock.Cost,
                MarketPrice = Stock.MarketPrice,
                BookValue = Stock.BookValue
            };
        }
        catch (StockNotFoundException ex)
        {
            await Shell.Current.DisplayAlert("Stock Not Found Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Edits Stock in database using input fields from view.
    /// </summary>
    [RelayCommand]
    async Task Edit()
    {
        try
        {
            // if symbol (primary key) changed, 
            if (initalStock.Symbol != Stock.Symbol)
                await StockService.UpdateStock(Stock, initalStock);
            else
                await StockService.UpdateStock(Stock);

            await Shell.Current.GoToAsync("..");
        }
        catch (SQLiteException ex)
        {
            await Shell.Current.DisplayAlert("Database Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Removes the Stock from the database.
    /// </summary>
    [RelayCommand]
    async Task Remove()
    {
        bool flag = await Shell.Current.DisplayAlert("", $"Are you sure you want to delete \"{Stock.Symbol}\"?", "Yes", "No");

        if (flag)
        {
            try
            {
                await StockService.RemoveStock(Stock.Symbol);
                await Shell.Current.GoToAsync("..");
            }
            catch (SQLiteException ex)
            {
                await Shell.Current.DisplayAlert("Database Error", ex.Message, "OK");
            }
        }
    }
}
