using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.ComponentModel;
using MvvmHelpers;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels;

public partial class HomeViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    readonly IAccountService accountService;
    readonly ICategoryService categoryService;
    readonly ITransactionService transactionService;
    readonly ILoggerService<HomeViewModel> logger;

    [ObservableProperty] ObservableRangeCollection<Transaction> recentTransactions = [];

    [ObservableProperty] decimal networth = 0;
    [ObservableProperty] ObservableRangeCollection<AccountTotalModel> accountTotals = [];

    [ObservableProperty] static DateTime from = new();
    [ObservableProperty] static DateTime to = new();

    [ObservableProperty] ObservableCollection<BalanceOverTimeData> data = [];

    [ObservableProperty] string showValue = "$0,k";

    public HomeViewModel(ITransactionService _transactionService, IAccountService _accountService,
        ICategoryService _categoryService, ILoggerService<HomeViewModel> _logger)
    {
        transactionService = _transactionService;
        accountService = _accountService;
        categoryService = _categoryService;
        logger = _logger;

        // first two months, show 1 year, starting March show YTD
        From = (DateTime.Today.Month <= 2) ? DateTime.Today.AddYears(-1) : new(DateTime.Today.Year, 1, 1);
        To = DateTime.Today;

        logger.LogFirebaseEvent(FirebaseParameters.EVENT_OPEN_APP, FirebaseParameters.GetFirebaseParameters());
    }

    public async Task Refresh()
    {
        try
        {
            ShowValue = Utilities.ShowValue ? "$0,k" : "$?";
            var accounts = await accountService.GetActiveAccounts();
            GetNetworth(accounts);
            Task getAccountBalances = GetAccountBalances(accounts);

            var transactions = await transactionService.GetTransactionsFromTo(From, To, true);
            if (!transactions.Any())
            {
                RecentTransactions.Clear();
                return;
            }

            GetRecentTransactions(transactions);
            Task getChartData = GetChartData(transactions);

            await Task.WhenAll(getAccountBalances, getChartData);
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(Refresh), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Gets updated total balance of all accounts combined.
    /// </summary>
    void GetNetworth(IEnumerable<Account> accounts)
    {
        if (Utilities.ShowValue == false)
        {
            Networth = 0;
            return;
        }

        Networth = accounts.Sum(acc => acc.CurrentBalance);
    }

    /// <summary>
    /// Gets updated transactions from database and refreshes Transactions collection.
    /// </summary>
    void GetRecentTransactions(IEnumerable<Transaction> transactions)
    {
        transactions = transactions.Take(5);
        RecentTransactions.ReplaceRange(transactions);
    }

    async Task GetAccountBalances(IEnumerable<Account> accounts)
    {
        try
        {
            var groupedAccounts = accounts.GroupBy(acc => acc.AccountType)
                                          .Select(group => new AccountTotalModel
                                          {
                                              AccountType = group.Key.ToString(),
                                              Total = Utilities.ShowValue ? group.Sum(acc => acc.CurrentBalance) : 0
                                          });
            
            AccountTotals.ReplaceRange(groupedAccounts.Where(acc => acc != null));
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(GetAccountBalances), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Gets data for running balance chart.
    /// </summary>
    async Task GetChartData(IEnumerable<Transaction> transactions)
    {
        // if the date range is > 1 year, group results by Month, if < 1 year, group by day
        // get non-transfer transactions, group by date, and select date and sum of amounts on each date
        bool isLong = (To - From).TotalDays > 365;
        decimal runningTotal = Networth;
        if (isLong)
        {
            Data = new ObservableCollection<BalanceOverTimeData>(
                await Task.Run(() =>
                    transactions.OrderByDescending(trans => trans.Date)
                       .Where(trans => trans.CategoryID != Constants.TRANSFER_ID)
                       .GroupBy(trans => trans.Date.Month)
                       .Select(group => new BalanceOverTimeData
                       {
                           Date = group.FirstOrDefault().Date,
                           Balance = runningTotal -= group.Sum(t => t.Amount)
                       })));
        }
        else
        {
            Data = new ObservableCollection<BalanceOverTimeData>(
                await Task.Run(() =>
                    transactions.OrderByDescending(trans => trans.Date)
                       .Where(trans => trans.CategoryID != Constants.TRANSFER_ID)
                       .GroupBy(trans => trans.Date)
                       .Select(group => new BalanceOverTimeData
                       {
                           Date = group.FirstOrDefault().Date,
                           Balance = runningTotal -= group.Sum(t => t.Amount)
                       })));
        }
    }

    [RelayCommand]
    public void ViewAllStats() => WeakReferenceMessenger.Default.Send(new ChangeTabMessage(3));

    [RelayCommand]
    public void ViewAllTransactions() => WeakReferenceMessenger.Default.Send(new ChangeTabMessage(1));
}