using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Models;
using MoMoney.Services;

namespace MoMoney.ViewModels
{
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
        public ObservableCollection<Data> data = new();

        public async Task Refresh()
        {
            await GetRecentTransactions();
            await GetChartData();
        }

        /// <summary>
        /// Gets updated transactions from database and refreshes Transactions collection.
        /// </summary>
        public async Task GetRecentTransactions()
        {
            RecentTransactions.Clear();
            var transactions = await TransactionService.GetRecentTransactions(To);
            foreach (var trans in transactions)
                RecentTransactions.Add(trans);
        }

        /// <summary>
        /// Gets updated total balance of all accounts combined.
        /// </summary>
        public async Task GetNetworth()
        {
            var accounts = await AccountService.GetActiveAccounts();
            Networth = 0;
            foreach (var acc in accounts)
                Networth += acc.CurrentBalance;
            isNetworthSet = true;
        }

        /// <summary>
        /// Gets data for running balance chart.
        /// </summary>
        public async Task GetChartData()
        {
            if (!isNetworthSet)
                await GetNetworth();
            decimal runningTotal = Networth; // running total starts at current net worth

            var results = await TransactionService.GetTransactionsFromTo(From, To);
            if (!results.Any())
                return;

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

            // if the date range is > 1 year, group results by Month, if < 1 year, group by day
            // get non-transfer transactions, group by date, and select date and sum of amounts on each date
            bool isLong = (To - From).TotalDays > 365;
            if (isLong)
            {
                Data = new ObservableCollection<Data>(
                    results.OrderByDescending(trans => trans.Date)
                           .Where(trans => trans.CategoryID != Constants.TRANSFER_ID)
                           .GroupBy(trans => trans.Date.Month)
                           .Select(group => new Data
                           {
                               Date = group.FirstOrDefault().Date,
                               Balance = runningTotal -= group.Sum(t => t.Amount)
                           }));
            }
            else
            {
                Data = new ObservableCollection<Data>(
                    results.OrderByDescending(trans => trans.Date)
                           .Where(trans => trans.CategoryID != Constants.TRANSFER_ID)
                           .GroupBy(trans => trans.Date)
                           .Select(group => new Data
                           {
                               Date = group.FirstOrDefault().Date,
                               Balance = runningTotal -= group.Sum(t => t.Amount)
                           }));
            }
        }
    }

    public class Data
    {
        public DateTime Date { get; set; }
        public decimal Balance { get; set; }
    }
}
