using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoMoney.Models;
using MoMoney.Services;
using System.Collections.ObjectModel;

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
        public int accountID;

        [ObservableProperty]
        public decimal amount;

        [ObservableProperty]
        public int categoryID;

        [ObservableProperty]
        public int subcategoryID;

        [ObservableProperty]
        public string payee;

        [ObservableProperty]
        public int? transferID;

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
            var income = await CategoryService.GetCategory(1);
            Categories.Add(income);

            Subcategories.Clear();
        }

        /// <summary>
        /// Gets transfer category from database and refreshes Categories collection
        /// </summary>
        public async Task GetTransferCategory()
        {
            Categories.Clear();
            var transfer = await CategoryService.GetCategory(2);
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
                if (CategoryID == Constants.INCOME_ID) // income = regular
                {
                    await TransactionService.AddTransaction(Date, AccountID, Amount, CategoryID, SubcategoryID, Payee, null);
                }
                else if (CategoryID == Constants.TRANSFER_ID) // transfer = 2 transactions
                {
                    // must cache Observable Properties because they reset after being added to db
                    var _date = Date;
                    var _accountID = AccountID;
                    var _amount = Amount;
                    var _categoryID = CategoryID;
                    var _payee = Payee;
                    var _transferID = TransferID;
                    await TransactionService.AddTransaction(_date, _accountID, -_amount, _categoryID, Constants.DEBIT_ID, _payee, _transferID);
                    await TransactionService.AddTransaction(_date, (int)_transferID, _amount, _categoryID, Constants.CREDIT_ID, _payee, _accountID);
                }
                else if (CategoryID >= Constants.EXPENSE_ID) // expense = negative amount
                {
                    await TransactionService.AddTransaction(Date, AccountID, -Amount, CategoryID, SubcategoryID, Payee, null);
                }
            }
            catch (Exception ex)
            {
                // if invalid, display error
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
            
        }
    }
}
