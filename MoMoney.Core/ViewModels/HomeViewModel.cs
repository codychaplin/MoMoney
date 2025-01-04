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

    [ObservableProperty] decimal networth = 0;
    [ObservableProperty] ObservableCollection<AccountTotalModel> accountTotals = [];

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
            await GetAccountBalances(accounts);

            var transactions = await transactionService.GetTransactionsFromTo(From, To, true);
            if (!transactions.Any())
            {
                RecentTransactions.Clear();
                return;
            }

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
        RecentTransactions.Clear();
        foreach (Transaction transaction in transactions)
            RecentTransactions.Add(transaction);
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
            
            AccountTotals.Clear();
            foreach (AccountTotalModel account in groupedAccounts.Where(acc => acc != null))
                AccountTotals.Add(account);
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
    void GetChartData(IEnumerable<Transaction> transactions)
    {
        // if the date range is > 1 year, group results by Month, if < 1 year, group by day
        // get non-transfer transactions, group by date, and select date and sum of amounts on each date
        bool isLong = (To - From).TotalDays > 365;
        decimal runningTotal = Networth;

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