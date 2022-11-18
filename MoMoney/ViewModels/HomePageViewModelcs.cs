using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Models;
using MoMoney.Services;
using System.Collections.ObjectModel;

namespace MoMoney.ViewModels
{
    public partial class HomePageViewModel : ObservableObject
    {
        [ObservableProperty]
        public ObservableCollection<Transaction> recentTransactions = new();

        /// <summary>
        /// Gets updated transactions from database and refreshes Transactions collection
        /// </summary>
        public async Task Refresh()
        {
            RecentTransactions.Clear();
            var transactions = await TransactionService.GetRecentTransactions();
            foreach (var trans in transactions)
                RecentTransactions.Add(trans);
        }
    }
}
