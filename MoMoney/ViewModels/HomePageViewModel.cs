using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Models;
using MoMoney.Services;

namespace MoMoney.ViewModels;

public partial class HomePageViewModel : BaseViewModel
{
    [ObservableProperty]
    public ObservableCollection<Transaction> recentTransactions = new();

    [ObservableProperty]
    public decimal networth = 0;
    bool isNetworthSet = false; // used to update networth on start

    [ObservableProperty]
    public decimal totalIncome = 0;

    [ObservableProperty]
    public decimal totalExpenses = 0;

    [ObservableProperty]
    public string topIncomeSubcategory = "N/A";

    [ObservableProperty]
    public string topExpenseCategory = "N/A";

    [ObservableProperty]
    public ObservableCollection<BalanceOverTimeData> data = new();

    public async Task Refresh()
    {
        var transactions = await TransactionService.GetTransactionsFromTo(From, To, true);
        if (!transactions.Any())
            return;

        // networth is needed for GetChartData, hence the .ContinueWith
        Task recentTransactions = GetRecentTransactions(transactions);
        Task getNetworth = GetNetworth();
        Task getStats = GetStats(transactions);
        Task getChartData = getNetworth.ContinueWith(_ =>
        {
            return GetChartData(transactions);
        }).Unwrap();

        await Task.WhenAll(getNetworth, recentTransactions, getStats, getChartData);
    }

    /// <summary>
    /// Gets updated total balance of all accounts combined.
    /// </summary>
    async Task GetNetworth()
    {
        if (isNetworthSet)
            return;

        Networth = 0;
        var accounts = await AccountService.GetActiveAccounts();
        foreach (var acc in accounts)
            Networth += acc.CurrentBalance;
        isNetworthSet = true;
    }

    /// <summary>
    /// Gets updated transactions from database and refreshes Transactions collection.
    /// </summary>
    async Task GetRecentTransactions(IEnumerable<Transaction> transactions)
    {
        transactions = transactions.Take(5);
        RecentTransactions.Clear();
        foreach (var trans in transactions)
            RecentTransactions.Add(trans);

        await Task.CompletedTask;
    }

    async Task GetStats(IEnumerable<Transaction> transactions)
    {
        // update income/expense totals
        TotalIncome = transactions.Where(t => t.CategoryID == Constants.INCOME_ID).Sum(t => t.Amount);
        TotalExpenses = transactions.Where(t => t.CategoryID >= Constants.EXPENSE_ID).Sum(t => t.Amount);

        // update top income subcategory
        var subcategoryID = transactions.Where(t => t.CategoryID == Constants.INCOME_ID)
                                   .GroupBy(t => t.SubcategoryID)
                                   .Select(group => new
                                   {
                                       Total = group.Sum(t => t.Amount),
                                       group.FirstOrDefault().SubcategoryID
                                   })
                                   .MaxBy(g => g.Total)
                                   .SubcategoryID;
        Category subcategory = await CategoryService.GetCategory(subcategoryID);
        TopIncomeSubcategory = subcategory.CategoryName;

        // update top expense category
        var categoryID = transactions.Where(t => t.CategoryID >= Constants.EXPENSE_ID)
                                .GroupBy(t => t.CategoryID)
                                .Select(group => new
                                {
                                    Total = group.Sum(t => t.Amount),
                                    group.FirstOrDefault().CategoryID
                                })
                                .MinBy(g => g.Total)
                                .CategoryID;
        Category category = await CategoryService.GetCategory(categoryID);
        TopExpenseCategory = category.CategoryName;
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

public class BalanceOverTimeData
{
    public DateTime Date { get; set; }
    public decimal Balance { get; set; }
}
