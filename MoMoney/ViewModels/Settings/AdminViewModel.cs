using SQLite;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Services;
using MoMoney.Views.Settings;

namespace MoMoney.ViewModels.Settings;

public partial class AdminViewModel : ObservableObject
{
    readonly IStockService stockService;
    readonly IAccountService accountService;
    readonly ICategoryService categoryService;
    readonly ITransactionService transactionService;
    readonly ILoggerService<AdminViewModel> logService;

    public AdminViewModel(ITransactionService _transactionService, IAccountService _accountService,
        ICategoryService _categoryService, IStockService _stockService, ILoggerService<AdminViewModel> _logService)
    {
        transactionService = _transactionService;
        accountService = _accountService;
        categoryService = _categoryService;
        stockService = _stockService;
        logService = _logService;
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
            await transactionService.RemoveAllTransactions();
        }
        catch (SQLiteException ex)
        {
            await Shell.Current.DisplayAlert("Database Error", ex.Message, "OK");
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
            await accountService.RemoveAllAccounts();
        }
        catch (SQLiteException ex)
        {
            await Shell.Current.DisplayAlert("Database Error", ex.Message, "OK");
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
            await categoryService.RemoveAllCategories();
        }
        catch (SQLiteException ex)
        {
            await Shell.Current.DisplayAlert("Database Error", ex.Message, "OK");
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
            await stockService.RemoveStocks();
        }
        catch (SQLiteException ex)
        {
            await Shell.Current.DisplayAlert("Database Error", ex.Message, "OK");
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
            await logService.RemoveLogs();
        }
        catch (SQLiteException ex)
        {
            await Shell.Current.DisplayAlert("Database Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Goes to LoggingPage.xaml.
    /// </summary>
    [RelayCommand]
    async Task GoToLogging()
    {
        await Shell.Current.GoToAsync(nameof(LoggingPage));
    }

    /*/// <summary>
    /// Goes to BulkEditingPage.xaml.
    /// </summary>
    [RelayCommand]
    async Task GoToBulkEditing()
    {
        await Shell.Current.GoToAsync(nameof(BulkEditingPage));
    }*/
}