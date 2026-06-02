using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Models;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels;

public abstract partial class BaseCategoryViewModel : ObservableObject
{
    protected readonly IAccountService accountService;
    protected readonly ICategoryService categoryService;
    protected readonly ITransactionService transactionService;

    [ObservableProperty] ObservableCollection<Account> accounts = [];
    [ObservableProperty] ObservableCollection<Category> categories = [];
    [ObservableProperty] ObservableCollection<Category> subcategories = [];
    [ObservableProperty] ObservableCollection<string> payees = [];

    [ObservableProperty] Account? account;
    [ObservableProperty] Category? category;
    [ObservableProperty] Category? subcategory;
    [ObservableProperty] Account? transferAccount;
    [ObservableProperty] string payee = string.Empty;
    async partial void OnPayeeChanged(string value) => await OnPayeeChangedCore(value);

    protected BaseCategoryViewModel(ITransactionService transactionService, IAccountService accountService, ICategoryService categoryService)
    {
        this.transactionService = transactionService;
        this.accountService = accountService;
        this.categoryService = categoryService;
    }
    
    protected virtual Task OnPayeeChangedCore(string value) => Task.CompletedTask;

    protected virtual async Task GetAccounts()
    {
        var accounts = await accountService.GetActiveAccounts();
        Accounts.Clear();
        foreach (var account in accounts)
            Accounts.Add(account);
    }

    protected virtual async Task GetAllParentCategories()
    {
        var categories = await categoryService.GetAllParentCategories();
        Categories.Clear();
        foreach (var category in categories)
            Categories.Add(category);
    }

    protected virtual async Task GetSubcategories()
    {
        if (Category is null) return;
        var subcategories = await categoryService.GetSubcategories(Category);
        Subcategories.Clear();
        foreach (var subcategory in subcategories)
            Subcategories.Add(subcategory);
    }

    public async Task GetPayees()
    {
        try
        {
            var payees = await transactionService.GetPayeesFromTransactions();
            Payees = new(payees);
        }
        catch (Exception ex)
        {
            await HandleError(nameof(GetPayees), ex);
        }
    }

    protected async Task HandleError(string method, Exception ex)
    {
        await LogError(method, ex);
        await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
    }

    protected async Task HandleWarning(string method, Exception ex)
    {
        await LogWarning(method, ex);
        await Shell.Current.DisplayAlertAsync("Warning", ex.Message, "OK");
    }

    protected virtual Task LogError(string method, Exception ex) => Task.CompletedTask;
    protected virtual Task LogWarning(string method, Exception ex) => Task.CompletedTask;
}
