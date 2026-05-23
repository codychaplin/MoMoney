using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MoMoney.Core.Data;
using MoMoney.Core.Helpers;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels.Settings;

public partial class AdminViewModel : ObservableObject
{
    readonly IMoMoneydb momoney;
    readonly IStockService stockService;
    readonly IAccountService accountService;
    readonly ICategoryService categoryService;
    readonly ITransactionService transactionService;
    readonly IMappingRuleService mappingRuleService;
    readonly ILoggerService<AdminViewModel> logger;

    [ObservableProperty]
    bool isAdmin;

    [ObservableProperty]
    bool transactionDictationEnabled;

    public AdminViewModel(IMoMoneydb _momoney, ITransactionService _transactionService, IAccountService _accountService,
        ICategoryService _categoryService, IStockService _stockService, IMappingRuleService _mappingRuleService, ILoggerService<AdminViewModel> _logger)
    {
        momoney = _momoney;
        transactionService = _transactionService;
        accountService = _accountService;
        categoryService = _categoryService;
        stockService = _stockService;
        mappingRuleService = _mappingRuleService;
        logger = _logger;

        IsAdmin = Utilities.IsAdmin;
        TransactionDictationEnabled = Utilities.TransactionDictationEnabled;
    }

    /// <summary>
    /// Generic method to remove all objects from database
    /// </summary>
    /// <param name="typeSingular"></param>
    /// <param name="typePlural"></param>
    /// <param name="eventName"></param>
    /// <param name="functionName"></param>
    /// <param name="getCount"></param>
    /// <param name="removeAll"></param>
    async Task RemoveAllObjects(string typeSingular, string typePlural, string eventName, string functionName,
        Func<Task<int>> getCount, Func<Task> removeAll)
    {
        bool flag = await Shell.Current.DisplayAlertAsync("", $"Are you sure you want to delete ALL {typePlural}?", "Yes", "No");
        if (!flag)
            return;

        try
        {
            int count = await getCount();
            await removeAll();
            string message = count == 1 ? $"1 {typeSingular} has been deleted." : $"{count} {typePlural} have been deleted.";
            _ = Shell.Current.DisplayAlertAsync("Success", message, "OK");
            logger.LogFirebaseEvent(eventName, FirebaseParameters.GetFirebaseParameters());
        }
        catch (Exception ex)
        {
            await logger.LogError(functionName, ex);
            await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Removes all Transactions from database.
    /// </summary>
    [RelayCommand]
    Task RemoveAllTransactions() =>
        RemoveAllObjects("transaction", "Transactions", FirebaseParameters.EVENT_REMOVE_ALL_TRANSACTIONS,
            nameof(RemoveAllTransactions), transactionService.GetTransactionCount, transactionService.RemoveAllTransactions);

    /// <summary>
    /// Removes all Accounts from database.
    /// </summary>
    [RelayCommand]
    Task RemoveAllAccounts() =>
        RemoveAllObjects("account", "Accounts", FirebaseParameters.EVENT_REMOVE_ALL_ACCOUNTS,
            nameof(RemoveAllAccounts), accountService.GetAccountCount, accountService.RemoveAllAccounts);

    /// <summary>
    /// Removes all Categories from database.
    /// </summary>
    [RelayCommand]
    Task RemoveAllCategories() =>
        RemoveAllObjects("category", "Categories", FirebaseParameters.EVENT_REMOVE_ALL_CATEGORIES,
            nameof(RemoveAllCategories), categoryService.GetCategoryCount, categoryService.RemoveAllCategories);

    /// <summary>
    /// Removes all Stocks from database.
    /// </summary>
    [RelayCommand]
    Task RemoveAllStocks() =>
        RemoveAllObjects("stock", "Stocks", FirebaseParameters.EVENT_REMOVE_ALL_STOCKS,
            nameof(RemoveAllStocks), stockService.GetStockCount, stockService.RemoveStocks);

    /// <summary>
    /// Removes all Logs from database.
    /// </summary>
    [RelayCommand]
    Task RemoveAllLogs() =>
        RemoveAllObjects("log", "Logs", FirebaseParameters.EVENT_REMOVE_ALL_LOGS,
            nameof(RemoveAllLogs), logger.GetLogCount, logger.RemoveLogs);

    /// <summary>
    /// Removes all Mapping Rules from database.
    /// </summary>
    [RelayCommand]
    Task RemoveAllMappingRules() =>
        RemoveAllObjects("rule", "Mapping Rules", FirebaseParameters.EVENT_REMOVE_ALL_MAPPING_RULES,
            nameof(RemoveAllMappingRules), mappingRuleService.GetMappingRuleCount, mappingRuleService.RemoveAllRules);

    /// <summary>
    /// Removes all data from database.
    /// </summary>
    [RelayCommand]
    async Task RemoveAllData()
    {
        bool flag = await Shell.Current.DisplayAlertAsync("", "Are you sure you want to delete ALL data?", "Yes", "No");
        if (!flag)
            return;

        try
        {
            await momoney.ResetDb();
            _ = Shell.Current.DisplayAlertAsync("Success", "All data has been deleted.", "OK");
            logger.LogFirebaseEvent(FirebaseParameters.EVENT_REMOVE_ALL_DATA, FirebaseParameters.GetFirebaseParameters());
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(RemoveAllData), ex);
            await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
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
            await Utilities.DisplayToast("Balances have been recalculated.");
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(CalculateAccountBalances), ex);
            await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
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

    /// <summary>
    /// Toggle TransactionDictationEnabled in Preferences (shows/hides AI feature)
    /// </summary>
    /// <param name="value"></param>
    partial void OnTransactionDictationEnabledChanged(bool value)
    {
        Utilities.TransactionDictationEnabled = value;
        WeakReferenceMessenger.Default.Send(new UpdateTransactionDictationMessage(value));
    }
}