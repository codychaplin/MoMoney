using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Views;
using MoMoney.Models;
using MoMoney.Services;
using MoMoney.Exceptions;

namespace MoMoney.ViewModels
{
    public partial class TransactionsViewModel : ObservableObject
    {
        [ObservableProperty]
        public ObservableCollection<Transaction> loadedTransactions = new();

        List<Transaction> Transactions = new();

        [ObservableProperty]
        public DateTime from = new();

        [ObservableProperty]
        public DateTime to = DateTime.Today;

        /// <summary>
        /// Depending on CRUD operation, update Transactions collection.
        /// </summary>
        public async Task Refresh(TransactionEventArgs e)
        {
            switch (e.Type)
            {
                case TransactionEventArgs.CRUD.Create:
                    {
                        Transactions.Add(e.Transaction);

                        // if transfer, add credit side too
                        if (e.Transaction.CategoryID == Constants.TRANSFER_ID)
                        {
                            try
                            {
                                var otherTrans = await TransactionService.GetTransaction(e.Transaction.TransactionID + 1);
                                Transactions.Add(otherTrans);
                            }
                            catch (TransactionNotFoundException)
                            {
                                await Shell.Current.DisplayAlert("Error", "Could not find corresponding transfer", "OK");
                            }
                        }
                        break;
                    }
                case TransactionEventArgs.CRUD.Read:
                    {
                        // get transactions from db, if count has changed, refresh Transactions collection
                        var transactions = await TransactionService.GetTransactionsFromTo(From, To);
                        if (transactions.Count() != Transactions.Count)
                        {
                            Transactions.Clear();
                            Transactions = new List<Transaction>(transactions.Reverse());
                        }
                        break;
                    }
                case TransactionEventArgs.CRUD.Update:
                    {
                        // finds transaction via ID and update values
                        Transaction transaction = e.Transaction;
                        foreach (var trans in Transactions.Where(t => t.TransactionID == transaction.TransactionID))
                        {
                            trans.Date = transaction.Date;
                            trans.AccountID = transaction.AccountID;
                            trans.Amount = transaction.Amount;
                            trans.CategoryID = transaction.CategoryID;
                            trans.SubcategoryID = transaction.SubcategoryID;
                            trans.Payee = transaction.Payee;
                            trans.TransferID = transaction.TransferID;
                        }
                        break;
                    }
                case TransactionEventArgs.CRUD.Delete:
                    {
                        // removes transaction from collection
                        Transaction trans = Transactions.Where(t => t.TransactionID == e.Transaction.TransactionID).FirstOrDefault();
                        if (trans is not null)
                            Transactions.Remove(trans);
                        break;
                    }
                default:
                    break;
            }
        }

        /// <summary>
        /// Goes to EditTransactionPage.xaml with a Transaction ID as a parameter.
        /// </summary>
        /// <param name="ID"></param>
        [RelayCommand]
        async Task GoToEditTransaction(int ID)
        {
            await Shell.Current.GoToAsync($"{nameof(EditTransactionPage)}?ID={ID}");
        }

        /// <summary>
        /// Loads items from Transactions
        /// </summary>
        /// <param name="obj"></param>
        [RelayCommand]
        async void LoadMoreItems(object obj)
        {
            var listView = obj as Syncfusion.Maui.ListView.SfListView;
            listView.IsLazyLoading = true;
            await Task.Delay(500);
            var index = LoadedTransactions.Count;
            var count = index + Constants.LOAD_COUNT >= Transactions.Count ? Transactions.Count - index : Constants.LOAD_COUNT;
            AddTransactions(index, count);
            listView.IsLazyLoading = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="count"></param>
        void AddTransactions(int index, int count)
        {
            for (int i = index; i < index + count && i < Transactions.Count; i++)
                LoadedTransactions.Add(Transactions[i]);
        }
    }
}
