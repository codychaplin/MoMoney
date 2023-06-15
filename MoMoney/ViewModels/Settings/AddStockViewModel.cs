using SQLite;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Services;

namespace MoMoney.ViewModels.Settings;

public partial class AddStockViewModel : ObservableObject
{
    readonly IStockService stockService;
    readonly ILoggerService<AddStockViewModel> logger;

    [ObservableProperty]
    public string symbol; // stock symbol

    [ObservableProperty]
    public int quantity; // quantity owned

    [ObservableProperty]
    public decimal cost; // purchase price per share

    [ObservableProperty]
    public decimal marketPrice; // current price

    [ObservableProperty]
    public decimal bookValue; // total price paid

    public AddStockViewModel(IStockService _stockService, ILoggerService<AddStockViewModel> _logger)
    {
        stockService = _stockService;
        logger = _logger;
    }

    /// <summary>
    /// adds Account to database using input fields from view.
    /// </summary>
    [RelayCommand]
    async Task Add()
    {
        try
        {
            await stockService.AddStock(Symbol, Quantity, Cost, MarketPrice, BookValue);
            await Shell.Current.GoToAsync("..");
        }
        catch (SQLiteException ex)
        {
            await logger.LogCritical(ex.Message, ex.GetType().Name);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }
}