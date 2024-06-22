using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels.Settings.Edit;

public partial class EditStockViewModel : BaseEditViewModel<IStockService, EditStockViewModel>
{
    [ObservableProperty] Stock stock = new();

    Stock initalStock = null;

    public EditStockViewModel(IStockService _stockService, ILoggerService<EditStockViewModel> _logger) : base(_stockService, _logger) { }

    /// <summary>
    /// Controls whether the view is in edit mode or not.
    /// </summary>
    /// <param name="query"></param>
    public override void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query["Stock"] is not Stock stock)
            return;

        IsEditMode = true;
        Stock = new(stock);
        initalStock = new(stock);
    }

    /// <summary>
    /// adds Account to database using input fields from view.
    /// </summary>
    [RelayCommand]
    protected override async Task Add()
    {
        try
        {
            await service.AddStock(Stock.Symbol, Stock.Market, Stock.Quantity, Stock.Cost);
            logger.LogFirebaseEvent(FirebaseParameters.EVENT_ADD_STOCK, FirebaseParameters.GetFirebaseParameters());
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(Add), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Edits Stock in database using input fields from view.
    /// </summary>
    [RelayCommand]
    protected override async Task Edit()
    {
        try
        {
            await service.UpdateStock(Stock);

            logger.LogFirebaseEvent(FirebaseParameters.EVENT_EDIT_STOCK, FirebaseParameters.GetFirebaseParameters());
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(Edit), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Removes the Stock from the database.
    /// </summary>
    [RelayCommand]
    protected override async Task Remove()
    {
        bool flag = await Shell.Current.DisplayAlert("", $"Are you sure you want to delete \"{initalStock.Symbol}\"?", "Yes", "No");
        if (!flag)
            return;

        try
        {
            await service.RemoveStock(initalStock.StockID);
            logger.LogFirebaseEvent(FirebaseParameters.EVENT_DELETE_STOCK, FirebaseParameters.GetFirebaseParameters());
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(Remove), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }
}