using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Views;
using MoMoney.Models;
using MoMoney.Services;

namespace MoMoney.ViewModels
{
    public partial class TransactionsViewModel : ObservableObject
    {
        [ObservableProperty]
        public ObservableCollection<Transaction> transactions = new();

        public bool HasChanged { get; set; }

        /// <summary>
        /// Gets updated transactions from database and refreshes Transactions collection
        /// </summary>
        public async Task Refresh()
        {
            Transactions.Clear();
            var transactions = await TransactionService.GetTransactions();
            foreach (var trans in transactions)
                Transactions.Add(trans);
        }

        /// <summary>
        /// Goes to EditTransactionPage.xaml with a Transaction ID as a parameter
        /// </summary>
        [RelayCommand]
        async Task GoToEditTransaction(int ID)
        {
            await Shell.Current.GoToAsync($"{nameof(EditTransactionPage)}?ID={ID}");
        }
    }
}
