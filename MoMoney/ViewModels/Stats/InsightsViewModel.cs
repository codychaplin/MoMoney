using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Models;
using MoMoney.Services;

namespace MoMoney.ViewModels.Stats;

public partial class InsightsViewModel : ObservableObject
{
    [ObservableProperty]
    public decimal totalIncome = 0;

    [ObservableProperty]
    public decimal totalExpenses = 0;

    [ObservableProperty]
    public string topIncomeSubcategory = "";

    [ObservableProperty]
    public string topExpenseCategory = "";

    [ObservableProperty]
    public ObservableCollection<int> years = new();

    [ObservableProperty]
    public int selectedYear;

    /// <summary>
    /// Initializes year range
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public async void Init(object sender, EventArgs e)
    {
        // gets earliest transaction in db, adds each year from then until today
        var first = await TransactionService.GetFirstTransaction();
        var start = first.Date;
        var end = DateTime.Today;
        while (start <= end)
        {
            Years.Add(start.Year);
            start = start.AddYears(1);
        }
        SelectedYear = end.Year;
    }

    public async void Refresh(object sender, EventArgs e)
    {
        // gets transactions from selected year
        var from = new DateTime(SelectedYear, 1, 1);
        var to = new DateTime(SelectedYear, 12, 31);
        var transactions = await TransactionService.GetTransactionsFromTo(from, to, false);
        if (!transactions.Any())
            return;

        Task getTotals = GetTotals(transactions);
        Task getTopCategories = GetTopCategories(transactions);

        await Task.WhenAll(getTotals, getTopCategories);
    }

    /// <summary>
    /// Gets income/expense totals for the year.
    /// </summary>
    /// <param name="transactions"></param>
    async Task GetTotals(IEnumerable<Transaction> transactions)
    {
        await Task.Run(() =>
        {
            TotalIncome = transactions.Where(t => t.CategoryID == Constants.INCOME_ID).Sum(t => t.Amount);
            TotalExpenses = Math.Abs(transactions.Where(t => t.CategoryID >= Constants.EXPENSE_ID).Sum(t => t.Amount));
        });
    }

    /// <summary>
    /// Updates top income/expense subcategory/category.
    /// </summary>
    /// <param name="transactions"></param>
    async Task GetTopCategories(IEnumerable<Transaction> transactions)
    {
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
}
