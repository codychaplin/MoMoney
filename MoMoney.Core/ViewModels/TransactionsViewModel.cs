using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels;

public partial class TransactionsViewModel : ObservableObject
{
    readonly IAccountService accountService;
    readonly ICategoryService categoryService;
    readonly ITransactionService transactionService;

    [ObservableProperty] ObservableCollection<Transaction> loadedTransactions = [];

    [ObservableProperty] ObservableCollection<Account> accounts = [];
    [ObservableProperty] ObservableCollection<Category> categories = [];
    [ObservableProperty] ObservableCollection<Category> subcategories = [];
    [ObservableProperty] ObservableCollection<string> payees = [];

    [ObservableProperty] Account account;

    [ObservableProperty] int amountRangeStart = 0;
    [ObservableProperty] int amountRangeEnd = 500;

    [ObservableProperty] Category category;
    [ObservableProperty] Category subcategory;
    [ObservableProperty] string payee = "";

    [ObservableProperty] static DateTime from = new();
    [ObservableProperty] static DateTime to = new();

    List<Transaction> Transactions = [];

    bool showValue = true;

    public TransactionsViewModel(ITransactionService _transactionService, IAccountService _accountService,
        ICategoryService _categoryService)
    {
        transactionService = _transactionService;
        accountService = _accountService;
        categoryService = _categoryService;

        // first two months, show 1 year, starting March show YTD
        From = (DateTime.Today.Month <= 2) ? DateTime.Today.AddYears(-1) : new(DateTime.Today.Year, 1, 1);
        To = DateTime.Today;
    }

    /// <summary>
    /// Loads data into filter pickers.
    /// </summary>
    public async Task Load()
    {
        await GetAccounts();
        await GetParentCategories();
        await Refresh(new(null, TransactionEventArgs.CRUD.Read));
    }

    /// <summary>
    /// Depending on CRUD operation, update Transactions collection.
    /// </summary>
    public async Task Refresh(TransactionEventArgs e)
    {
        switch (e.Type)
        {
            case TransactionEventArgs.CRUD.Create:
                Create(e);
                break;
            case TransactionEventArgs.CRUD.Read:
                await Read();
                break;
            case TransactionEventArgs.CRUD.Update:
                Update(e);
                break;
            case TransactionEventArgs.CRUD.Delete:
                Delete(e);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Adds new Transaction to [Loaded]Transactions list.
    /// </summary>
    /// <param name="e"></param>
    void Create(TransactionEventArgs e)
    {
        Transactions.Insert(0, e.Transaction);
        LoadedTransactions.Insert(0, e.Transaction);

        // add payee if new (transfers don't have a payee)
        string payee = e.Transaction.Payee;
        if (!string.IsNullOrEmpty(payee) && !Payees.Contains(payee))
            Payees.Add(payee);

    }

    /// <summary>
    /// Get transactions from db, if count has changed, refresh Transactions collection.
    /// </summary>
    async Task Read()
    {
        var transactions = await transactionService.GetTransactionsFromTo(From, To, true);
        if (transactions.Count != Transactions.Count)
        {
            // update transactions
            Transactions.Clear();
            Transactions = new(transactions);
            LoadedTransactions.Clear();
            await LoadMoreItems();

            // update payees
            var payees = transactions.Select(t => t.Payee).Distinct();
            Payees.Clear();
            foreach (var payee in payees)
                Payees.Add(payee);
        }
        if (showValue != Utilities.ShowValue)
        {
            // workaround for triggering converter
            if (LoadedTransactions.Any())
                foreach (var trans in LoadedTransactions)
                    trans.Amount = (Utilities.ShowValue) ? trans.Amount + 0.0001m : trans.Amount - 0.0001m;
        }

        showValue = Utilities.ShowValue;
    }

    /// <summary>
    /// Finds transaction via ID and update values.
    /// </summary>
    /// <param name="e"></param>
    void Update(TransactionEventArgs e)
    {
        Transaction transaction = e.Transaction;
        foreach (var trans in Transactions.Where(t => t.TransactionID == transaction.TransactionID))
        {
            // if payee has changed, update in Payees
            if (trans.Payee != transaction.Payee)
            {
                Payees.Remove(trans.Payee);
                Payees.Add(transaction.Payee);
            }

            trans.Date = transaction.Date;
            trans.AccountID = transaction.AccountID;
            trans.Amount = transaction.Amount;
            trans.CategoryID = transaction.CategoryID;
            trans.SubcategoryID = transaction.SubcategoryID;
            trans.Payee = transaction.Payee;
            trans.TransferID = transaction.TransferID;
        }
        foreach (var trans in LoadedTransactions.Where(t => t.TransactionID == transaction.TransactionID))
        {
            trans.Date = transaction.Date;
            trans.AccountID = transaction.AccountID;
            trans.Amount = transaction.Amount;
            trans.CategoryID = transaction.CategoryID;
            trans.SubcategoryID = transaction.SubcategoryID;
            trans.Payee = transaction.Payee;
            trans.TransferID = transaction.TransferID;
        }
    }

    /// <summary>
    /// Removes transaction from collection.
    /// </summary>
    /// <param name="e"></param>
    void Delete(TransactionEventArgs e)
    {
        Transaction trans = Transactions.Where(t => t.TransactionID == e.Transaction.TransactionID).FirstOrDefault();
        if (trans is not null)
        {
            Transactions.Remove(trans);
            LoadedTransactions.Remove(trans);
        }
    }

    /// <summary>
    /// Gets accounts from database.
    /// </summary>
    /// <returns></returns>
    public async Task GetAccounts()
    {
        var accounts = await accountService.GetActiveAccounts();
        Accounts.Clear();
        foreach (var account in accounts)
            Accounts.Add(account);
    }

    /// <summary>
    /// Gets all parent categories from database.
    /// </summary>
    public async Task GetParentCategories()
    {
        var categories = await categoryService.GetAllParentCategories();
        Categories.Clear();
        foreach (var category in categories)
            Categories.Add(category);
    }

    [RelayCommand]
    async Task CategoryChanged()
    {
        if (Category != null)
        {
            await GetSubcategories(Category);
            await UpdateFilter();
        }
        else
        {
            Subcategory = null;
            Subcategories.Clear();
            await UpdateFilter();
        }
    }

    /// <summary>
    /// Updates Subcategories based on selected parent Category.
    /// </summary>
    /// <param name="parentCategory"></param>
    public async Task GetSubcategories(Category parentCategory)
    {
        if (parentCategory != null)
        {
            var subcategories = await categoryService.GetSubcategories(parentCategory);
            Subcategories.Clear();
            foreach (var subcategory in subcategories)
                Subcategories.Add(subcategory);
        }
    }

    /// <summary>
    /// Calls UpdateFilter().
    /// </summary>
    [RelayCommand]
    async Task AmountDragCompleted()
    {
        await UpdateFilter();
    }

    /// <summary>
    /// Updates Transactions Filter.
    /// </summary>
    [RelayCommand]
    async Task UpdateFilter()
    {
        Transactions = await transactionService.GetFilteredTransactions(
            from, to, Account?.AccountID, AmountRangeStart, AmountRangeEnd,
            Category?.CategoryID, Subcategory?.CategoryID, Payee);
        LoadedTransactions.Clear();
        await LoadMoreItems();
    }

    /// <summary>
    /// Loads items from Transactions.
    /// </summary>
    [RelayCommand]
    async Task LoadMoreItems()
    {
        await Task.Delay(50);
        int index = LoadedTransactions.Count;
        int totalItems = Transactions.Count;
        int count = index + Constants.LOAD_COUNT >= totalItems ? totalItems - index : Constants.LOAD_COUNT;
        var transactions = Transactions.Skip(index).Take(count);
        foreach (var transaction in transactions)
            LoadedTransactions.Add(transaction);
    }
}