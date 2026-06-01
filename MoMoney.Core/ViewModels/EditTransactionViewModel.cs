using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Exceptions;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels;

[QueryProperty(nameof(ID), "ID")]
public partial class EditTransactionViewModel : BaseTransactionViewModel
{
    readonly ILoggerService<EditTransactionViewModel> logger;

    public string ID { get; set; } = string.Empty;

    [ObservableProperty] Transaction? transaction;

    public Account? InitialAccount { get; private set; }
    public Category? InitialCategory { get; private set; }
    public Category? InitialSubcategory { get; private set; }
    public Account? InitialTransferAccount { get; private set; }

    Transaction? InitialTransaction;

    public EditTransactionViewModel(ITransactionService _transactionService, IAccountService _accountService,
        ICategoryService _categoryService, ILoggerService<EditTransactionViewModel> _logger)
        : base(_transactionService, _accountService, _categoryService)
    {
        logger = _logger;
        IsCategoryEnabled = true;
        IsSubcategoryEnabled = true;
    }

    protected override async Task LogError(string method, Exception ex) => await logger.LogError(method, ex);
    protected override async Task LogWarning(string method, Exception ex) => await logger.LogWarning(method, ex);

    /// <summary>
    /// Initializes the page: loads transaction, accounts, payees, categories, and subcategories.
    /// </summary>
    public async Task Init()
    {
        await GetTransaction();
        await GetAccounts();
        await GetPayees();

        switch (InitialCategory?.CategoryID)
        {
            case Constants.INCOME_ID:
                await GetIncomeCategory();
                IsCategoryEnabled = false;
                break;
            case Constants.TRANSFER_ID:
                await GetTransferCategory();
                IsCategoryEnabled = false;
                IsSubcategoryEnabled = false;
                IsPayeeVisible = false;
                IsTransferAccountVisible = true;
                break;
            default:
                await GetExpenseCategories();
                break;
        }

        await CategoryChanged();
    }

    /// <summary>
    /// Resets input fields.
    /// </summary>
    [RelayCommand]
    void Clear()
    {
        if (Category?.CategoryID == Constants.TRANSFER_ID)
            return;

        IsCategoryEnabled = true;
        IsSubcategoryEnabled = true;

        Transaction?.Payee = string.Empty;
        Transaction = new Transaction { Date = DateTime.Now };
        Account = null;
        TransferAccount = null;
        Category = null;
        Subcategory = null;
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

                if (InitialCategory?.CategoryID >= Constants.EXPENSE_ID) // if expense, make amount appear positive for user
                    Transaction.Amount *= -1;
                else if (InitialCategory?.CategoryID == Constants.TRANSFER_ID) // if transfer, set initial payee account
                {
                    // if debit, make amount appear positive for user
                    if (InitialSubcategory?.CategoryID == Constants.DEBIT_ID)
                        Transaction.Amount *= -1;

                    if (Transaction.TransferID.HasValue)
                        InitialTransferAccount = await accountService.GetAccount(Transaction.TransferID.Value);
                }
            }
            catch (Exception ex)
            {
                await HandleError(nameof(GetTransaction), ex);
            }
        }
        else
        {
            string message = $"{ID} is not a valid ID";
            await logger.LogError(nameof(GetTransaction), new Exception(message));
            await Shell.Current.DisplayAlertAsync("Transaction ID Error", message, "OK");
        }
    }

    protected override async Task GetAccounts()
    {
        try
        {
            // TODO: if using disabled account, retrieve from db as well
            await base.GetAccounts();

            Account = InitialAccount;
            if (InitialCategory?.CategoryID == Constants.TRANSFER_ID)
                TransferAccount = InitialTransferAccount;
        }
        catch (Exception ex)
        {
            await HandleError(nameof(GetAccounts), ex);
        }
    }

    protected override async Task<Category?> GetIncomeCategory()
    {
        Category? income = null;
        try
        {
            income = await base.GetIncomeCategory();
            Category = Categories.FirstOrDefault(c => c.CategoryID == InitialCategory?.CategoryID);
        }
        catch (Exception ex)
        {
            await HandleError(nameof(GetIncomeCategory), ex);
        }

        return income;
    }

    protected override async Task<Category?> GetTransferCategory()
    {
        Category? transfer = null;
        try
        {
            transfer = await base.GetTransferCategory();
            Category = Categories.FirstOrDefault(c => c.CategoryID == InitialCategory?.CategoryID);
        }
        catch (Exception ex)
        {
            await HandleError(nameof(GetTransferCategory), ex);
        }

        return transfer;
    }

    protected override async Task GetExpenseCategories()
    {
        try
        {
            await base.GetExpenseCategories();
            Category = Categories.FirstOrDefault(c => c.CategoryID == InitialCategory?.CategoryID);
        }
        catch (Exception ex)
        {
            await HandleError(nameof(GetExpenseCategories), ex);
        }
    }

    /// <summary>
    /// Updates Subcategories based on selected parent Category.
    /// </summary>
    [RelayCommand]
    public async Task CategoryChanged()
    {
        try
        {
            await GetSubcategories();
            Subcategory = Subcategories.FirstOrDefault(s => s.CategoryID == InitialSubcategory?.CategoryID);
        }
        catch (Exception ex)
        {
            await HandleError(nameof(CategoryChanged), ex);
        }
    }

    /// <summary>
    /// Edits Transaction in database using input fields from view.
    /// </summary>
    [RelayCommand]
    async Task EditTransaction(string payee)
    {
        try
        {
            bool isValid = await Validation(payee);
            if (!isValid)
                return;

            if (Transaction is null || InitialAccount is null || InitialTransaction is null)
                return;

            await transactionService.UpdateTransaction(Transaction);

            // if account unchanged, update account balance
            if (InitialAccount.AccountID == Transaction.AccountID)
            {
                await accountService.UpdateBalance(Transaction.AccountID, Transaction.Amount - InitialTransaction.Amount);
            }
            else // if account changed, update original and new account balance
            {
                await accountService.UpdateBalance(InitialAccount.AccountID, -InitialTransaction.Amount);
                await accountService.UpdateBalance(Transaction.AccountID, Transaction.Amount);
            }

            // if transfer, update other side of transfer if it exists
            if (Transaction.CategoryID == Constants.TRANSFER_ID)
            {
                Transaction? otherTrans = await transactionService.TryGetCorrespondingTransfer(Transaction);
                if (otherTrans is not null)
                {
                    // update
                    otherTrans.Date = Transaction.Date;
                    otherTrans.AccountID = Transaction.TransferID!.Value;
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
                        await accountService.UpdateBalance(InitialTransaction.TransferID!.Value, InitialTransaction.Amount);
                        await accountService.UpdateBalance(otherTrans.AccountID, -Transaction.Amount);
                    }
                }
            }

            logger.LogFirebaseEvent(FirebaseParameters.EVENT_EDIT_TRANSACTION, FirebaseParameters.GetFirebaseParameters());
        }
        catch (Exception ex)
        {
            await HandleError(nameof(EditTransaction), ex);
        }

        await Shell.Current.GoToAsync("..");
    }

    /// <summary>
    /// Removes the Transaction from the database.
    /// </summary>
    [RelayCommand]
    async Task RemoveTransaction()
    {
        bool flag = await Shell.Current.DisplayAlertAsync("", "Are you sure you want to delete this transaction?", "Yes", "No");
        if (!flag)
            return;

        if (Transaction is null)
            return;

        try
        {
            await transactionService.RemoveTransaction(Transaction);

            if (Transaction.CategoryID == Constants.TRANSFER_ID)
            {
                // get and remove other Transaction
                Transaction? otherTrans = await transactionService.TryGetCorrespondingTransfer(Transaction);
                if (otherTrans is not null)
                    await transactionService.RemoveTransaction(otherTrans);
            }

            logger.LogFirebaseEvent(FirebaseParameters.EVENT_DELETE_TRANSACTION, FirebaseParameters.GetFirebaseParameters());
        }
        catch (Exception ex)
        {
            await HandleError(nameof(RemoveTransaction), ex);
        }

        await Shell.Current.GoToAsync("..");
    }

    async Task<bool> Validation(string payee)
    {
        if (Transaction is null || Account is null || Category is null || Subcategory is null)
        {
            await Shell.Current.DisplayAlertAsync("Validation Error", "Please fill out all fields", "OK");
            return false;
        }

        // if expense or transfer debit, convert back to negative
        if (InitialCategory?.CategoryID >= Constants.EXPENSE_ID || InitialSubcategory?.CategoryID == Constants.DEBIT_ID)
            Transaction.Amount *= -1;

        // if payee is not in Payees list, update
        if (Transaction.Payee is null && !string.IsNullOrEmpty(payee))
            Transaction.Payee = payee;

        // update transaction
        Transaction.AccountID = Account.AccountID;
        Transaction.CategoryID = Category.CategoryID;
        Transaction.SubcategoryID = Subcategory.CategoryID;
        Transaction.TransferID = TransferAccount?.AccountID;

        // if nothing has changed, don't update
        if (Transaction == InitialTransaction!)
        {
            await Shell.Current.GoToAsync("..");
            return false;
        }

        // if account and transfer account are same, don't update
        if (Transaction.AccountID == Transaction.TransferID)
        {
            await Shell.Current.DisplayAlertAsync("Error", "Cannot transfer to and from the same Account", "OK");
            return false;
        }

        return true;
    }
}