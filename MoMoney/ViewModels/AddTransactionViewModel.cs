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
    public async Task GetAccounts()
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
            // if invalid, display error
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

    /// <summary>
    /// Prompts the user to open a CSV file. Valid Transactions are then added to the database.
    /// </summary>
    [RelayCommand]
    async Task ImportTransactionsCSV()
    {
        try
        {
            var options = new PickOptions { PickerTitle = "Select a .CSV file" };
            var result = await FilePicker.Default.PickAsync(options);

            if (result != null)
            {
                if (result.FileName.EndsWith("csv", StringComparison.OrdinalIgnoreCase))
                {
                    List<Transaction> transactions = new();

                    using var sr = new StreamReader(result.FullPath);
                    try
                    {
                        int i = 1;
                        while (sr.Peek() != -1)
                        {
                            // format line
                            string row = sr.ReadLine();
                            string[] transactionsInfo = row.Split(',');

                            if (transactionsInfo.Length != 6)
                                throw new FormatException($"Transaction {i} does not have the correct amount of fields");

                            // split line into Transaction parameters and create new Transaction

                            // date
                            if (!DateTime.TryParse(transactionsInfo[0], out var date))
                                throw new InvalidTransactionException($"Transaction {i}: Invalid date");

                            // account ID
                            var account = await AccountService.GetAccount(transactionsInfo[1]);
                            if (account == null)
                                throw new InvalidTransactionException($"Transaction {i}: Account '{transactionsInfo[1]}' does not exist");

                            // amount
                            if (!decimal.TryParse(transactionsInfo[2], out decimal amount))
                                throw new InvalidTransactionException($"Transaction {i}: '{transactionsInfo[2]}' is not a valid number");

                            // category ID
                            var parentCategory = await CategoryService.GetParentCategory(transactionsInfo[3]);
                            if (category == null)
                                throw new InvalidTransactionException($"Transaction {i}: '{transactionsInfo[3]}' is not a valid parent category");

                            // subcategory ID
                            var subcategory = await CategoryService.GetCategory(transactionsInfo[4], parentCategory.CategoryName);
                            if (subcategory == null)
                                throw new InvalidTransactionException($"Transaction {i}: '{transactionsInfo[4]}' is not a valid subcategory");

                            // payee
                            string payee = "";
                            if (parentCategory.CategoryID != Constants.TRANSFER_ID && string.IsNullOrEmpty(transactionsInfo[5]))
                                throw new InvalidTransactionException($"Transaction {i}: Payee cannot be blank");
                            else
                                payee = transactionsInfo[5];

                            // transfer ID
                            int? transferID;
                            if (subcategory.CategoryID == Constants.CREDIT_ID)
                            {
                                transactions[i - 2].TransferID = account.AccountID; // -2 because i started at 1, not 0
                                transferID = transactions[i - 2].AccountID;
                            }
                            else
                                transferID = null;

                            Transaction transaction = new()
                            {
                                Date = date,
                                AccountID = account.AccountID,
                                Amount = amount,
                                CategoryID = parentCategory.CategoryID,
                                SubcategoryID = subcategory.CategoryID,
                                Payee = payee,
                                TransferID = transferID
                            };

                            transactions.Add(transaction);
                            i++;
                        }

                        await TransactionService.AddTransactions(transactions);
                    }
                    catch (SQLiteException ex)
                    {
                        await Shell.Current.DisplayAlert("Database Error", ex.Message, "OK");
                    }
                    catch (CategoryNotFoundException ex)
                    {
                        await Shell.Current.DisplayAlert("Category Not Found Error", ex.Message, "OK");
                    }
                    catch (AccountNotFoundException ex)
                    {
                        await Shell.Current.DisplayAlert("Account Not Found Error", ex.Message, "OK");
                    }
                    finally
                    {
                        sr.Close();
                    }
                    
                }
                else
                    throw new FormatException("Invalid file type. Must be a CSV");
            }
        }
        catch (Exception ex)
        {
            // if invalid, display error
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }
}
