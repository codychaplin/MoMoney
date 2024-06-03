using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels.Stats;

public partial class InsightsViewModel : ObservableObject
{
    readonly ITransactionService transactionService;
    readonly ILoggerService<InsightsViewModel> logger;

    [ObservableProperty] int selectedYear;

    [ObservableProperty] ObservableCollection<int> years = [];

    [ObservableProperty] ObservableCollection<IncomeExpenseData> incomeData = [];
    [ObservableProperty] ObservableCollection<IncomeExpenseData> expenseData = [];

    [ObservableProperty] decimal totalIncome = 0;
    [ObservableProperty] decimal totalExpense = 0;

    public InsightsViewModel(ITransactionService _transactionService, ILoggerService<InsightsViewModel> _logger)
    {
        transactionService = _transactionService;
        logger = _logger;
        logger.LogFirebaseEvent(FirebaseParameters.EVENT_VIEW_INSIGHTS, FirebaseParameters.GetFirebaseParameters());
    }

    /// <summary>
    /// Initializes year range
    /// </summary>
    public async Task Init()
    {
        var first = await transactionService.GetFirstTransaction();
        if (first is null)
            return;

        // get date of first transaction, today's date, and add each year to collection
        var start = first.Date;
        var end = DateTime.Today;
        while (end >= start)
        {
            Years.Add(end.Year);
            end = end.AddYears(-1);
        }
        SelectedYear = DateTime.Today.Year;
    }

    [RelayCommand]
    async Task Refresh()
    {
        await Task.Delay(50);
        // gets transactions from selected year
        var from = new DateTime(SelectedYear, 1, 1);
        var to = new DateTime(SelectedYear, 12, 31);
        var transactions = await transactionService.GetTransactionsFromTo(from, to, false);
        if (!transactions.Any())
            return;

        GetTotals(transactions);
        GetIncomeExpenseData(transactions);
    }

    /// <summary>
    /// Populates Income/Expense Chart.
    /// </summary>
    /// <param name="transactions"></param>
    void GetIncomeExpenseData(IEnumerable<Transaction> transactions)
    {
        // group transactions (minus transfers) by month, select Month, then split incomes and expenses
        var groupByMonth = transactions.Where(t => t.CategoryID != Constants.TRANSFER_ID)
                                        .GroupBy(t => t.Date.Month)
                                        .Select(group => new
                                        {
                                            Month = Constants.MONTHS[group.Key - 1],
                                            income = group.Where(t => t.CategoryID == Constants.INCOME_ID && t.Amount > 0),
                                            expenses = group.Where(t => t.CategoryID >= Constants.EXPENSE_ID && t.Amount < 0)
                                        });

        if (!groupByMonth.Any())
            return;

        // split grouped into income/expenses and select as IncomeExpenseData object
        IncomeData = new ObservableCollection<IncomeExpenseData>(
            groupByMonth.Select(group => 
            {
                decimal amount = group.income.Sum(t => t.Amount);
                return new IncomeExpenseData
                {
                    Month = group.Month,
                    Amount = amount > 0 ? amount : 0
                };
            }));
        ExpenseData = new ObservableCollection<IncomeExpenseData>(
            groupByMonth.Select(group =>
            {
                decimal amount = Math.Abs(group.expenses.Sum(t => t.Amount));
                return new IncomeExpenseData
                {
                    Month = group.Month,
                    Amount = amount
                };
            }));

        // if year isn't complete add default data
        if (IncomeData.Count < 12)
        {
            for (int i = IncomeData.Count; i < 12; i++)
                IncomeData.Add(new IncomeExpenseData
                {
                    Month = Constants.MONTHS[i],
                    Amount = 0
                });
        }
    }

    /// <summary>
    /// Gets income/expense totals for the year.
    /// </summary>
    /// <param name="transactions"></param>
    /// <returns></returns>
    void GetTotals(IEnumerable<Transaction> transactions)
    {
        if (Utilities.ShowValue)
        {
            TotalIncome = transactions.Where(t => t.CategoryID == Constants.INCOME_ID).Sum(t => t.Amount);
            TotalExpense = Math.Abs(transactions.Where(t => t.CategoryID >= Constants.EXPENSE_ID).Sum(t => t.Amount));
            return;
        }

        TotalIncome = 0;
        TotalExpense = 0;
    }
}