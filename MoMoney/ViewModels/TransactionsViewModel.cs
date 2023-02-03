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
        public ObservableCollection<Account> accounts = new();

        [ObservableProperty]
        public ObservableCollection<Category> categories = new();

        [ObservableProperty]
        public ObservableCollection<Category> subcategories = new();

        [ObservableProperty]
        public Account account;

        [ObservableProperty]
        public int amountRangeStart = 0;

        [ObservableProperty]
        public int amountRangeEnd = 500;

        [ObservableProperty]
        public Category category;

        [ObservableProperty]
        public Category subcategory;

        [ObservableProperty]
        public string searchText = "";

        [ObservableProperty]
        public DateTime from = new();

        [ObservableProperty]
        public DateTime to = DateTime.Today;

        public SfListView ListView { get; set; }

        /// <summary>
        /// Depending on CRUD operation, update Transactions collection.
        /// </summary>
        public async Task Refresh(TransactionEventArgs e)
        {
            switch (e.Type)
            {
                case TransactionEventArgs.CRUD.Create:
                    {
                        int index = BinarySearch(e.Transaction);
                        Transactions.Insert(index, e.Transaction);

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
                            Transactions = new ObservableCollection<Transaction>(transactions);
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
        /// Gets accounts from database.
        /// </summary>
        /// <returns></returns>
        public async Task GetAccounts()
        {
            Accounts.Clear();
            var accounts = await AccountService.GetActiveAccounts();
            foreach (var acc in accounts)
                Accounts.Add(acc);
        }

        /// <summary>
        /// Gets all parent categories from database.
        /// </summary>
        public async Task GetParentCategories()
        {
            Categories.Clear();
            var categories = await CategoryService.GetAllParentCategories();
            foreach (var cat in categories)
                Categories.Add(cat);
        }

        /// <summary>
        /// Updates Subcategories based on selected parent Category.
        /// </summary>
        /// <param name="parentCategory"></param>
        public async Task GetSubcategories(Category parentCategory)
        {
            Subcategories.Clear();
            if (parentCategory is not null)
            {
                var subcategories = await CategoryService.GetSubcategories(parentCategory);
                foreach (var cat in subcategories)
                    Subcategories.Add(cat);
            }
        }

        /// <summary>
        /// Clears selected Account
        /// </summary>
        [RelayCommand]
        void ClearAccount()
        {
            Account = null;
            UpdateFilter();
        }

        /// <summary>
        /// Clears selected Category
        /// </summary>
        [RelayCommand]
        void ClearCategory()
        {
            Category = null;
            UpdateFilter();
        }

        /// <summary>
        /// Clears selected Subcategory
        /// </summary>
        [RelayCommand]
        void ClearSubcategory()
        {
            Subcategory = null;
            UpdateFilter();
        }

        /// <summary>
        /// Calls UpdateFilter()
        /// </summary>
        [RelayCommand]
        void ReturnPressed()
        {
            UpdateFilter();
        }

        /// <summary>
        /// Calls UpdateFilter()
        /// </summary>
        [RelayCommand]
        void AmountDragCompleted()
        {
            UpdateFilter();
        }

        /// <summary>
        /// Updates Transactions Filter.
        /// </summary>
        public void UpdateFilter()
        {
            if (ListView.DataSource != null)
            {
                ListView.DataSource.Filter = FilterTransactions;
                ListView.DataSource.RefreshFilter();
            }
        }

        /// <summary>
        /// Checks if transaction contains text from search bar or specified Account/(sub)category.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        bool FilterTransactions(object obj)
        {
            // if all are blank, show Transaction
            if (Account == null && Category == null && Subcategory == null &&
                AmountRangeStart == 0 && AmountRangeEnd == 500 && SearchText == "")
                return true;

            var trans = obj as Transaction;
            var amount = Math.Abs(trans.Amount);
            var text = SearchText.ToLower();

            // if fields aren't blank and match text/amount/IDs, show Transaction
            if ((Account != null && trans.AccountID == Account.AccountID) ||
                (amount >= AmountRangeStart && (amount <= AmountRangeEnd || AmountRangeEnd == 500)) ||
                (Category != null && Subcategory == null && trans.CategoryID == Category.CategoryID) ||
                (Subcategory != null && trans.SubcategoryID == Subcategory.CategoryID) ||
                text.Length > 0 && trans.Payee.ToLower().Contains(text))
                return true;
            else
                return false;
        }

        /// <summary>
        /// Binary search used to find index in sorted CollectionView.
        /// </summary>
        /// <param name="newTransaction"></param>
        /// <returns>Index in Transactions where newTransaction should be inserted.</returns>
        int BinarySearch(Transaction newTransaction)
        {
            int left = 0;
            int right = Transactions.Count - 1;

            while (left <= right)
            {
                int middle = (left + right) / 2;
                int comparison = Transactions[middle].Date.CompareTo(newTransaction.Date);

                if (comparison == 0)
                    return Transactions.Count - middle;
                else if (comparison < 0)
                    left = middle + 1;
                else
                    right = middle - 1;
            }

            // minus from Transaction.Count because list is reversed
            return Transactions.Count - left;
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
    }
}
