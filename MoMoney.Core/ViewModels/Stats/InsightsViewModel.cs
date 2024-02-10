using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Exceptions;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels.Stats;

public partial class InsightsViewModel : ObservableObject
{
    readonly ICategoryService categoryService;
    readonly ITransactionService transactionService;
    readonly ILoggerService<InsightsViewModel> logger;

    [ObservableProperty]
    public int selectedYear;

    [ObservableProperty]
    public ObservableCollection<int> years = new();

    [ObservableProperty]
    public ObservableCollection<IncomeExpenseData> incomeData = new();

    [ObservableProperty]
    public ObservableCollection<IncomeExpenseData> expenseData = new();

    [ObservableProperty]
    public decimal totalIncome = 0;

    [ObservableProperty]
    public decimal totalExpense = 0;

    [ObservableProperty]
    public string topIncomeSubcategoryName = "";
    [ObservableProperty]
    public decimal topIncomeSubcategoryAmount = 0;

    [ObservableProperty]
    public string topExpenseCategoryName = "";
    [ObservableProperty]
    public decimal topExpenseCategoryAmount = 0;

    public InsightsViewModel(ITransactionService _transactionService, ICategoryService _categoryService, ILoggerService<InsightsViewModel> _logger)
    {
        transactionService = _transactionService;
        categoryService = _categoryService;
        logger = _logger;
        logger.LogFirebaseEvent(FirebaseParameters.EVENT_VIEW_INSIGHTS, FirebaseParameters.GetFirebaseParameters());
    }

    /// <summary>
    /// Initializes year range
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public async void Init(object sender, EventArgs e)
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

    public async void Refresh(object sender, EventArgs e)
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
        await GetTopCategories(transactions);
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
                var amount = group.income.Sum(t => t.Amount);
                return new IncomeExpenseData
                {
                    Month = group.Month,
                    Amount = amount > 0 ? amount : 0
                };
            }));
        ExpenseData = new ObservableCollection<IncomeExpenseData>(
            groupByMonth.Select(group =>
            {
                var amount = group.expenses.Sum(t => t.Amount);
                return new IncomeExpenseData
                {
                    Month = group.Month,
                    Amount = amount < 0 ? amount : 0
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
    /// Updates top income/expense subcategory/category.
    /// </summary>
    /// <param name="transactions"></param>
    async Task GetTopCategories(IEnumerable<Transaction> transactions)
    {
        try
        {
            // get income transactions, group by subcategory,
            // select sum of Amounts and subcategory ID, then get max value
            var incomeResults = transactions.Where(t => t.CategoryID == Constants.INCOME_ID)
                                            .GroupBy(t => t.SubcategoryID)
                                            .Select(group => new
                                            {
                                                Total = group.Sum(t => t.Amount),
                                                group.FirstOrDefault().SubcategoryID
                                            })
                                            .MaxBy(g => g.Total);

            if (incomeResults is null)
            {
                TopIncomeSubcategoryName = "N/A";
                TopIncomeSubcategoryAmount = 0;
                return;
            }
                
            // get category name and amount
            Category incomeCategory = await categoryService.GetCategory(incomeResults.SubcategoryID);
            TopIncomeSubcategoryName = incomeCategory.CategoryName;
            TopIncomeSubcategoryAmount = incomeResults.Total;

            // get expense transactions, group by category,
            // select sum of Amounts and category ID, then get min value (expenses are negative so Min is used)
            var expenseResults = transactions.Where(t => t.CategoryID >= Constants.EXPENSE_ID)
                                             .GroupBy(t => t.CategoryID)
                                             .Select(group => new
                                             {
                                                 Total = group.Sum(t => t.Amount),
                                                 group.FirstOrDefault().CategoryID
                                             })
                                             .MinBy(g => g.Total);

            if (expenseResults is null)
            {
                TopExpenseCategoryName = "N/A";
                TopExpenseCategoryAmount = 0;
                return;
            }

            // get category name and amount
            Category expenseCategory = await categoryService.GetCategory(expenseResults.CategoryID);
            TopExpenseCategoryName = expenseCategory.CategoryName;
            TopExpenseCategoryAmount = Math.Abs(expenseResults.Total);
        }
        catch (CategoryNotFoundException ex)
        {
            await logger.LogError(nameof(GetTopCategories), ex);
            await Shell.Current.DisplayAlert("Category Not Found Error", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(GetTopCategories), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
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