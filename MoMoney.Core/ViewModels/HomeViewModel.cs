using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Exceptions;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    readonly IAccountService accountService;
    readonly ICategoryService categoryService;
    readonly ITransactionService transactionService;
    readonly ILoggerService<HomeViewModel> logger;

    [ObservableProperty]
    public ObservableCollection<Transaction> recentTransactions = new();

    [ObservableProperty]
    public decimal networth = 0;

    [ObservableProperty]
    public decimal totalIncome = 0;

    [ObservableProperty]
    public decimal totalExpenses = 0;

    [ObservableProperty]
    public string topIncomeSubcategory = "N/A";

    [ObservableProperty]
    public string topExpenseCategory = "N/A";

    [ObservableProperty]
    public static DateTime from = new();

    [ObservableProperty]
    public static DateTime to = new();

    [ObservableProperty]
    public ObservableCollection<BalanceOverTimeData> data = new();

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
            await GetNetworth();

            var transactions = await transactionService.GetTransactionsFromTo(From, To, true);
            if (!transactions.Any())
            {
                RecentTransactions?.Clear();
                return;
            }

            GetRecentTransactions(transactions);
            Task getStats = GetStats(transactions);
            Task getChartData = GetChartData(transactions);

            await Task.WhenAll(getStats, getChartData);
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
    async Task GetNetworth()
    {
        if (Utilities.ShowValue == false)
        {
            Networth = 0;
            return;
        }

        decimal total = 0;
        var accounts = await accountService.GetActiveAccounts();
        if (accounts.Any())
            foreach (var acc in accounts)
                total += acc.CurrentBalance;
        Networth = total;
    }

    /// <summary>
    /// Gets updated transactions from database and refreshes Transactions collection.
    /// </summary>
    void GetRecentTransactions(IEnumerable<Transaction> transactions)
    {
        transactions = transactions.Take(5);
        RecentTransactions.Clear();
        foreach (var trans in transactions)
            RecentTransactions.Add(trans);
    }

    async Task GetStats(IEnumerable<Transaction> transactions)
    {
        try
        {
            // update income/expense totals
            if (Utilities.ShowValue)
            {
                TotalIncome = transactions.Where(t => t.CategoryID == Constants.INCOME_ID).Sum(t => t.Amount);
                TotalExpenses = transactions.Where(t => t.CategoryID >= Constants.EXPENSE_ID).Sum(t => t.Amount);
            }
            else
            {
                TotalIncome = 0;
                TotalExpenses = 0;
            }

            // update top income subcategory
            var incomeTransactions = transactions.Where(t => t.CategoryID == Constants.INCOME_ID);
            if (incomeTransactions.Any())
            {
                var subcategoryID = incomeTransactions.GroupBy(t => t.SubcategoryID)
                                                      .Select(group => new
                                                      {
                                                          Total = group.Sum(t => t.Amount),
                                                          group.FirstOrDefault().SubcategoryID
                                                      })
                                                      .MaxBy(g => g.Total)
                                                      .SubcategoryID;

                Category subcategory = await categoryService.GetCategory(subcategoryID);
                TopIncomeSubcategory = subcategory.CategoryName;
            }

            // update top expense category
            var expenseTransactions = transactions.Where(t => t.CategoryID >= Constants.EXPENSE_ID);
            if (expenseTransactions.Any())
            {

                var categoryID = expenseTransactions.GroupBy(t => t.CategoryID)
                                                    .Select(group => new
                                                    {
                                                        Total = group.Sum(t => t.Amount),
                                                        group.FirstOrDefault().CategoryID
                                                    })
                                                    .MinBy(g => g.Total)
                                                    .CategoryID;
                Category category = await categoryService.GetCategory(categoryID);
                TopExpenseCategory = category.CategoryName;
            }
        }
        catch (CategoryNotFoundException ex)
        {
            await logger.LogError(nameof(GetStats), ex);
            await Shell.Current.DisplayAlert("Category Not Found Error", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(GetStats), ex);
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
}