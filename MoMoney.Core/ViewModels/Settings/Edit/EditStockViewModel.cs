using SQLite;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Models;
using MoMoney.Core.Exceptions;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels.Settings.Edit;

[QueryProperty(nameof(Symbol), nameof(Symbol))]
public partial class EditStockViewModel : ObservableObject
{
    readonly IStockService stockService;
    readonly ILoggerService<EditStockViewModel> logger;

    [ObservableProperty]
    public Stock stock = new();

    Stock initalStock;

    public string Symbol { get; set; } // Stock Symbol

    public EditStockViewModel(IStockService _stockService, ILoggerService<EditStockViewModel> _logger)
    {
        stockService = _stockService;
        logger = _logger;
    }

    /// <summary>
    /// Gets Stock using Symbol.
    /// </summary>
    public async Task GetStock()
    {
        try
        {
            Stock = await stockService.GetStock(Symbol);
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
            await logger.LogError(ex.Message, ex.GetType().Name);
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
                await stockService.UpdateStock(Stock, initalStock);
            else
                await stockService.UpdateStock(Stock);

            await Shell.Current.GoToAsync("..");
        }
        catch (SQLiteException ex)
        {
            await logger.LogCritical(ex.Message, ex.GetType().Name);
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

        if (!flag)
            return;

        try
        {
            await stockService.RemoveStock(Stock.Symbol);
            await Shell.Current.GoToAsync("..");
        }
        catch (SQLiteException ex)
        {
            await logger.LogCritical(ex.Message, ex.GetType().Name);
            await Shell.Current.DisplayAlert("Database Error", ex.Message, "OK");
        }
    }
}
