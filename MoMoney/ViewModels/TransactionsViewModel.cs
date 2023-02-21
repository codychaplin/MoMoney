using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Syncfusion.Maui.ListView;
using MoMoney.Views;
using MoMoney.Models;
using MoMoney.Services;
using MoMoney.Exceptions;

namespace MoMoney.ViewModels;

public partial class TransactionsViewModel : BaseViewModel
{
    public List<Transaction> Transactions = new();

    [ObservableProperty]
    public ObservableCollection<Transaction> loadedTransactions = new();

    [ObservableProperty]
    public ObservableCollection<Account> accounts = new();

    [ObservableProperty]
    public ObservableCollection<Category> categories = new();

    [ObservableProperty]
    public ObservableCollection<Category> subcategories = new();

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

    public SfListView ListView { get; set; }

    bool showValue = true;

    /// <summary>
    /// Depending on CRUD operation, update Transactions collection.
    /// </summary>
    public async Task Refresh(TransactionEventArgs e)
    {
        switch (e.Type)
        {
            case TransactionEventArgs.CRUD.Create:
                {
                    Transactions.Insert(0, e.Transaction);
                    LoadedTransactions.Insert(0, e.Transaction);

                    // if transfer, add credit side too
                    if (e.Transaction.CategoryID == Constants.TRANSFER_ID)
                    {
                        try
                        {
                            var otherTrans = await TransactionService.GetTransaction(e.Transaction.TransactionID + 1);
                            Transactions.Insert(0, otherTrans);
                            LoadedTransactions.Insert(0, otherTrans);
                        }
                        catch (TransactionNotFoundException)
                        {
                            await Shell.Current.DisplayAlert("Error", "Could not find corresponding transfer", "OK");
                        }
                    }
                    break;
                }
            case TransactionEventArgs.CRUD.Read:
                {
                    // get transactions from db, if count has changed, refresh Transactions collection
                    var transactions = await TransactionService.GetTransactionsFromTo(From, To, true);
                    if (transactions.Count() != Transactions.Count)
                    {
                        LoadedTransactions.Clear();
                        Transactions.Clear();
                        Transactions = new List<Transaction>(transactions);
                    }
                    if (showValue != Constants.ShowValue)
                    {
                        // workaround to triggering converter
                        if (LoadedTransactions.Any())
                            foreach (var trans in LoadedTransactions)
                                trans.Amount = (Constants.ShowValue) ? trans.Amount + 0.0001m : trans.Amount - 0.0001m;
                    }

                    showValue = Constants.ShowValue;
                    break;
                }
            case TransactionEventArgs.CRUD.Update:
                {
                    // finds transaction via ID and update values
                    Transaction transaction = e.Transaction;
                    foreach (var trans in Transactions.Where(t => t.TransactionID == transaction.TransactionID))
                    {
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
                    break;
                }
            case TransactionEventArgs.CRUD.Delete:
                {
                    // removes transaction from collection
                    Transaction trans = Transactions.Where(t => t.TransactionID == e.Transaction.TransactionID).FirstOrDefault();
                    if (trans is not null)
                    {
                        Transactions.Remove(trans);
                        LoadedTransactions.Remove(trans);
                    }
                    break;
                }
            default:
                break;
        }
    }

    /// <summary>
    /// Gets accounts from database.
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
    /// Gets all parent categories from database.
    /// </summary>
    public async Task GetParentCategories()
    {
        var categories = await CategoryService.GetAllParentCategories();
        Categories.Clear();
        foreach (var cat in categories)
            Categories.Add(cat);
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
    /// Clears selected Account.
    /// </summary>
    [RelayCommand]
    void ClearAccount()
    {
        Account = null;
        UpdateFilter();
    }

    /// <summary>
    /// Clears selected Category.
    /// </summary>
    [RelayCommand]
    void ClearCategory()
    {
        Category = null;
        UpdateFilter();
    }

    /// <summary>
    /// Clears selected Subcategory and calls UpdateFilter().
    /// </summary>
    [RelayCommand]
    void ClearSubcategory()
    {
        Subcategory = null;
        UpdateFilter();
    }

    /// <summary>
    /// Clears selected Payee and calls UpdateFilter().
    /// </summary>
    [RelayCommand]
    void ClearPayee()
    {
        Payee = "";
        UpdateFilter();
    }

    /// <summary>
    /// Calls UpdateFilter().
    /// </summary>
    [RelayCommand]
    void ReturnPressed()
    {
        UpdateFilter();
    }

    /// <summary>
    /// Calls UpdateFilter() and brings account frame to back.
    /// </summary>
    [RelayCommand]
    void AmountDragStarted(object obj)
    {
        var frame = obj as Frame;
        frame.ZIndex = 0;
    }

    /// <summary>
    /// Calls UpdateFilter() and brings account frame to front.
    /// </summary>
    [RelayCommand]
    async Task AmountDragCompleted(object obj)
    {
        UpdateFilter();

        var frame = obj as Frame;
        await Task.Delay(200);
        frame.ZIndex = 2;
    }

    /// <summary>
    /// Updates Transactions Filter.
    /// </summary>
    public void UpdateFilter()
    {
        if (ListView.DataSource != null)
        {
            ListView.DataSource.Filter = FilterTransactions;
            ListView.DataSource.RefreshFilter();
        }
    }

    /// <summary>
    /// Checks if transaction matches filters.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
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
    /// Loads items from Transactions
    /// </summary>
    [RelayCommand]
    async void LoadMoreItems()
    {
        ListView.IsLazyLoading = true;
        await Task.Delay(10);
        var index = LoadedTransactions.Count;
        int loadCount = 50;
        var count = index + loadCount >= Transactions.Count ? Transactions.Count - index : loadCount;
        AddTransactions(index, count);
        ListView.IsLazyLoading = false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <param name="count"></param>
    void AddTransactions(int index, int count)
    {
        for (int i = index; i < index + count && i < Transactions.Count; i++)
            LoadedTransactions.Add(Transactions[i]);
    }

    /// <summary>
    /// Goes to EditTransactionPage.xaml with a Transaction ID as a parameter.
    /// </summary>
    /// <param name="ID"></param>
    [RelayCommand]
    async Task GoToEditTransaction(int ID)
    {
        await Shell.Current.GoToAsync($"{nameof(EditTransactionPage)}?ID={ID}");
    }
}
