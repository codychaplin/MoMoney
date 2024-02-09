using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Exceptions;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels;

public partial class AddTransactionViewModel : ObservableObject
{
    readonly IAccountService accountService;
    readonly ICategoryService categoryService;
    readonly ITransactionService transactionService;
    readonly ILoggerService<AddTransactionViewModel> logger;

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

    public AddTransactionViewModel(ITransactionService _transactionService, IAccountService _accountService,
        ICategoryService _categoryService, ILoggerService<AddTransactionViewModel> _logger)
    {
        transactionService = _transactionService;
        accountService = _accountService;
        categoryService = _categoryService;
        logger = _logger;
    }

    /// <summary>
    /// Gets accounts from database and refreshes Accounts collection.
    /// </summary>
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
            // cache selected Subcategory
            var subcategory = Subcategory;

            var income = await categoryService.GetCategory(Constants.INCOME_ID);
            Categories.Clear();
            Subcategories.Clear();
            Categories.Add(income);
            Category = income;

            // re-add selected Subcategory if not null
            if (subcategory is not null)
                Subcategory = subcategory;
        }
        catch (CategoryNotFoundException ex)
        {
            await logger.LogWarning(nameof(GetIncomeCategory), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(GetIncomeCategory), ex);
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
            await logger.LogWarning(nameof(GetTransferCategory), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(GetTransferCategory), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Gets updated expense categories from database and refreshes Categories collection.
    /// </summary>
    [RelayCommand]
    async Task GetExpenseCategories()
    {
        try
        {
            // cache selected Category and Subcategory
            var savedCategory = Category;
            var savedSubcategory = Subcategory;

            var categories = await categoryService.GetExpenseCategories();
            Categories.Clear();
            foreach (var cat in categories)
                Categories.Add(cat);
            Subcategories.Clear();

            // re-add selected Category and Subcategory if not null
            if (savedCategory is not null)
                Category = savedCategory;
            if (savedSubcategory is not null)
                Subcategory = savedSubcategory;
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(GetExpenseCategories), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Updates Subcategories based on selected parent Category.
    /// </summary>
    /// <param name="parentCategory"></param>
    async Task GetSubcategories(Category parentCategory)
    {
        try
        {
            if (parentCategory is null)
                return;

            var subcategories = await categoryService.GetSubcategories(parentCategory);
            Subcategories.Clear();
            foreach (var cat in subcategories)
                Subcategories.Add(cat);
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(GetSubcategories), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    public async void GetPayees(object sender, EventArgs e)
    {
        try
        {
            var payees = await transactionService.GetPayeesFromTransactions();
            Payees = new ObservableCollection<string>(payees);
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(GetPayees), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    public async void CategoryChanged(object sender, EventArgs e)
    {
        // check if Category is null, update subcategories
        if (Category is null) return;
        await GetSubcategories(Category);

        // if transfer, auto-select "Debit"
        if (Category.CategoryID == Constants.TRANSFER_ID && Subcategories.Count > 0)
            Subcategory = Subcategories.First();
    }

    /// <summary>
    /// adds Category to database using input fields from view.
    /// </summary>
    /// <param name="payee"></param>
    [RelayCommand]
    async Task AddTransaction(string payee)
    {
        try
        {
            if (Account is null || Category is null || Subcategory is null || 
                (Category.CategoryID == Constants.TRANSFER_ID && TransferAccount is null))
                return;

            // add payee to Payees if not already in list
            if (!Payees.Contains(payee))
                Payees.Add(payee);

            if (Category.CategoryID == Constants.INCOME_ID) // income = regular
            {
                await transactionService.AddTransaction(Date, Account.AccountID, Amount,Category.CategoryID, Subcategory.CategoryID, payee, null);
            }
            else if (Category.CategoryID == Constants.TRANSFER_ID) // transfer = 2 transactions
            {
                // must cache observable properties because they reset after being added to db
                var _date = Date;
                var _accountID = Account.AccountID;
                var _amount = Amount;
                var _categoryID = Category.CategoryID;
                var _transferID = TransferAccount.AccountID;
                await transactionService.AddTransaction(_date, _accountID, -_amount, _categoryID, Constants.DEBIT_ID, string.Empty, _transferID);
                await transactionService.AddTransaction(_date, _transferID, _amount, _categoryID, Constants.CREDIT_ID, string.Empty, _accountID);
            }
            else if (Category.CategoryID >= Constants.EXPENSE_ID) // expense = negative amount
            {
                await transactionService.AddTransaction(Date, Account.AccountID, -Amount, Category.CategoryID, Subcategory.CategoryID, payee, null);
            }

            ClearAfterAdd();

            logger.LogFirebaseEvent(FirebaseParameters.EVENT_ADD_TRANSACTION, FirebaseParameters.GetFirebaseParameters());
        }
        catch (InvalidTransactionException ex)
        {
            await logger.LogWarning(nameof(AddTransaction), ex);
            await Shell.Current.DisplayAlert("Validation Error", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(AddTransaction), ex);
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