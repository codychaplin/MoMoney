using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Views;
using MoMoney.Models;
using MoMoney.Services;
using MoMoney.Exceptions;
using Syncfusion.Maui.ListView;

namespace MoMoney.ViewModels
{
    public partial class TransactionsViewModel : ObservableObject
    {
        [ObservableProperty]
        public ObservableCollection<Transaction> transactions = new();

        [ObservableProperty]
        string searchText = "";

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
                            Transactions = new ObservableCollection<Transaction>(transactions.Reverse());
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
        /// Updates Transactions Filter.
        /// </summary>
        [RelayCommand]
        void ReturnPressed(object obj)
        {
            var listView = obj as SfListView;
            if (listView.DataSource != null)
            {
                listView.DataSource.Filter = FilterContacts;
                listView.DataSource.RefreshFilter();
            }
        }

        /// <summary>
        /// Checks if transaction contains text from search bar.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private bool FilterContacts(object obj)
        {
            if (SearchText.Length < 1)
                return true;

            var text = SearchText.ToLower();
            var trans = obj as Transaction;
            if (AccountService.Accounts[trans.AccountID].ToLower().Contains(text) ||
                trans.Amount.ToString().Contains(text) ||
                CategoryService.Categories[trans.CategoryID].ToLower().Contains(text) ||
                CategoryService.Categories[trans.SubcategoryID].ToLower().Contains(text))
            {
                return true;
            }
            else
                return false;
        }
    }
}
