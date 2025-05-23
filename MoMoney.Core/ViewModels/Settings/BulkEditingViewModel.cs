﻿using System.Text;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using UraniumUI.Material.Controls;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Exceptions;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels.Settings;

public partial class BulkEditingViewModel : ObservableObject
{
    readonly IAccountService accountService;
    readonly ICategoryService categoryService;
    readonly ITransactionService transactionService;
    readonly ILoggerService<BulkEditingViewModel> logger;

    [ObservableProperty] ObservableCollection<Account> accounts = [];
    [ObservableProperty] Account? findAccount;
    [ObservableProperty] Account? replaceAccount;

    [ObservableProperty] ObservableCollection<Category> categories = [];
    [ObservableProperty] Category? findCategory;
    [ObservableProperty] Category? replaceCategory;

    [ObservableProperty] ObservableCollection<Category> findSubcategories = [];
    [ObservableProperty] ObservableCollection<Category> replaceSubcategories = [];
    [ObservableProperty] Category? findSubcategory;
    [ObservableProperty] Category? replaceSubcategory;

    [ObservableProperty] ObservableCollection<string> payees = [];
    [ObservableProperty] string? findPayee;
    [ObservableProperty] string? replacePayee;
    [ObservableProperty] string info = string.Empty;

    List<Transaction> FoundTransactions { get; set; } = [];
    int TotalTransactionCount;
    int FoundTransactionCount;

    StringBuilder infoSb = new();

    public BulkEditingViewModel(IAccountService _accountService, ICategoryService _categoryService,
        ITransactionService _transactionService, ILoggerService<BulkEditingViewModel> _logger)
    {
        accountService = _accountService;
        categoryService = _categoryService;
        transactionService = _transactionService;
        logger = _logger;
    }

    public async Task LoadBulkData()
    {
        try
        {
            await GetAccounts();
            await GetCategories();
            await GetPayees();
            TotalTransactionCount = await transactionService.GetTransactionCount();
            ReplacePayee = string.Empty;
            UpdateText();
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(LoadBulkData), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Gets accounts from database and refreshes Accounts collection.
    /// </summary>
    public async Task GetAccounts()
    {
        try
        {
            var accounts = await accountService.GetOrderedAccounts();
            Accounts.Clear();
            foreach (var account in accounts)
                Accounts.Add(account);
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(GetAccounts), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Gets categories from database and refreshes Categories collection.
    /// </summary>
    public async Task GetCategories()
    {
        try
        {
            var categories = await categoryService.GetParentCategories();
            Categories.Clear();
            foreach (var category in categories)
                Categories.Add(category);
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(GetCategories), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Gets payees from transactions and refreshes Payees collection.
    /// </summary>
    public async Task GetPayees()
    {
        try
        {
            var payees = await transactionService.GetPayeesFromTransactions();
            Payees = new(payees);
            //Payees.ReplaceRange(payees);
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(GetPayees), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Gets subcategories from database and refreshes FindSubcategories collection.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="newValue"></param>
    public async void GetFindSubcategories(object? sender, object newValue)
    {
        try
        {
            FindSubcategories.Clear();
            FindSubcategory = null;

            // event might fire before SelectedItem updates in vm
            if (FindCategory is null)
            {
                if (sender is not Picker pckCategories)
                    return;
                var category = pckCategories.SelectedItem as Category;
                FindCategory = category;
                if (FindCategory is null)
                    return;
            }

            var subcategories = await categoryService.GetSubcategories(FindCategory);
            foreach (var subcategory in subcategories)
                FindSubcategories.Add(subcategory);
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(GetFindSubcategories), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Gets subcategories from database and refreshes ReplaceSubcategories collection.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="newValue"></param>
    public async void GetReplaceSubcategories(object? sender, object newValue)
    {
        try
        {
            ReplaceSubcategories.Clear();
            ReplaceSubcategory = null;

            // event might fire before SelectedItem updates in vm
            if (ReplaceCategory is null)
            {
                if (sender is not Picker pckCategories)
                    return;
                var category = pckCategories.SelectedItem as Category;
                ReplaceCategory = category;
                if (ReplaceCategory is null)
                    return;
            }

            var subcategories = await categoryService.GetSubcategories(ReplaceCategory);
            foreach (var subcategory in subcategories)
                ReplaceSubcategories.Add(subcategory);
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(GetReplaceSubcategories), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    public void Clear()
    {
        FoundTransactionCount = 0;
        UpdateText();
    }

    [RelayCommand]
    async Task<bool> BulkFind(bool fromCommand = true)
    {
        try
        {
            if (FindAccount == null && FindCategory == null && FindSubcategory == null && string.IsNullOrEmpty(FindPayee))
            {
                await Shell.Current.DisplayAlert("Warning", "Please select at least one filter", "OK");
                return false;
            }

            FoundTransactions = await transactionService.GetFilteredTransactions(accountID: FindAccount?.AccountID,
                categoryID: FindCategory?.CategoryID, subcategoryID: FindSubcategory?.CategoryID, payee: FindPayee);
            FoundTransactionCount = FoundTransactions.Count;
            UpdateText(0, true, false);

            // don't log if called from another method
            if (fromCommand)
                logger.LogFirebaseEvent(FirebaseParameters.EVENT_BULK_FIND, FirebaseParameters.GetFirebaseParameters());

            return true;
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(BulkFind), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }

        return false;
    }

    [RelayCommand]
    async Task BulkReplace()
    {
        if (FoundTransactions.Count == 0)
            if (await BulkFind(false) == false)
                return;

        bool flag = await Shell.Current.DisplayAlert("", $"Are you sure you want to replace {FoundTransactionCount} transactions", "Yes", "No");
        if (!flag)
            return;

        int i = 0;
        try
        {
            foreach (var trans in FoundTransactions)
            {
                trans.AccountID = ReplaceAccount?.AccountID ?? trans.AccountID;
                trans.CategoryID = ReplaceCategory?.CategoryID ?? trans.CategoryID;
                trans.SubcategoryID = ReplaceSubcategory?.CategoryID ?? trans.SubcategoryID;
                trans.Payee = string.IsNullOrEmpty(ReplacePayee) ? trans.Payee : ReplacePayee;
                await transactionService.UpdateTransaction(trans);
                i++;
            }

            logger.LogFirebaseEvent(FirebaseParameters.EVENT_BULK_REPLACE, FirebaseParameters.GetFirebaseParameters());
        }
        catch (InvalidTransactionException ex)
        {
            await logger.LogWarning(nameof(BulkReplace), ex);
            await Shell.Current.DisplayAlert("Validation Error", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(BulkReplace), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            UpdateText(i, true, true);
        }
    }

    void UpdateText(int replacedCount = 0, bool found = false, bool replaced = false)
    {
        infoSb.Clear();
        infoSb.Append($"Total Transactions: {TotalTransactionCount}\n");
        if (found)
            infoSb.Append($"Transactions Found: {FoundTransactionCount}\n");
        if (replaced)
            infoSb.Append($"Transactions Replaced: {replacedCount} \n");
        Info = infoSb.ToString();
    }
}