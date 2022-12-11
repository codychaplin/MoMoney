using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Models;
using MoMoney.Services;

namespace MoMoney.ViewModels
{
    public partial class HomePageViewModel : ObservableObject
    {
        [ObservableProperty]
        public ObservableCollection<Transaction> recentTransactions = new();

        [ObservableProperty]
        public decimal netWorth = 0;

        [ObservableProperty]
        public decimal totalIncome = 0;

        [ObservableProperty]
        public decimal totalExpenses = 0;

        [ObservableProperty]
        public string topIncomeSubcategory;

        [ObservableProperty]
        public string topExpenseCategory;

        [ObservableProperty]
        public ObservableCollection<Data> data = new();

        [ObservableProperty]
        public DateTime from = new(DateTime.Today.Year, 1, 1);

        [ObservableProperty]
        public DateTime to = DateTime.Today;

        /// <summary>
        /// Gets updated transactions from database and refreshes Transactions collection.
        /// </summary>
        public async Task GetRecentTransactions()
        {
            RecentTransactions.Clear();
            var transactions = await TransactionService.GetRecentTransactions();
            foreach (var trans in transactions)
                RecentTransactions.Add(trans);
        }

        /// <summary>
        /// Gets updated total balance of all accounts combined.
        /// </summary>
        public async Task GetTotalBalance()
        {
            var accounts = await AccountService.GetActiveAccounts();
            foreach (var acc in accounts)
                NetWorth += acc.CurrentBalance;
        }

        /// <summary>
        /// Gets data for running balance chart.
        /// </summary>
        public async Task GetChartData()
        {
            decimal runningTotal = NetWorth; // running total starts at current net worth
            var results = await TransactionService.GetTransactionsFromTo(From, To);

            // update income/expense totals
            TotalIncome = results.Where(t => t.CategoryID == Constants.INCOME_ID).Sum(t => t.Amount);
            TotalExpenses = results.Where(t => t.CategoryID >= Constants.EXPENSE_ID).Sum(t => t.Amount);

            // update top income subcategory
            var subcategoryID = results.Where(t => t.CategoryID == Constants.INCOME_ID)
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
            var categoryID = results.Where(t => t.CategoryID >= Constants.EXPENSE_ID)
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

            // get non-transfer transactions, group by date, and select date and sum of amounts on each date
            Data = new ObservableCollection<Data>(
                results.OrderByDescending(trans => trans.Date)
                       .Where(trans => trans.CategoryID != Constants.TRANSFER_ID)
                       .GroupBy(trans => trans.Date.Day)
                       .Select(group => new Data
                       {
                           Date = group.FirstOrDefault().Date,
                           Balance = runningTotal -= group.Sum(t => t.Amount)
                       }));
        }
    }

    public class Data
    {
        public DateTime Date { get; set; }
        public decimal Balance { get; set; }
    }
}
