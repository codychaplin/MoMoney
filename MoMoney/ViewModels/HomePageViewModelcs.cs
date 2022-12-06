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
        public decimal total = 0;

        [ObservableProperty]
        public ObservableCollection<Data> data = new();

        DateTime From = new DateTime(DateTime.Today.Year, 1, 1);
        DateTime To = DateTime.Today;

        /// <summary>
        /// Gets updated transactions from database and refreshes Transactions collection
        /// </summary>
        public async Task GetRecentTransactions()
        {
            RecentTransactions.Clear();
            var transactions = await TransactionService.GetRecentTransactions();
            foreach (var trans in transactions)
                RecentTransactions.Add(trans);
        }

        /// <summary>
        /// Gets updated total balance of all accounts combined
        /// </summary>
        public async Task GetTotalBalance()
        {
            var accounts = await AccountService.GetActiveAccounts();
            foreach (var acc in accounts)
                Total += acc.CurrentBalance;
        }

        /// <summary>
        /// Gets data for running balance chart
        /// </summary>
        public async Task GetChartData()
        {
            decimal runningTotal = Total; // running total starts at current net worth
            var results = await TransactionService.GetTransactionsFromTo(From, To);

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
