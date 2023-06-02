using SQLite;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Services;

namespace MoMoney.ViewModels.Settings;

public partial class AddStockViewModel : ObservableObject
{
    readonly IStockService stockService;

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

    public AddStockViewModel(IStockService _stockService)
    {
        stockService = _stockService;
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
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }
}