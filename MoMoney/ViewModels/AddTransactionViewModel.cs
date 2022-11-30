using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Models;
using MoMoney.Views;
using MoMoney.Services;
using MoMoney.Exceptions;

namespace MoMoney.ViewModels
{
    public partial class AddTransactionViewModel : ObservableObject
    {
        [ObservableProperty]
        public ObservableCollection<Account> accounts = new();

        [ObservableProperty]
        public ObservableCollection<Category> categories = new();

        [ObservableProperty]
        public ObservableCollection<Category> subcategories = new();

        [ObservableProperty]
        public DateTime date;

        [ObservableProperty]
        public Account account = new();

        [ObservableProperty]
        public decimal amount;

        [ObservableProperty]
        public Category category = new();

        [ObservableProperty]
        public Category subcategory = new();

        [ObservableProperty]
        public string payee;

        [ObservableProperty]
        public Account transferAccount = new();

        /// <summary>
        /// Gets accounts from database and refreshes Accounts collection
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
        /// Gets income category from database and refreshes Categories collection
        /// </summary>
        public async Task GetIncomeCategory()
        {
            Categories.Clear();
            var income = await CategoryService.GetCategory(Constants.INCOME_ID);
            Categories.Add(income);

            Subcategories.Clear();
        }

        /// <summary>
        /// Gets transfer category from database and refreshes Categories collection
        /// </summary>
        public async Task GetTransferCategory()
        {
            Categories.Clear();
            var transfer = await CategoryService.GetCategory(Constants.TRANSFER_ID);
            Categories.Add(transfer);

            Subcategories.Clear();
        }

        /// <summary>
        /// Gets updated expense categories from database and refreshes Categories collection
        /// </summary>
        public async Task GetExpenseCategories()
        {
            Categories.Clear();
            var categories = await CategoryService.GetExpenseCategories();
            foreach (var cat in categories)
                Categories.Add(cat);
            
            Subcategories.Clear();
        }

        /// <summary>
        /// Updates Subcategories based on selected parent Category
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
        /// adds Category to database using input fields from view
        /// </summary>
        [RelayCommand]
        async Task Add()
        {
            try
            {
                Transaction transaction = new();

                if (Category.CategoryID == Constants.INCOME_ID) // income = regular
                {
                    transaction = await TransactionService.AddTransaction(Date, Account.AccountID, Amount,Category.CategoryID, Subcategory.CategoryID, Payee, null);
                }
                else if (Category.CategoryID == Constants.TRANSFER_ID) // transfer = 2 transactions
                {
                    // must cache Observable Properties because they reset after being added to db
                    var _date = Date;
                    var _accountID = Account.AccountID;
                    var _amount = Amount;
                    var _categoryID = Category.CategoryID;
                    var _transferID = TransferAccount.AccountID;
                    transaction = await TransactionService.AddTransaction(_date, _accountID, -_amount, _categoryID, Constants.DEBIT_ID, "", _transferID);
                    await TransactionService.AddTransaction(_date, _transferID, _amount, _categoryID, Constants.CREDIT_ID, "", _accountID);

                    await AccountService.UpdateBalance(_transferID, _amount); // update corresponding Account balance
                }
                else if (Category.CategoryID >= Constants.EXPENSE_ID) // expense = negative amount
                {
                    transaction = await TransactionService.AddTransaction(Date, Account.AccountID, -Amount, Category.CategoryID, Subcategory.CategoryID, Payee, null);
                }

                if (transaction is null)
                    throw new InvalidTransactionException("Could not get new Transaction from database");

                await AccountService.UpdateBalance(transaction.AccountID, transaction.Amount); // update corresponding Account balance

                // update TransactionsPage Transactions
                var args = new TransactionEventArgs(transaction, TransactionEventArgs.CRUD.Create);
                TransactionsPage.TransactionsChanged?.Invoke(this, args);
            }
            catch (InvalidTransactionException ex)
            {
                // if invalid, display error
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
            
        }
    }
}
