using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels;

public abstract partial class BaseTransactionViewModel : BaseCategoryViewModel
{
    [ObservableProperty] bool areFieldsEnabled = true;
    [ObservableProperty] bool isCategoryEnabled = false;
    [ObservableProperty] bool isSubcategoryEnabled = false;
    [ObservableProperty] bool isPayeeVisible = true;
    [ObservableProperty] bool isTransferAccountVisible = false;

    protected BaseTransactionViewModel(ITransactionService transactionService, IAccountService accountService, ICategoryService categoryService)
        : base(transactionService, accountService, categoryService) { }

    protected virtual async Task<Category?> GetIncomeCategory()
    {
        var income = await categoryService.GetCategory(Constants.INCOME_ID);
        Categories.Clear();
        Subcategories.Clear();
        if (income != null)
            Categories.Add(income);

        return income;
    }

    protected virtual async Task<Category?> GetTransferCategory()
    {
        var transfer = await categoryService.GetCategory(Constants.TRANSFER_ID);
        Categories.Clear();
        Subcategories.Clear();
        if (transfer != null)
            Categories.Add(transfer);

        return transfer;
    }

    protected virtual async Task GetExpenseCategories()
    {
        var categories = await categoryService.GetExpenseCategories();
        Categories.Clear();
        foreach (var category in categories)
            Categories.Add(category);
        Subcategories.Clear();
    }

    protected abstract override Task LogError(string method, Exception ex);
    protected abstract override Task LogWarning(string method, Exception ex);
}
