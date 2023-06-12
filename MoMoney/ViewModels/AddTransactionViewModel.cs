using System.Collections.ObjectModel;
using SQLite;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Views;
using MoMoney.Models;
using MoMoney.Services;
using MoMoney.Exceptions;

namespace MoMoney.ViewModels;

public partial class AddTransactionViewModel : ObservableObject
{
    readonly IAccountService accountService;
    readonly ICategoryService categoryService;
    readonly ITransactionService transactionService;

    [ObservableProperty]
    public ObservableCollection<Account> accounts = new();

    [ObservableProperty]
    public ObservableCollection<Category> categories = new();

    [ObservableProperty]
    public ObservableCollection<Category> subcategories = new();

    [ObservableProperty]
    public ObservableCollection<string> payees = new();

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
    public Account transferAccount = new();

    public AddTransactionViewModel(ITransactionService _transactionService, IAccountService _accountService, ICategoryService _categoryService)
    {
        transactionService = _transactionService;
        accountService = _accountService;
        categoryService = _categoryService;
    }

    /// <summary>
    /// Gets accounts from database and refreshes Accounts collection.
    /// </summary>
    /// <returns></returns>
    public async void GetAccounts(object sender, EventArgs e)
    {
        var accounts = await accountService.GetActiveAccounts();
        Accounts.Clear();
        foreach (var acc in accounts)
            Accounts.Add(acc);
    }

    /// <summary>
    /// Gets income category from database and refreshes Categories collection.
    /// </summary>
    [RelayCommand]
    async Task GetIncomeCategory()
    {
        try
        {
            var income = await categoryService.GetCategory(Constants.INCOME_ID);
            Categories.Clear();
            Subcategories.Clear();
            Categories.Add(income);
            Category = income;
        }
        catch (CategoryNotFoundException ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Gets transfer category from database and refreshes Categories collection.
    /// </summary>
    [RelayCommand]
    async Task GetTransferCategory()
    {
        try
        {
            var transfer = await categoryService.GetCategory(Constants.TRANSFER_ID);
            Categories.Clear();
            Subcategories.Clear();
            Categories.Add(transfer);
            Category = transfer;
        }
        catch (CategoryNotFoundException ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Gets updated expense categories from database and refreshes Categories collection.
    /// </summary>
    [RelayCommand]
    async Task GetExpenseCategories()
    {
        var categories = await categoryService.GetExpenseCategories();
        Categories.Clear();
        foreach (var cat in categories)
            Categories.Add(cat);
        Subcategories.Clear();
    }

    /// <summary>
    /// Updates Subcategories based on selected parent Category.
    /// </summary>
    /// <param name="parentCategory"></param>
    async Task GetSubcategories(Category parentCategory)
    {
        if (parentCategory is null)
            return;

        var subcategories = await categoryService.GetSubcategories(parentCategory);
        Subcategories.Clear();
        foreach (var cat in subcategories)
            Subcategories.Add(cat);
    }

    public async void GetPayees(object sender, EventArgs e)
    {
        var payees = await transactionService.GetPayeesFromTransactions();
        Payees = new ObservableCollection<string>(payees);
    }

    public async void CategoryChanged(object sender, EventArgs e)
    {
        // check if Category is null, update subcategories, if transfer, auto-select "Debit"
        if (Category is null) return;
        await GetSubcategories(Category);
        if (Category.CategoryID == Constants.TRANSFER_ID && Subcategories.Count > 0)
            Subcategory = Subcategories.First();
    }

    /// <summary>
    /// adds Category to database using input fields from view.
    /// </summary>
    /// <param name="payee"></param>
    [RelayCommand]
    async Task Add(string payee)
    {
        try
        {
            if (Account is null || Category is null || Subcategory is null || 
                (Category?.CategoryID == Constants.TRANSFER_ID && TransferAccount is null))
                return;

            Transaction transaction = new();

            // add payee to Payees if not already in list
            if (!Payees.Contains(payee))
                Payees.Add(payee);

            if (Category.CategoryID == Constants.INCOME_ID) // income = regular
            {
                transaction = await transactionService.AddTransaction(Date, Account.AccountID, Amount,Category.CategoryID, Subcategory.CategoryID, payee, null);
            }
            else if (Category.CategoryID == Constants.TRANSFER_ID) // transfer = 2 transactions
            {
                // must cache Observable Properties because they reset after being added to db
                var _date = Date;
                var _accountID = Account.AccountID;
                var _amount = Amount;
                var _categoryID = Category.CategoryID;
                var _transferID = TransferAccount.AccountID;
                transaction = await transactionService.AddTransaction(_date, _accountID, -_amount, _categoryID, Constants.DEBIT_ID, "", _transferID);
                await transactionService.AddTransaction(_date, _transferID, _amount, _categoryID, Constants.CREDIT_ID, "", _accountID);

                await accountService.UpdateBalance(_transferID, _amount); // update corresponding Account balance
            }
            else if (Category.CategoryID >= Constants.EXPENSE_ID) // expense = negative amount
            {
                transaction = await transactionService.AddTransaction(Date, Account.AccountID, -Amount, Category.CategoryID, Subcategory.CategoryID, payee, null);
            }

            if (transaction is null)
                throw new InvalidTransactionException("Could not get new Transaction from database");

            await accountService.UpdateBalance(transaction.AccountID, transaction.Amount); // update corresponding Account balance

            ClearAfterAdd();

            // update TransactionsPage Transactions
            var args = new TransactionEventArgs(transaction, TransactionEventArgs.CRUD.Create);
            TransactionsPage.TransactionsChanged?.Invoke(this, args);
        }
        catch (InvalidTransactionException ex)
        {
            await Shell.Current.DisplayAlert("Validation Error", ex.Message, "OK");
        }
        catch (SQLiteException ex)
        {
            await Shell.Current.DisplayAlert("Database Error", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    void ClearAfterAdd()
    {
        Amount = 0;
        TransferAccount = null;
    }

    public void Clear()
    {
        Date = DateTime.Today;
        Account = null;
        Amount = 0;
        Category = null;
        Subcategory = null;
        TransferAccount = null;

        Categories.Clear();
        Subcategories.Clear();
    }
}