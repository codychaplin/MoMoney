using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels;

public partial class TransactionsViewModel : BaseCategoryViewModel
{
    readonly ILoggerService<TransactionsViewModel> logger;

    [ObservableProperty] ObservableCollection<Transaction> loadedTransactions = [];

    [ObservableProperty] decimal? amountRangeStart = null;
    async partial void OnAmountRangeStartChanged(decimal? value) => await UpdateFilterDebounced(true, value);
    [ObservableProperty] decimal? amountRangeEnd = null;
    async partial void OnAmountRangeEndChanged(decimal? value) => await UpdateFilterDebounced(false, value);

    CancellationTokenSource amountDebounce = new();

    [ObservableProperty] DateTime from;
    async partial void OnFromChanged(DateTime value) => await UpdateFilter();
    [ObservableProperty] DateTime to;
    async partial void OnToChanged(DateTime value) => await UpdateFilter();

    List<Transaction> Transactions = [];

    bool showValue = true;

    public TransactionsViewModel(ITransactionService _transactionService, IAccountService _accountService,
        ICategoryService _categoryService, ILoggerService<TransactionsViewModel> _logger)
        : base(_transactionService, _accountService, _categoryService)
    {
        logger = _logger;
        From = new(DateTime.Today.Year, 1, 1);
    }

    protected override async Task LogError(string method, Exception ex) => await logger.LogError(method, ex);

    /// <summary>
    /// Loads data into filter pickers.
    /// </summary>
    public async Task Load()
    {
        To = DateTime.Today.AddDays(-1); // workaround until https://github.com/enisn/UraniumUI/pull/996 is fixed
        await GetAccounts();
        await GetAllParentCategories();
        await GetPayees();
        To = DateTime.Today;
        await Refresh(new(null, TransactionEventArgs.CRUD.Read));
    }

    /// <summary>
    /// Depending on CRUD operation, update Transactions collection.
    /// </summary>
    public async Task Refresh(TransactionEventArgs? e)
    {
        if (e is null)
            return;

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
        if (e.Transaction is null)
            return;

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
        var transactions = await transactionService.GetTransactionsFromTo(From, To);
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
                    trans.Amount = Utilities.ShowValue ? trans.Amount + 0.0001m : trans.Amount - 0.0001m;
        }

        showValue = Utilities.ShowValue;
    }

    /// <summary>
    /// Finds transaction via ID and update values.
    /// </summary>
    /// <param name="e"></param>
    void Update(TransactionEventArgs e)
    {
        if (e.Transaction is null)
            return;

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
        Transaction? trans = Transactions.FirstOrDefault(t => t.TransactionID == e.Transaction?.TransactionID);
        if (trans is not null)
        {
            Transactions.Remove(trans);
            LoadedTransactions.Remove(trans);
        }
    }

    [RelayCommand]
    async Task CategoryChanged()
    {
        if (Category is null)
        {
            Subcategory = null;
            Subcategories.Clear();
        }
        else
        {
            await GetSubcategories();
        }
        
        await UpdateFilter();
    }

    /// <summary>
    /// Updates Transactions Filter.
    /// </summary>
    [RelayCommand]
    async Task UpdateFilter()
    {
        Transactions = await transactionService.GetFilteredTransactions(
            From, To, Account?.AccountID, AmountRangeStart, AmountRangeEnd,
            Category?.CategoryID, Subcategory?.CategoryID, Payee);
        LoadedTransactions.Clear();
        await LoadMoreItems();
    }

    /// <summary>
    /// Updates Transactions Filter after debounce.
    /// </summary>
    /// <param name="isStart"></param>
    /// <param name="newValue"></param>
    async Task UpdateFilterDebounced(bool isStart, decimal? newValue)
    {
        amountDebounce.Cancel();
        amountDebounce = new();
        var token = amountDebounce.Token;
        try
        {
            await Task.Delay(600, token);
            if (isStart)
            {
                if (newValue is not null && newValue > AmountRangeEnd)
                {
                    AmountRangeStart = null;
                    await Utilities.DisplayToast("Start amount must be less than the end amount.");
                    return;
                }
            }
            else
            {
                if (newValue is not null && newValue < AmountRangeStart)
                {
                    AmountRangeEnd = null;
                    await Utilities.DisplayToast("End amount must be greater than the start amount.");
                    return;
                }
            }
            await UpdateFilter();
        }
        catch (OperationCanceledException) { }
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