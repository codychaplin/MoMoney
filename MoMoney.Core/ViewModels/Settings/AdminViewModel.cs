using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Data;
using MoMoney.Core.Helpers;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels.Settings;

public partial class AdminViewModel : ObservableObject
{
    readonly MoMoneydb momoney;
    readonly IStockService stockService;
    readonly IAccountService accountService;
    readonly ICategoryService categoryService;
    readonly ITransactionService transactionService;
    readonly ILoggerService<AdminViewModel> logger;

    [ObservableProperty]
    bool isAdmin;

    public AdminViewModel(MoMoneydb _momoney, ITransactionService _transactionService, IAccountService _accountService,
        ICategoryService _categoryService, IStockService _stockService, ILoggerService<AdminViewModel> _logger)
    {
        momoney = _momoney;
        transactionService = _transactionService;
        accountService = _accountService;
        categoryService = _categoryService;
        stockService = _stockService;
        logger = _logger;

        IsAdmin = Preferences.Get("IsAdmin", false);
    }

    /// <summary>
    /// Removes all Transactions from database.
    /// </summary>
    [RelayCommand]
    async Task RemoveAllTransactions()
    {
        bool flag = await Shell.Current.DisplayAlert("", "Are you sure you want to delete ALL Transactions?", "Yes", "No");
        if (!flag)
            return;

        try
        {
            int count = await transactionService.GetTransactionCount();
            await transactionService.RemoveAllTransactions();
            string message = count == 1 ? "1 transaction has been deleted." : $"{count} transactions have been deleted.";
            _ = Shell.Current.DisplayAlert("Success", message, "OK");
            logger.LogFirebaseEvent(FirebaseParameters.EVENT_REMOVE_ALL_TRANSACTIONS, FirebaseParameters.GetFirebaseParameters());
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(RemoveAllTransactions), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Removes all Accounts from database.
    /// </summary>
    [RelayCommand]
    async Task RemoveAllAccounts()
    {
        bool flag = await Shell.Current.DisplayAlert("", "Are you sure you want to delete ALL Accounts?", "Yes", "No");
        if (!flag)
            return;

        try
        {
            int count = await accountService.GetAccountCount();
            await accountService.RemoveAllAccounts();
            string message = count == 1 ? "1 account has been deleted." : $"{count} accounts have been deleted.";
            _ = Shell.Current.DisplayAlert("Success", message, "OK");
            logger.LogFirebaseEvent(FirebaseParameters.EVENT_REMOVE_ALL_ACCOUNTS, FirebaseParameters.GetFirebaseParameters());
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(RemoveAllAccounts), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Removes all Categories from database.
    /// </summary>
    [RelayCommand]
    async Task RemoveAllCategories()
    {
        bool flag = await Shell.Current.DisplayAlert("", "Are you sure you want to delete ALL Categories?", "Yes", "No");
        if (!flag)
            return;

        try
        {
            int count = await categoryService.GetCategoryCount();
            await categoryService.RemoveAllCategories();
            string message = count == 1 ? "1 category has been deleted." : $"{count} categories have been deleted.";
            _ = Shell.Current.DisplayAlert("Success", message, "OK");
            logger.LogFirebaseEvent(FirebaseParameters.EVENT_REMOVE_ALL_CATEGORIES, FirebaseParameters.GetFirebaseParameters());
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(RemoveAllCategories), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Removes all Stocks from database.
    /// </summary>
    [RelayCommand]
    async Task RemoveAllStocks()
    {
        bool flag = await Shell.Current.DisplayAlert("", "Are you sure you want to delete ALL Stocks?", "Yes", "No");
        if (!flag)
            return;

        try
        {
            int count = await stockService.GetStockCount();
            await stockService.RemoveStocks();
            string message = count == 1 ? "1 stock has been deleted." : $"{count} stocks have been deleted.";
            _ = Shell.Current.DisplayAlert("Success", message, "OK");
            logger.LogFirebaseEvent(FirebaseParameters.EVENT_REMOVE_ALL_STOCKS, FirebaseParameters.GetFirebaseParameters());
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(RemoveAllStocks), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Removes all Logs from database.
    /// </summary>
    [RelayCommand]
    async Task RemoveAllLogs()
    {
        bool flag = await Shell.Current.DisplayAlert("", "Are you sure you want to delete ALL Logs?", "Yes", "No");
        if (!flag)
            return;

        try
        {
            int count = await logger.GetLogCount();
            await logger.RemoveLogs();
            string message = count == 1 ? "1 log has been deleted." : $"{count} logs have been deleted.";
            _ = Shell.Current.DisplayAlert("Success", message, "OK");
            logger.LogFirebaseEvent(FirebaseParameters.EVENT_REMOVE_ALL_LOGS, FirebaseParameters.GetFirebaseParameters());
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(RemoveAllLogs), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Removes all data from database.
    /// </summary>
    [RelayCommand]
    async Task RemoveAllData()
    {
        bool flag = await Shell.Current.DisplayAlert("", "Are you sure you want to delete ALL data?", "Yes", "No");
        if (!flag)
            return;

        try
        {
            await momoney.ResetDb();
            _ = Shell.Current.DisplayAlert("Success", "All data has been deleted.", "OK");
            logger.LogFirebaseEvent(FirebaseParameters.EVENT_REMOVE_ALL_DATA, FirebaseParameters.GetFirebaseParameters());
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(RemoveAllLogs), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Calculates the current balance of each account and updates the database.
    /// </summary>
    [RelayCommand]
    async Task CalculateAccountBalances()
    {
        try
        {
            await transactionService.CalculateAccountBalances();
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(CalculateAccountBalances), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Goes to LoggingPage.xaml.
    /// </summary>
    [RelayCommand]
    async Task GoToLogging()
    {
        await Shell.Current.GoToAsync("LoggingPage");
    }

    /// <summary>
    /// Goes to BulkEditingPage.xaml.
    /// </summary>
    [RelayCommand]
    async Task GoToBulkEditing()
    {
        await Shell.Current.GoToAsync("BulkEditingPage");
    }
}