using System.Collections.ObjectModel;
using SQLite;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Views;
using MoMoney.Models;
using MoMoney.Services;
using MoMoney.Exceptions;
using MoMoney.Helpers;

namespace MoMoney.ViewModels;

[QueryProperty(nameof(ID), "ID")]
public partial class EditTransactionViewModel : ObservableObject
{
    readonly IAccountService accountService;
    readonly ICategoryService categoryService;
    readonly ITransactionService transactionService;
    readonly ILoggerService<EditTransactionViewModel> logger;

    public string ID { get; set; }

    [ObservableProperty]
    public ObservableCollection<Account> accounts = new();

    [ObservableProperty]
    public ObservableCollection<Category> categories = new();

    [ObservableProperty]
    public ObservableCollection<Category> subcategories = new();

    [ObservableProperty]
    public ObservableCollection<string> payees = new();

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

    Transaction InitialTransaction;

    public EditTransactionViewModel(ITransactionService _transactionService, IAccountService _accountService,
        ICategoryService _categoryService, ILoggerService<EditTransactionViewModel> _logger)
    {
        transactionService = _transactionService;
        accountService = _accountService;
        categoryService = _categoryService;
        logger = _logger;
    }

    /// <summary>
    /// Gets Transaction using ID and updates Account, Category, and Subcategory.
    /// </summary>
    public async Task GetTransaction()
    {
        if (int.TryParse(ID, out int id))
        {
            try
            {
                Transaction = await transactionService.GetTransaction(id);

                InitialTransaction = new Transaction(Transaction);
                InitialAccount = await accountService.GetAccount(InitialTransaction.AccountID);
                InitialCategory = await categoryService.GetCategory(InitialTransaction.CategoryID);
                InitialSubcategory = await categoryService.GetCategory(InitialTransaction.SubcategoryID);

                if (InitialCategory.CategoryID >= Constants.EXPENSE_ID) // if expense, make amount appear positive for user
                    Transaction.Amount *= -1;
                else if (InitialCategory.CategoryID == Constants.TRANSFER_ID) // if transfer, set initial payee account
                {
                    // if debit, make amount appear positive for user
                    if (InitialSubcategory.CategoryID == Constants.DEBIT_ID)
                        Transaction.Amount *= -1;
                    InitialPayeeAccount = await accountService.GetAccount((int)Transaction.TransferID);
                }
            }
            catch (TransactionNotFoundException ex)
            {
                await logger.LogError(ex.Message, ex.GetType().Name);
                await Shell.Current.DisplayAlert("Transaction Error", ex.Message, "OK");
            }
            catch (AccountNotFoundException ex)
            {
                await logger.LogError(ex.Message, ex.GetType().Name);
                await Shell.Current.DisplayAlert("Account Error", ex.Message, "OK");
            }
            catch (CategoryNotFoundException ex)
            {
                await logger.LogError(ex.Message, ex.GetType().Name);
                await Shell.Current.DisplayAlert("Category Error", ex.Message, "OK");
            }
        }
        else
        {
            string message = $"{ID} is not a valid ID";
            await logger.LogError(message);
            await Shell.Current.DisplayAlert("Transaction ID Error", message, "OK");
        }
    }

    /// <summary>
    /// Gets accounts from database and refreshes Accounts collection.
    /// </summary>
    /// <returns></returns>
    public async Task GetAccounts()
    {
        // TODO: if using disabled account, retrieve from db as well
        var accounts = await accountService.GetActiveAccounts();
        Accounts.Clear();
        foreach (var acc in accounts)
            Accounts.Add(acc);

        Account = InitialAccount;
        if (InitialCategory.CategoryID == Constants.TRANSFER_ID)
            PayeeAccount = InitialPayeeAccount;
    }

    /// <summary>
    /// Gets income category from database and refreshes Categories collection.
    /// </summary>
    public async Task GetIncomeCategory()
    {
        try
        {
            var income = await categoryService.GetCategory(Constants.INCOME_ID);
            Categories.Clear();
            Categories.Add(income);
            Subcategories.Clear();
            Category = InitialCategory;
        }
        catch (CategoryNotFoundException ex)
        {
            await logger.LogError(ex.Message, ex.GetType().Name);
            await Shell.Current.DisplayAlert("Category Not Found Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Gets transfer category from database and refreshes Categories collection.
    /// </summary>
    public async Task GetTransferCategory()
    {
        try
        {
            var transfer = await categoryService.GetCategory(Constants.TRANSFER_ID);
            Categories.Clear();
            Categories.Add(transfer);
            Subcategories.Clear();
            Category = InitialCategory;
        }
        catch (CategoryNotFoundException ex)
        {
            await logger.LogError(ex.Message, ex.GetType().Name);
            await Shell.Current.DisplayAlert("Category Not Found Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Gets updated expense categories from database and refreshes Categories collection.
    /// </summary>
    public async Task GetExpenseCategories()
    {
        var categories = await categoryService.GetExpenseCategories();
        Categories.Clear();
        foreach (var cat in categories)
            Categories.Add(cat);
        Subcategories.Clear();
        Category = InitialCategory;
    }

    /// <summary>
    /// Updates Subcategories based on selected parent Category.
    /// </summary>
    /// <param name="parentCategory"></param>
    public async Task GetSubcategories(Category parentCategory)
    {
        if (parentCategory is not null)
        {
            var subcategories = await categoryService.GetSubcategories(parentCategory);
            Subcategories.Clear();
            foreach (var cat in subcategories)
                Subcategories.Add(cat);
        }

        Subcategory = InitialSubcategory;
    }

    /// <summary>
    /// Gets all distinct payees from all transactions
    /// </summary>
    public async Task GetPayees()
    {
        var payees = await transactionService.GetPayeesFromTransactions();
        Payees = new ObservableCollection<string>(payees);
    }

    /// <summary>
    /// Edits Transaction in database using input fields from view.
    /// </summary>
    [RelayCommand]
    async Task Edit(string payee)
    {
        if (Transaction is null || Account is null || Category is null || Subcategory is null)
        {
            await Shell.Current.DisplayAlert("Validation Error", "Information not valid", "OK");
            return;
        }
        
        try
        {
            // if expense or transfer debit, convert back to negative
            if (InitialCategory.CategoryID >= Constants.EXPENSE_ID || InitialSubcategory.CategoryID == Constants.DEBIT_ID)
                Transaction.Amount *= -1;

            // if payee is not in Payees list, update
            if (Transaction.Payee is null && !string.IsNullOrEmpty(payee))
                Transaction.Payee = payee;

            // update transaction
            Transaction.AccountID = Account.AccountID;
            Transaction.CategoryID = Category.CategoryID;
            Transaction.SubcategoryID = Subcategory.CategoryID;
            Transaction.TransferID = PayeeAccount?.AccountID;

            // if nothing has changed, don't update
            if (Transaction == InitialTransaction)
            {
                await Shell.Current.GoToAsync("..");
                return;
            }

            // if account and transfer account are same, don't update
            if (Transaction.AccountID == Transaction.TransferID)
            {
                await Shell.Current.DisplayAlert("Error", "Cannot transfer to and from the same Account", "OK");
                return;
            }

            await transactionService.UpdateTransaction(Transaction);

            // if account unchanged, update account balance
            if (InitialAccount.AccountID == Transaction.AccountID)
            {
                await accountService.UpdateBalance(Transaction.AccountID, Transaction.Amount - InitialTransaction.Amount);
            }
            else // if account changed, updated original and new account balance
            {
                await accountService.UpdateBalance(InitialAccount.AccountID, -InitialTransaction.Amount);
                await accountService.UpdateBalance(Transaction.AccountID, Transaction.Amount);
            }

            // update TransactionsPage Transactions
            var args = new TransactionEventArgs(Transaction, TransactionEventArgs.CRUD.Update);
            TransactionsPage.TransactionsChanged?.Invoke(this, args);

            // if transfer, update other side of transfer
            if (Transaction.CategoryID == Constants.TRANSFER_ID)
            {
                // get other Transaction
                int transID = Transaction.TransactionID;
                int otherTransID = (Transaction.SubcategoryID == Constants.DEBIT_ID) ? transID + 1 : transID - 1;

                Transaction otherTrans = await transactionService.GetTransaction(otherTransID);

                // update
                otherTrans.AccountID = (int)Transaction.TransferID;
                otherTrans.TransferID = Transaction.AccountID;
                otherTrans.Amount = Transaction.Amount * -1;
                await transactionService.UpdateTransaction(otherTrans);

                // if payee unchanged, update payee account balance
                if (InitialTransaction.TransferID == Transaction.TransferID)
                {
                    await accountService.UpdateBalance(otherTrans.AccountID, InitialTransaction.Amount - Transaction.Amount);
                }
                else // if changed, update both
                {
                    await accountService.UpdateBalance((int)InitialTransaction.TransferID, InitialTransaction.Amount);
                    await accountService.UpdateBalance(otherTrans.AccountID, -Transaction.Amount);
                }

                // update TransactionsPage Transactions
                args.Transaction = otherTrans;
                TransactionsPage.TransactionsChanged?.Invoke(this, args);
            }
        }
        catch (SQLiteException ex)
        {
            await logger.LogCritical(ex.Message, ex.GetType().Name);
            await Shell.Current.DisplayAlert("Database Error", ex.Message, "OK");
        }
        catch (TransactionNotFoundException ex)
        {
            await logger.LogError(ex.Message, ex.GetType().Name);
            await Shell.Current.DisplayAlert("Error", "Could not find corresponding transfer", "OK");
        }
        catch (Exception ex)
        {
            await logger.LogError(ex.Message, ex.GetType().Name);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }

        await Shell.Current.GoToAsync("..");
    }

    /// <summary>
    /// Removes the Transaction from the database.
    /// </summary>
    [RelayCommand]
    async Task Remove()
    {
        bool flag = await Shell.Current.DisplayAlert("", "Are you sure you want to delete this transaction?", "Yes", "No");

        if (!flag)
            return;

        try
        {
            // remove Transaction and update corresponding Account balance
            await transactionService.RemoveTransaction(Transaction.TransactionID);
            decimal amount = (Transaction.CategoryID == Constants.INCOME_ID) ? -Transaction.Amount : Transaction.Amount;
            await accountService.UpdateBalance(Transaction.AccountID, amount);

            // update TransactionsPage Transactions
            var args = new TransactionEventArgs(Transaction, TransactionEventArgs.CRUD.Delete);
            TransactionsPage.TransactionsChanged?.Invoke(this, args);

            if (Transaction.CategoryID == Constants.TRANSFER_ID)
            {
                // get other Transaction
                int transID = Transaction.TransactionID;
                int otherTransID = (Transaction.SubcategoryID == Constants.DEBIT_ID) ? transID + 1 : transID - 1;
                Transaction otherTrans = await transactionService.GetTransaction(otherTransID);

                // remove Transaction and update corresponding Account balance
                await transactionService.RemoveTransaction(otherTransID);
                await accountService.UpdateBalance(otherTrans.AccountID, -amount);

                // update TransactionsPage Transactions
                args.Transaction = otherTrans;
                TransactionsPage.TransactionsChanged?.Invoke(this, args);
            }
        }
        catch (SQLiteException ex)
        {
            await logger.LogCritical(ex.Message, ex.GetType().Name);
            await Shell.Current.DisplayAlert("Database Error", ex.Message, "OK");
        }
        catch (TransactionNotFoundException ex)
        {
            await logger.LogError(ex.Message, ex.GetType().Name);
            await Shell.Current.DisplayAlert("Error", "Could not find corresponding transfer", "OK");
        }
        catch (Exception ex)
        {
            await logger.LogError(ex.Message, ex.GetType().Name);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }

        await Shell.Current.GoToAsync("..");

    }
}