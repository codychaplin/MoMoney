using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels;

public partial class TransactionsViewModel : BaseCategoryViewModel
{
    enum FilterType
    {
        AmountStart,
        AmountEnd,
        Payee
    }

    readonly ILoggerService<TransactionsViewModel> logger;

    [ObservableProperty] ObservableCollection<Transaction> loadedTransactions = [];

    public string PageTitle => $"Transactions - {LoadedTransactions.Count}/{Transactions.Count}";

    void NotifyPageTitle() => OnPropertyChanged(nameof(PageTitle));

    [ObservableProperty] decimal? amountRangeStart = null;
    async partial void OnAmountRangeStartChanged(decimal? value) => await UpdateFilterDebounced(FilterType.AmountStart, value);
    [ObservableProperty] decimal? amountRangeEnd = null;
    async partial void OnAmountRangeEndChanged(decimal? value) => await UpdateFilterDebounced(FilterType.AmountEnd, value);

    CancellationTokenSource amountDebounce = new();

    [ObservableProperty] DateTime from;
    async partial void OnFromChanged(DateTime value) => await UpdateFilter();
    [ObservableProperty] DateTime to;
    async partial void OnToChanged(DateTime value) => await UpdateFilter();

    protected override async Task OnPayeeChangedCore(string value) => await UpdateFilterDebounced(FilterType.Payee, value);

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

        await UpdateFilter();
        NotifyPageTitle();
    }

    /// <summary>
    /// Handles transaction create event: Add payee to list if new.
    /// </summary>
    void Create(TransactionEventArgs e)
    {
        if (e.Transaction is null)
            return;

        // add payee if new (transfers don't have a payee)
        string payee = e.Transaction.Payee;
        if (!string.IsNullOrEmpty(payee) && !Payees.Contains(payee))
            Payees.Add(payee);
    }

    /// <summary>
    /// Handles read event: Gets transactions from db and refresh payees.
    /// </summary>
    async Task Read()
    {
        var transactions = await transactionService.GetTransactionsFromTo(From, To);

        var payees = transactions.Select(t => t.Payee).Distinct();
        Payees.Clear();
        foreach (var payee in payees)
            Payees.Add(payee);

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
    /// Handles update event: Updates Payees list if payee changed.
    /// </summary>
    void Update(TransactionEventArgs e)
    {
        if (e.Transaction is null)
            return;

        Transaction transaction = e.Transaction;
        var existing = Transactions.FirstOrDefault(t => t.TransactionID == transaction.TransactionID);
        if (existing is not null && existing.Payee != transaction.Payee)
        {
            Payees.Remove(existing.Payee);
            if (!string.IsNullOrEmpty(transaction.Payee) && !Payees.Contains(transaction.Payee))
                Payees.Add(transaction.Payee);
        }
    }

    /// <summary>
    /// Handles delete event: Currently does nothing.
    /// </summary>
    /// <param name="e"></param>
    void Delete(TransactionEventArgs e)
    {
        if (e.Transaction is null)
            return;
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
    /// <param name="type"></param>
    /// <param name="newValue"></param>
    async Task UpdateFilterDebounced(FilterType type, object? newValue)
    {
        amountDebounce.Cancel();
        amountDebounce = new();
        var token = amountDebounce.Token;
        try
        {
            await Task.Delay(600, token);
            if (type == FilterType.AmountStart && newValue is decimal startValue)
            {
                if (newValue is not null && startValue > AmountRangeEnd)
                {
                    AmountRangeStart = null;
                    await Utilities.DisplayToast("Start amount must be less than the end amount.");
                    return;
                }
            }
            else if (type == FilterType.AmountEnd && newValue is decimal endValue)
            {
                if (newValue is not null && endValue < AmountRangeStart)
                {
                    AmountRangeEnd = null;
                    await Utilities.DisplayToast("End amount must be greater than the start amount.");
                    return;
                }
            }
            else if (type == FilterType.Payee && newValue is string payeeValue)
            {
                if (!string.IsNullOrEmpty(payeeValue) && !Payees.Contains(payeeValue))
                    return;
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
        NotifyPageTitle();
    }
}