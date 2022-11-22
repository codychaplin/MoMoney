using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Models;
using MoMoney.Services;

namespace MoMoney.ViewModels
{
    [QueryProperty(nameof(ID), nameof(ID))]
    public partial class EditTransactionViewModel : ObservableObject
    {
        public string ID { get; set; } // Transaction ID

        [ObservableProperty]
        public ObservableCollection<Account> accounts = new();

        [ObservableProperty]
        public ObservableCollection<Category> categories = new();

        [ObservableProperty]
        public ObservableCollection<Category> subcategories = new();

        [ObservableProperty]
        public Account account;

        [ObservableProperty]
        public Category category;

        [ObservableProperty]
        public Category subcategory;

        [ObservableProperty]
        public Account payeeAccount;

        [ObservableProperty]
        public Transaction transaction;
        
        public Account InitialAccount { get; private set; }
        public Category InitialCategory { get; private set; }
        public Category InitialSubcategory { get; private set; }
        public Account InitialPayeeAccount { get; private set; }

        /// <summary>
        /// Gets Transaction using ID and updates Account, Category, and Subcategory
        /// </summary>
        public async Task GetTransaction()
        {
            if (int.TryParse(ID, out int id))
            {
                Transaction = await TransactionService.GetTransaction(id);
                InitialAccount = await AccountService.GetAccount(Transaction.AccountID);
                InitialCategory = await CategoryService.GetCategory(Transaction.CategoryID);
                InitialSubcategory = await CategoryService.GetCategory(Transaction.SubcategoryID);

                if (InitialCategory.CategoryID >= Constants.EXPENSE_ID) // if expense, make amount appear positive for user
                    Transaction.Amount *= -1;
                else if (InitialCategory.CategoryID == Constants.TRANSFER_ID) // if transfer, set initial payee account
                {
                    // if debit, make amount appear positive for user
                    if (InitialSubcategory.CategoryID == Constants.DEBIT_ID)
                        Transaction.Amount *= -1;
                    InitialPayeeAccount = await AccountService.GetAccount((int)Transaction.TransferID);
                }
            }
            else
                await Shell.Current.DisplayAlert("Error", "Could not find transaction", "OK");
        }

        /// <summary>
        /// Gets accounts from database and refreshes Accounts collection
        /// </summary>
        /// <returns></returns>
        public async Task GetAccounts()
        {
            Accounts.Clear();
            // TODO: if using disabled account, retrieve from db as well
            var accounts = await AccountService.GetActiveAccounts();
            foreach (var acc in accounts)
                Accounts.Add(acc);

            Account = InitialAccount;
            if (InitialCategory.CategoryID == Constants.TRANSFER_ID)
                PayeeAccount = InitialPayeeAccount;
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

            Category = InitialCategory;
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

            Category = InitialCategory;
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

            Category = InitialCategory;
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

            Subcategory = InitialSubcategory;
        }

        /// <summary>
        /// Edits Transaction in database using input fields from view
        /// </summary>
        [RelayCommand]
        async Task Edit()
        {
            if (Transaction is null ||
                Account is null ||
                Category is null ||
                Subcategory is null)
            {
                // if invalid, display error
                await Shell.Current.DisplayAlert("Error", "Information not valid", "OK");
            }
            else
            {
                // update transaction
                if (InitialCategory.CategoryID >= Constants.EXPENSE_ID ||
                    InitialSubcategory.CategoryID == Constants.DEBIT_ID) // if expense or transfer debit, convert back to negative
                    Transaction.Amount *= -1;
                Transaction.AccountID = Account.AccountID;
                Transaction.CategoryID = Category.CategoryID;
                Transaction.SubcategoryID = Subcategory.CategoryID;
                Transaction.TransferID = (PayeeAccount != null) ? PayeeAccount.AccountID : null;
                await TransactionService.UpdateTransaction(Transaction);

                // if transfer, update other side of transfer
                if (Transaction.CategoryID == Constants.TRANSFER_ID)
                {
                    int transID = Transaction.TransactionID;
                    int otherTransID = (Transaction.SubcategoryID == Constants.DEBIT_ID) ? transID + 1 : transID - 1;
                    Transaction otherTrans = await TransactionService.GetTransaction(otherTransID);
                    otherTrans.AccountID = (int)Transaction.TransferID;
                    otherTrans.TransferID = Transaction.AccountID;
                    if (otherTrans.CategoryID == Constants.DEBIT_ID) // if debit, convert back to negative
                        otherTrans.Amount = Transaction.Amount * -1;
                    await TransactionService.UpdateTransaction(otherTrans);
                }
                await Shell.Current.GoToAsync("..");
            }
        }

        /// <summary>
        /// Removes the Transaction from the database
        /// </summary>
        [RelayCommand]
        async Task Remove()
        {
            bool flag = await Shell.Current.DisplayAlert("", "Are you sure you want to delete this transaction?", "Yes", "No");

            if (flag)
            {
                await TransactionService.RemoveTransaction(Transaction.TransactionID);
                await Shell.Current.GoToAsync("..");
            }
        }
    }
}
