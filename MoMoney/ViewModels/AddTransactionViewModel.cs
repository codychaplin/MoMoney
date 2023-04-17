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
    public string payee;

    [ObservableProperty]
    public Account transferAccount = new();

    /// <summary>
    /// Gets accounts from database and refreshes Accounts collection.
    /// </summary>
    /// <returns></returns>
    public async void GetAccounts(object sender, EventArgs e)
    {
        var accounts = await AccountService.GetActiveAccounts();
        Accounts.Clear();
        foreach (var acc in accounts)
            Accounts.Add(acc);
    }

    /// <summary>
    /// Gets income category from database and refreshes Categories collection.
    /// </summary>
    public async Task GetIncomeCategory()
    {
        try
        {
            var income = await CategoryService.GetCategory(Constants.INCOME_ID);
            Categories.Clear();
            Categories.Add(income);
            Subcategories.Clear();
        }
        catch (CategoryNotFoundException ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Gets transfer category from database and refreshes Categories collection.
    /// </summary>
    public async Task GetTransferCategory()
    {
        try
        {
            var transfer = await CategoryService.GetCategory(Constants.TRANSFER_ID);
            Categories.Clear();
            Categories.Add(transfer);
            Subcategories.Clear();
        }
        catch (CategoryNotFoundException ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Gets updated expense categories from database and refreshes Categories collection.
    /// </summary>
    public async Task GetExpenseCategories()
    {
        var categories = await CategoryService.GetExpenseCategories();
        Categories.Clear();
        foreach (var cat in categories)
            Categories.Add(cat);
        Subcategories.Clear();
    }

    /// <summary>
    /// Updates Subcategories based on selected parent Category.
    /// </summary>
    /// <param name="parentCategory"></param>
    public async Task GetSubcategories(Category parentCategory)
    {
        if (parentCategory is not null)
        {
            var subcategories = await CategoryService.GetSubcategories(parentCategory);
            Subcategories.Clear();
            foreach (var cat in subcategories)
                Subcategories.Add(cat);
        }
    }

    public async void GetPayees(object sender, EventArgs e)
    {
        var payees = await TransactionService.GetPayeesFromTransactions();
        Payees = new ObservableCollection<string>(payees);

    }

    /// <summary>
    /// adds Category to database using input fields from view.
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
}
