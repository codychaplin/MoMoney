using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Syncfusion.Maui.ListView;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Exceptions;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels;

public partial class TransactionsViewModel : ObservableObject
{
    readonly IAccountService accountService;
    readonly ICategoryService categoryService;
    readonly ITransactionService transactionService;
    readonly ILoggerService<TransactionsViewModel> logger;

    [ObservableProperty]
    public ObservableCollection<Transaction> loadedTransactions = new();

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
    public int amountRangeStart = 0;

    [ObservableProperty]
    public int amountRangeEnd = 500;

    [ObservableProperty]
    public Category category;

    [ObservableProperty]
    public Category subcategory;

    [ObservableProperty]
    public string payee = "";

    [ObservableProperty]
    public static DateTime from = new();

    [ObservableProperty]
    public static DateTime to = new();

    List<Transaction> Transactions = new();

    public SfListView ListView { get; set; }

    bool showValue = true;

    public TransactionsViewModel(ITransactionService _transactionService, IAccountService _accountService,
        ICategoryService _categoryService, ILoggerService<TransactionsViewModel> _logger)
    {
        transactionService = _transactionService;
        accountService = _accountService;
        categoryService = _categoryService;
        logger = _logger;

        // first two months, show 1 year, starting March show YTD
        From = (DateTime.Today.Month <= 2) ? DateTime.Today.AddYears(-1) : new(DateTime.Today.Year, 1, 1);
        To = DateTime.Today;
    }

    /// <summary>
    /// Loads data into filter pickers.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public async void Loaded(object sender, EventArgs e)
    {
        await GetAccounts();
        await GetParentCategories();
    }

    /// <summary>
    /// Depending on CRUD operation, update Transactions collection.
    /// </summary>
    public async void Refresh(object sender, TransactionEventArgs e)
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

            Payees.Clear();
            Payees = new ObservableCollection<string>(transactions.Select(t => t.Payee).Distinct());
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
        foreach (var acc in accounts)
            Accounts.Add(acc);
    }

    /// <summary>
    /// Gets all parent categories from database.
    /// </summary>
    public async Task GetParentCategories()
    {
        var categories = await categoryService.GetAllParentCategories();
        Categories.Clear();
        foreach (var cat in categories)
            Categories.Add(cat);
    }

    public async void CategoryChanged(object sender, EventArgs e)
    {
        if (Category != null)
        {
            await GetSubcategories(Category);
            UpdateFilter(sender, e);
        }
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
    }

    /// <summary>
    /// Clears selected Account.
    /// </summary>
    [RelayCommand]
    void ClearAccount()
    {
        Account = null;
        UpdateFilter(this, default);
    }

    /// <summary>
    /// Clears selected Category.
    /// </summary>
    [RelayCommand]
    void ClearCategory()
    {
        Category = null;
        Subcategory = null;
        UpdateFilter(this, default);
    }

    /// <summary>
    /// Clears selected Subcategory and calls UpdateFilter().
    /// </summary>
    [RelayCommand]
    void ClearSubcategory()
    {
        Subcategory = null;
        UpdateFilter(this, default);
    }

    /// <summary>
    /// Calls UpdateFilter() and brings slider to front.
    /// </summary>
    [RelayCommand]
    void AmountDragStarted(object obj)
    {
        var frame = obj as Frame;
        frame.ZIndex = 3;
    }

    /// <summary>
    /// Calls UpdateFilter() and brings slider to back.
    /// </summary>
    [RelayCommand]
    async Task AmountDragCompleted(object obj)
    {
        UpdateFilter(this, default);

        var frame = obj as Frame;
        await Task.Delay(300);
        frame.ZIndex = 1;
    }

    /// <summary>
    /// Updates Transactions Filter.
    /// </summary>
    public void UpdateFilter(object sender, EventArgs e)
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
        for (int i = index; i < index + count && i < Transactions.Count; i++)
            LoadedTransactions.Add(Transactions[i]);
    }
}