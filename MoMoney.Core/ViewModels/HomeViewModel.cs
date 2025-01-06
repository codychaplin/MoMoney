using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    readonly IAccountService accountService;
    readonly ICategoryService categoryService;
    readonly ITransactionService transactionService;
    readonly ILoggerService<HomeViewModel> logger;

    [ObservableProperty] ObservableCollection<Transaction> recentTransactions = [];

    [ObservableProperty] decimal networthAtEndDate = 0;
    [ObservableProperty] ObservableCollection<AccountTotalModel> accountTotals = [];

    [ObservableProperty] static DateTime startDate = new();
    [ObservableProperty] static DateTime endDate = new();

    [ObservableProperty] ObservableCollection<BalanceOverTimeData> data = [];

    [ObservableProperty] string showValue = "$0,k";

    bool firstLoad = true;
    DateTime latestTransactionDate = DateTime.Today;
    decimal currentNetworth = 0;

    public HomeViewModel(ITransactionService _transactionService, IAccountService _accountService,
        ICategoryService _categoryService, ILoggerService<HomeViewModel> _logger)
    {
        transactionService = _transactionService;
        accountService = _accountService;
        categoryService = _categoryService;
        logger = _logger;

        // first two months, show 1 year, starting March show YTD
        StartDate = (DateTime.Today.Month <= 2) ? DateTime.Today.AddYears(-1) : new(DateTime.Today.Year, 1, 1);
        EndDate = DateTime.Today;

        logger.LogFirebaseEvent(FirebaseParameters.EVENT_OPEN_APP, FirebaseParameters.GetFirebaseParameters());
    }

    /// <summary>
    /// Refreshes the page with updated data.
    /// </summary>
    /// <returns></returns>
    public async Task Refresh()
    {
        try
        {
            ShowValue = Utilities.ShowValue ? "$0,k" : "$?";

            var transactions = await transactionService.GetTransactionsFromTo(StartDate, EndDate, true);
            if (transactions.Count == 0)
            {
                RecentTransactions.Clear();
                return;
            }

            if (firstLoad)
            {
                firstLoad = false;
                latestTransactionDate = transactions[0].Date;
            }

            await CalculateNetworthAndAccountBalances();

            GetRecentTransactions(transactions);
            GetChartData(transactions);
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(Refresh), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Calculates networth at the end date.
    /// </summary>
    /// <returns></returns>
    async Task CalculateNetworthAndAccountBalances()
    {
        // calculate current networth
        if (Utilities.ShowValue == false)
        {
            currentNetworth = 0;
            return;
        }

        var accounts = await accountService.GetActiveAccounts();
        currentNetworth = accounts.Sum(acc => acc.CurrentBalance);

        // calculate account balances
        var groupedAccounts = accounts
            .GroupBy(acc => acc.AccountType)
            .Select(group => new AccountTotalModel
            {
                AccountType = group.Key.ToString(),
                Total = Utilities.ShowValue ? group.Sum(acc => acc.CurrentBalance) : 0
            })
            .Where(acc => acc != null).ToList();

        if (EndDate >= latestTransactionDate)
        {
            // if the end date is >= the date of the latest transaction, networth is current networth
            NetworthAtEndDate = currentNetworth;
        }
        else
        {
            // if the end date is before the latest transaction date, get transactions between the two dates and calculate the difference
            var transactions = await transactionService.GetTransactionsFromTo(EndDate, latestTransactionDate, true);
            NetworthAtEndDate = currentNetworth - transactions.Where(trans => trans.CategoryID != Constants.TRANSFER_ID).Sum(t => t.Amount);
            foreach (var account in groupedAccounts)
            {
                var transactionsForGroup = transactions.Where(t => accountService.Accounts[t.AccountID].AccountType == account.AccountType);
                account.Total -= transactionsForGroup.Sum(t => t.Amount);
            }
        }

        AccountTotals.Clear();
        foreach (AccountTotalModel account in groupedAccounts)
            AccountTotals.Add(account);
    }

    /// <summary>
    /// Gets updated transactions from database and refreshes Transactions collection.
    /// </summary>
    void GetRecentTransactions(IEnumerable<Transaction> transactions)
    {
        transactions = transactions.Take(5);
        RecentTransactions.Clear();
        foreach (Transaction transaction in transactions)
            RecentTransactions.Add(transaction);
    }

    /// <summary>
    /// Gets data for running balance chart.
    /// </summary>
    void GetChartData(IEnumerable<Transaction> transactions)
    {
        // if the date range is > 1 year, group results by Month, if < 1 year, group by day
        // get non-transfer transactions, group by date, and select date and sum of amounts on each date
        bool isLong = (EndDate - StartDate).TotalDays > 365;
        decimal runningTotal = NetworthAtEndDate;

        if (isLong)
        {
            var data = transactions
                .Where(trans => trans.CategoryID != Constants.TRANSFER_ID)
                .GroupBy(trans => trans.Date.Month)
                .Select(group =>
                {
                    var balanceData = new BalanceOverTimeData
                    {
                        Date = group.First().Date,
                        Balance = runningTotal
                    };
                    runningTotal -= group.Sum(t => t.Amount);
                    return balanceData;
                });
            Data.Clear();
            foreach (var d in data)
                Data.Add(d);
        }
        else
        {
            var data = transactions
                .Where(trans => trans.CategoryID != Constants.TRANSFER_ID)
                .GroupBy(trans => trans.Date)
                .Select(group =>
                {
                    var balanceData = new BalanceOverTimeData
                    {
                        Date = group.Key,
                        Balance = runningTotal
                    };
                    runningTotal -= group.Sum(t => t.Amount);
                    return balanceData;
                });
            Data.Clear();
            foreach (var d in data)
                Data.Add(d);
        }
    }

    [RelayCommand]
    public void ViewAllStats() => WeakReferenceMessenger.Default.Send(new ChangeTabMessage(3));

    [RelayCommand]
    public void ViewAllTransactions() => WeakReferenceMessenger.Default.Send(new ChangeTabMessage(1));
}