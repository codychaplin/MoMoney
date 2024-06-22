using MvvmHelpers;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Syncfusion.Maui.ListView;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels;

public partial class TransactionsViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    readonly IAccountService accountService;
    readonly ICategoryService categoryService;
    readonly ITransactionService transactionService;

    [ObservableProperty] ObservableRangeCollection<Transaction> loadedTransactions = [];

    [ObservableProperty] ObservableRangeCollection<Account> accounts = [];
    [ObservableProperty] ObservableRangeCollection<Category> categories = [];
    [ObservableProperty] ObservableRangeCollection<Category> subcategories = [];
    [ObservableProperty] ObservableRangeCollection<string> payees = [];

    [ObservableProperty] Account account;

    [ObservableProperty] int amountRangeStart = 0;
    [ObservableProperty] int amountRangeEnd = 500;

    [ObservableProperty] Category category;
    [ObservableProperty] Category subcategory;
    [ObservableProperty] string payee = "";

    [ObservableProperty] static DateTime from = new();
    [ObservableProperty] static DateTime to = new();

    List<Transaction> Transactions = [];

    public SfListView ListView { get; set; }

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
        if (transactions.Count() != Transactions.Count)
        {
            ListView.LoadMoreOption = LoadMoreOption.Auto;

            LoadedTransactions.Clear();
            Transactions.Clear();
            Transactions = new(transactions);

            Payees.ReplaceRange(transactions.Select(t => t.Payee).Distinct());
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
        Accounts.ReplaceRange(accounts);
    }

    /// <summary>
    /// Gets all parent categories from database.
    /// </summary>
    public async Task GetParentCategories()
    {
        var categories = await categoryService.GetAllParentCategories();
        Categories.ReplaceRange(categories);
    }

    [RelayCommand]
    async Task CategoryChanged()
    {
        if (Category != null)
        {
            await GetSubcategories(Category);
            UpdateFilter();
        }
        else
        {
            Subcategory = null;
            Subcategories.Clear();
            UpdateFilter();
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
            Subcategories.ReplaceRange(subcategories);
        }
    }

    /// <summary>
    /// Calls UpdateFilter().
    /// </summary>
    [RelayCommand]
    void AmountDragCompleted()
    {
        UpdateFilter();
    }

    /// <summary>
    /// Updates Transactions Filter.
    /// </summary>
    [RelayCommand]
    void UpdateFilter()
    {
        if (ListView.DataSource != null)
        {
            ListView.LoadMoreOption = LoadMoreOption.Auto;
            ListView.DataSource.Filter = FilterTransactions;
            ListView.DataSource.RefreshFilter();
        }
    }

    /// <summary>
    /// Checks if transaction matches filters.
    /// </summary>
    /// <param name="obj"></param>
    bool FilterTransactions(object obj)
    {
        // if all are blank, show Transaction
        if (Account == null && Category == null && Subcategory == null &&
            AmountRangeStart == 0 && AmountRangeEnd == 500 && Payee == "")
            return true;

        var trans = obj as Transaction;
        var amount = Math.Abs(trans.Amount);
        var payee = Payee.ToLower();

        // if fields aren't blank and match values, show Transaction
        if (Account != null && trans.AccountID != Account.AccountID)
            return false;
        if (amount < AmountRangeStart && AmountRangeStart != 0)
            return false;
        if (amount > AmountRangeEnd && AmountRangeEnd != 500)
            return false;
        if (Category != null && trans.CategoryID != Category.CategoryID)
            return false;
        if (Subcategory != null && trans.SubcategoryID != Subcategory.CategoryID)
            return false;
        if (payee.Length > 0 && !trans.Payee.ToLower().Contains(payee))
            return false;
        
        return true;
    }

    /// <summary>
    /// Loads items from Transactions.
    /// </summary>
    [RelayCommand]
    async Task LoadMoreItems()
    {
        ListView.IsLazyLoading = true;
        await Task.Delay(250);
        int index = LoadedTransactions.Count;
        int totalItems = Transactions.Count;
        int count = index + Constants.LOAD_COUNT >= totalItems ? totalItems - index : Constants.LOAD_COUNT;
        AddTransactions(index, count);
        ListView.IsLazyLoading = false;

        // disables loading indicator
        if (count == 0)
            ListView.LoadMoreOption = LoadMoreOption.None;
    }

    /// <summary>
    /// Copies transactions from Transactions to LoadedTransactions.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="count"></param>
    void AddTransactions(int index, int count)
    {
        LoadedTransactions.AddRange(Transactions.Skip(index).Take(count));
    }
}