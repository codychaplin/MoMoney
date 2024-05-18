using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Helpers;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels.Settings.Edit;

public partial class AddStockViewModel : ObservableObject
{
    readonly IStockService stockService;
    readonly ILoggerService<AddStockViewModel> logger;

    [ObservableProperty] string symbol; // stock symbol
    [ObservableProperty] int quantity; // quantity owned
    [ObservableProperty] decimal cost; // purchase price per share
    [ObservableProperty] decimal marketPrice; // current price
    [ObservableProperty] decimal bookValue; // total price paid

    public AddStockViewModel(IStockService _stockService, ILoggerService<AddStockViewModel> _logger)
    {
        stockService = _stockService;
        logger = _logger;
    }

    /// <summary>
    /// adds Account to database using input fields from view.
    /// </summary>
    [RelayCommand]
    async Task AddStock()
    {
        try
        {
            await stockService.AddStock(Symbol, Quantity, Cost, MarketPrice, BookValue);
            logger.LogFirebaseEvent(FirebaseParameters.EVENT_ADD_STOCK, FirebaseParameters.GetFirebaseParameters());
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(AddStock), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }
}