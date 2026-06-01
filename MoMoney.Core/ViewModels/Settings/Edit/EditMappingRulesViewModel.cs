using System.Text.Json;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Models.Statements;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels.Settings.Edit;

public partial class EditMappingRulesViewModel : BaseCategoryViewModel
{
    readonly IMappingRuleService mappingRuleService;
    readonly ILoggerService<EditMappingRulesViewModel> logger;

    [ObservableProperty] string? selectedBankType;

    List<MappingRule> allMappingRules = [];
    [ObservableProperty] ObservableCollection<MappingRule> mappingRules = [];
    [ObservableProperty] MappingRule? selectedMappingRule;

    [ObservableProperty] bool isTransfer;
    [ObservableProperty] bool isSkipRule = false;

    public EditMappingRulesViewModel(IMappingRuleService _mappingRuleService, ICategoryService _categoryService, IAccountService _accountService, ITransactionService _transactionService, ILoggerService<EditMappingRulesViewModel> _logger)
        : base(_transactionService, _accountService, _categoryService)
    {
        mappingRuleService = _mappingRuleService;
        logger = _logger;
    }

    protected override async Task LogError(string method, Exception ex) => await logger.LogError(method, ex);

    public async Task LoadData(string? bank = null)
    {
        if (bank != null)
            SelectedBankType = bank;

        try
        {
            await GetAllParentCategories();
            await GetAccounts();
            await GetPayees();

            var rules = await mappingRuleService.GetRules();
            allMappingRules = rules;
            SetMappingRules();
        }
        catch (Exception ex)
        {
            await HandleError(nameof(LoadData), ex);
        }
    }

    public async Task ReloadRules()
    {
        var rules = await mappingRuleService.GetRules();
        allMappingRules = rules;
        SetMappingRules();
    }
    
    void SetMappingRules()
    {
        if (SelectedBankType == null || !Enum.TryParse<BankType>(SelectedBankType, out var bankType))
            return;

        var rules = allMappingRules.Where(a => a.BankType == bankType);
        
        MappingRules.Clear();
        foreach (var rule in rules)
            MappingRules.Add(rule);
    }

    [RelayCommand]
    async Task SelectedBankTypeChanged()
    {
        try
        {
            SetMappingRules();
        }
        catch (Exception ex)
        {
            await HandleError(nameof(SelectedBankTypeChanged), ex);
        }
    }

    [RelayCommand]
    async Task CategoryChanged()
    {
        if (Category is null)
        {
            Subcategories.Clear();
            Subcategory = null;
            IsTransfer = false;
            return;
        }

        IsTransfer = Category.CategoryID == Constants.TRANSFER_ID;
        
        await GetSubcategories();
    }

    async partial void OnSelectedMappingRuleChanged(MappingRule? value)
    {
        if (value == null || string.IsNullOrEmpty(value.MappingJson))
        {
            Category = null;
            Subcategory = null;
            TransferAccount = null;
            Payee = string.Empty;
            IsTransfer = false;
            IsSkipRule = false;
            return;
        }

        try
        {
            var mapping = JsonSerializer.Deserialize<Dictionary<string, string>>(value.MappingJson);
            if (mapping == null) return;

            IsSkipRule = mapping.ContainsKey("Skip") && mapping["Skip"] == "true";
            if (IsSkipRule)
            {
                Category = null;
                Subcategory = null;
                TransferAccount = null;
                Payee = string.Empty;
                IsTransfer = false;
                return;
            }

            if (mapping.TryGetValue("Category", out string? catIdStr) && int.TryParse(catIdStr, out int catId))
            {
                Category = Categories.FirstOrDefault(c => c.CategoryID == catId);
                if (Category != null)
                {
                    await CategoryChanged();
                    if (mapping.TryGetValue("Subcategory", out string? subCatIdStr) && int.TryParse(subCatIdStr, out int subCatId))
                        Subcategory = Subcategories.FirstOrDefault(c => c.CategoryID == subCatId);
                }
            }

            if (mapping.TryGetValue("TransferAccount", out string? transferAccIdStr) && int.TryParse(transferAccIdStr, out int transferAccId))
                TransferAccount = Accounts.FirstOrDefault(a => a.AccountID == transferAccId);

            if (mapping.TryGetValue("Payee", out string? payeeStr))
                Payee = payeeStr;
        }
        catch (Exception ex)
        {
            await LogError(nameof(OnSelectedMappingRuleChanged), ex);
        }
    }

    [RelayCommand]
    async Task AddRuleToList()
    {
        if (!Enum.TryParse<BankType>(SelectedBankType, out var bankType))
        {
            await Utilities.DisplayToast("Please select a bank first");
            return;
        }

        var newRule = new MappingRule("New Rule", bankType);
        MappingRules.Add(newRule);
        SelectedMappingRule = newRule;
    }

    [RelayCommand]
    async Task ClearRule()
    {
        try
        {
            if (SelectedMappingRule == null)
                return;

            SelectedMappingRule.Name = string.Empty;
            SelectedMappingRule.Pattern = string.Empty;
            SelectedMappingRule.MappingJson = string.Empty;

            Category = null;
            Subcategory = null;
            TransferAccount = null;
            Payee = string.Empty;
            IsTransfer = false;
            IsSkipRule = false;
        }
        catch (Exception ex)
        {
            await HandleError(nameof(ClearRule), ex);
        }
    }

    [RelayCommand]
    async Task InsertOrReplaceRule()
    {
        try
        {
            if (SelectedMappingRule == null)
                return;

            // add selected category/subcategory/transfer account/payee or skip status
            // transfer account can only be added for transfers, payee is used for all else
            var mapping = new Dictionary<string, string>();

            if (IsSkipRule)
            {
                mapping.Add("Skip", "true");
            }
            else
            {
                if (Category != null)
                    mapping.Add("Category", Category.CategoryID.ToString());
                if (Subcategory != null)
                    mapping.Add("Subcategory", Subcategory.CategoryID.ToString());
                if (TransferAccount != null && Category?.CategoryID == Constants.TRANSFER_ID)
                    mapping.Add("TransferAccount", TransferAccount.AccountID.ToString());
                if (!string.IsNullOrEmpty(Payee) && Category?.CategoryID != Constants.TRANSFER_ID)
                    mapping.Add("Payee", Payee);
            }

            SelectedMappingRule.MappingJson = JsonSerializer.Serialize(mapping);

            var result = await mappingRuleService.InsertOrReplaceRule(SelectedMappingRule);
            if (result > 0)
            {
                _ = Utilities.DisplayToast("Rule saved");
                var selectedId = SelectedMappingRule.Id;
                await ReloadRules();
                if (selectedId.HasValue)
                    SelectedMappingRule = MappingRules.FirstOrDefault(r => r.Id == selectedId);
            }
        }
        catch (Exception ex)
        {
            await HandleError(nameof(InsertOrReplaceRule), ex);
        }
    }

    [RelayCommand]
    async Task RemoveRule()
    {
        bool answer = await Shell.Current.DisplayAlertAsync("Delete Rule", "Are you sure you want to delete this rule?", "Yes", "No");
        if (!answer)
            return;
        try
        {

            if (SelectedMappingRule == null)
                return;

            if (!SelectedMappingRule.Id.HasValue)
            {
                MappingRules.Remove(SelectedMappingRule);
                return;
            }

            await mappingRuleService.RemoveRule(SelectedMappingRule.Id.Value);
            MappingRules.Remove(SelectedMappingRule);
        }
        catch (Exception ex)
        {
            await HandleError(nameof(RemoveRule), ex);
        }
    }
}
