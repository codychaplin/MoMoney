using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels.Tabs;

public partial class HomeViewModel : ObservableObject
{
    readonly IAccountService accountService;
    readonly ICategoryService categoryService;
    readonly ITransactionService transactionService;
    readonly ILoggerService<HomeViewModel> logger;

    [ObservableProperty] bool isBusy = false;

    [ObservableProperty] ObservableCollection<Transaction> recentTransactions = [];

    [ObservableProperty] decimal networthAtEndDate = 0;
    [ObservableProperty] ObservableCollection<AccountTotalModel> accountTotals = [];

    [ObservableProperty] static DateTime startDate = new();
    [ObservableProperty] static DateTime endDate = new();

    [ObservableProperty] ObservableCollection<BalanceOverTimeData> data = [];

    [ObservableProperty] string showValue = "$0,k";
    [ObservableProperty] DateRange selectedRange = DateRange.Y1;
    [ObservableProperty] string xLabelFormat = "MMM";
    [ObservableProperty] double xInterval = 1;
    [ObservableProperty] string xIntervalType = "Months";

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

        StartDate = DateTime.Today.AddYears(-1);
        EndDate = DateTime.Today;

        logger.LogFirebaseEvent(FirebaseParameters.EVENT_OPEN_APP, FirebaseParameters.GetFirebaseParameters());

        WeakReferenceMessenger.Default.Register<UpdateHomePageMessage>(this, async (r, m) => await Refresh());
    }

    /// <summary>
    /// Refreshes the page with updated data.
    /// </summary>
    /// <returns></returns>
    public async Task Refresh(bool showLoading = false)
    {
        try
        {
            if (showLoading)
                IsBusy = true;
                
            ShowValue = Utilities.ShowValue ? "$0,k" : "$?";

            var transactions = await transactionService.GetTransactionsFromTo(StartDate, EndDate);
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
            await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
        }
        finally
        {
            if (showLoading)
                IsBusy = false;
        }
    }

    /// <summary>
    /// Calculates networth at the end date.
    /// </summary>
    /// <returns></returns>
    async Task CalculateNetworthAndAccountBalances()
    {
        // calculate current networth
        var accounts = await accountService.GetActiveAccounts();
        currentNetworth = accounts.Sum(acc => acc.CurrentBalance);

        // calculate account balances
        var groupedAccounts = accounts
            .GroupBy(acc => acc.AccountType)
            .Select(group => new AccountTotalModel
            {
                AccountType = group.Key!.ToString(),
                Total = Utilities.ShowValue ? group.Sum(acc => acc.CurrentBalance) : 0
            })
            .Where(acc => acc != null).ToList();

        if (Utilities.ShowValue == false)
        {
            NetworthAtEndDate = 0;
        }
        else if (EndDate >= latestTransactionDate)
        {
            // if the end date is >= the date of the latest transaction, networth is current networth
            NetworthAtEndDate = currentNetworth;
        }
        else
        {
            // if the end date is before the latest transaction date, get transactions between the two dates and calculate the difference
            var transactions = await transactionService.GetTransactionsFromTo(EndDate, latestTransactionDate);
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
        // group by year if range > 1Y, by month if range > 1M, otherwise by day
        // get non-transfer transactions, group by date, and select date and sum of amounts on each date
        double totalDays = (EndDate - StartDate).TotalDays;
        decimal runningTotal = NetworthAtEndDate;

        IEnumerable<BalanceOverTimeData> data;
        if (totalDays > 365)
        {
            XLabelFormat = "yyyy";
            XIntervalType = "Years";
            data = transactions
                .Where(trans => trans.CategoryID != Constants.TRANSFER_ID)
                .GroupBy(trans => trans.Date.Year)
                .Select(group =>
                {
                    var balanceData = new BalanceOverTimeData
                    {
                        Date = new DateTime(group.Key, 1, 1),
                        Balance = runningTotal
                    };
                    runningTotal -= group.Sum(t => t.Amount);
                    return balanceData;
                });
        }
        else if (totalDays > 93)
        {
            XLabelFormat = "MMM-yy";
            XIntervalType = "Months";
            data = transactions
                .Where(trans => trans.CategoryID != Constants.TRANSFER_ID)
                .GroupBy(trans => new { trans.Date.Year, trans.Date.Month })
                .Select(group =>
                {
                    var balanceData = new BalanceOverTimeData
                    {
                        Date = new DateTime(group.Key.Year, group.Key.Month, 1),
                        Balance = runningTotal
                    };
                    runningTotal -= group.Sum(t => t.Amount);
                    return balanceData;
                });
        }
        else
        {
            XLabelFormat = "dd-MMM";
            XIntervalType = "Days";
            data = transactions
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
        }

        Data.Clear();
        foreach (var d in data)
            Data.Add(d);
    }

    partial void OnSelectedRangeChanged(DateRange value)
    {
        var today = DateTime.Today;
        StartDate = value switch
        {
            DateRange.D1  => today.AddDays(-1),
            DateRange.W1  => today.AddDays(-7),
            DateRange.M1  => today.AddMonths(-1),
            DateRange.M3  => today.AddMonths(-3),
            DateRange.M6  => today.AddMonths(-6),
            DateRange.YTD => new DateTime(today.Year, 1, 1),
            DateRange.Y1  => today.AddYears(-1),
            DateRange.ALL => DateTime.MinValue,
            _             => StartDate
        };
        EndDate = today;
        _ = Refresh(true);
    }

    [RelayCommand]
    public void ViewAllStats() => WeakReferenceMessenger.Default.Send(new ChangeTabMessage(3));

    [RelayCommand]
    public void ViewAllTransactions() => WeakReferenceMessenger.Default.Send(new ChangeTabMessage(1));
}

public enum DateRange
{
    D1,
    W1,
    M1,
    M3,
    M6,
    YTD,
    Y1,
    ALL
}