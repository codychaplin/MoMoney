using System.Globalization;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Exceptions;
using MoMoney.Core.Services.Interfaces;
using MoMoney.Core.Models.Statements;

namespace MoMoney.Core.ViewModels.Settings;

public partial class ImportStatementViewModel : ObservableObject
{
    // services
    readonly IAccountService accountService;
    readonly ITransactionService transactionService;
    readonly IMappingRuleService mappingRuleService;
    readonly ICategoryService categoryService;
    readonly ILoggerService<ImportStatementViewModel> logger;

    // first section
    [ObservableProperty] ObservableCollection<BankType> bankTypes = [];
    [ObservableProperty] string? selectedBankType;
    [ObservableProperty] ObservableCollection<Account> accounts = [];
    [ObservableProperty] Account? account;

    [ObservableProperty] bool fileUploaded = false;
    [ObservableProperty] bool isCommitButtonEnabled = false;

    // Rule section
    [ObservableProperty] string ruleRegex = "";
    
    // transaction that is currently being reviewed
    [ObservableProperty] string progressText = "";
    [ObservableProperty] int selectedTransactionIndex = -1;
    partial void OnSelectedTransactionIndexChanged(int value)
    {
        ProgressText = $"{value + 1}/{uploadedRecords.Count}";
    }
    [ObservableProperty] TangerineTransactionPair? selectedTangerineTransactionPair;

    // source data fields
    [ObservableProperty] ObservableCollection<Category> categories = [];
    [ObservableProperty] Category? category;
    [ObservableProperty] ObservableCollection<Category> subcategories = [];
    [ObservableProperty] Category? subcategory;
    [ObservableProperty] ObservableCollection<string> payees = [];

    // progress tracking info
    [ObservableProperty] string statusMessage = string.Empty;
    List<TangerineTransactionPair> uploadedRecords = [];

    public ImportStatementViewModel(ITransactionService _transactionService, IAccountService _accountService, ICategoryService _categoryService, IMappingRuleService _mappingRuleService, ILoggerService<ImportStatementViewModel> _logger)
    {
        transactionService = _transactionService;
        accountService = _accountService;
        categoryService = _categoryService;
        mappingRuleService = _mappingRuleService;
        logger = _logger;
    }

    [ObservableProperty] bool isBusy;

    /// <summary>
    /// Gets updated data from database to be used in this page/vm.
    /// </summary>
    public async Task LoadData()
    {
        try
        {
            var accounts = await accountService.GetOrderedAccounts();
            Accounts.Clear();
            foreach (var account in accounts)
                Accounts.Add(account);

            var categories = await categoryService.GetAllCategories();
            Categories.Clear();
            foreach (var category in categories)
                Categories.Add(category);

            var payees = await transactionService.GetPayeesFromTransactions();
            Payees = new(payees);
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(LoadData), ex);
            await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }

    [RelayCommand]
    void Clear()
    {
        uploadedRecords.Clear();
        SelectedTangerineTransactionPair = null;
        SelectedTransactionIndex = -1;
        FileUploaded = false;
        IsCommitButtonEnabled = false;
        RuleRegex = string.Empty;
        ProgressText = string.Empty;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    async Task Reload()
    {
        try
        {
            await MapTransactions(false);
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(Reload), ex);
            await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Prompts the user to open a CSV file. Data is processed and the user is presented of what will be added.
    /// </summary>
    [RelayCommand]
    async Task ImportStatement()
    {
        try
        {
            await MapTransactions(true);
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(ImportStatement), ex);
            await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }

    async Task MapTransactions(bool selectFile)
    {
        if (Account == null)
        {
            await Utilities.DisplayToast("Please select an account first");
            return;
        }

        if (SelectedBankType == null || !Enum.TryParse<BankType>(SelectedBankType, out var bankType))
        {
            await Utilities.DisplayToast("Please select a bank first");
            return;
        }

        var rules = await mappingRuleService.GetRules(bankType);

        int i = 1;
        try
        {
            IsBusy = true;

            if (selectFile)
            {
                // read CSV and add each element to appropriate lists
                var result = await SelectFile();
                if (result == null)
                    return;

                var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true };
                using var sr = new StreamReader(result.FullPath);
                using var csv = new CsvReader(sr, config);
                await foreach (var record in csv.GetRecordsAsync<Tangerine>())
                {
                    TangerineTransactionPair newRecord = new(record);
                    uploadedRecords.Add(newRecord);
                    TryMapTransaction(rules, newRecord, Account.AccountID);
                    i++;
                }
            }
            else
            {
                // run on already uploaded records
                foreach (var record in uploadedRecords)
                {
                    TryMapTransaction(rules, record, Account.AccountID);
                    i++;
                }
            }
        }
        catch (TypeConverterException ex)
        {
            string errorMessage = $"Transaction {i}: '{ex.Text}' is not a valid value for '{ex.MemberMapData.Member?.Name}'";
            throw new InvalidStatementException(errorMessage);
        }
        finally
        {
            IsBusy = false;
        }
        
        UpdateProgressMessage();

        if (uploadedRecords.Count > 0)
        {
            SelectedTransactionIndex = 0;
            SelectedTangerineTransactionPair = uploadedRecords[SelectedTransactionIndex];
            FileUploaded = true;
        }
    }

    /// <summary>
    /// Prompts user to select a CSV file and returns the result.
    /// </summary>
    /// <returns>FileResult</returns>
    /// <exception cref="FormatException"></exception>
    async static Task<FileResult?> SelectFile()
    {
        var options = new PickOptions { PickerTitle = "Select a .CSV file" };
        var result = await FilePicker.Default.PickAsync(options);

        if (result == null)
            return null;
        else if (!result.FileName.EndsWith("csv", StringComparison.OrdinalIgnoreCase))
            throw new FormatException("Invalid file type. Must be a CSV");

        return result;
    }

    /// <summary>
    /// Map a Tangerine record to a Transaction object, or mark it as 'needs manual validation'.
    /// </summary>
    /// <param name="rules"></param>
    /// <param name="pair"></param>
    /// <param name="accountID"></param>
    /// <returns></returns>
    void TryMapTransaction(List<MappingRule> rules, TangerineTransactionPair pair, int accountID)
    {
        // separate CSV record and transaction
        Tangerine record = pair.RawData;
        Transaction transaction = pair.Transaction;

        var date = DateTime.Parse(record.Date);
        int categoryID = -1;
        int subcategoryID = -1;
        string payee = string.Empty;
        int? transferID = null;

        // Hard rules:
        // if debit or credit, this is a transfer
        bool isTransfer = record.Transaction is "DEBIT" or "CREDIT";
        if (isTransfer)
        {
            // set cat/subcat IDs
            categoryID = Constants.TRANSFER_ID;
            subcategoryID = record.Transaction == "DEBIT" ? Constants.DEBIT_ID : Constants.CREDIT_ID;
        }

        // Soft rules:
        var searchText = $"{record.Name} {record.Memo}".ToLower();
        bool match = false;
        foreach (var rule in rules)
        {
            // if a pattern matches, check the mapping and set any applicable values
            if (Regex.IsMatch(searchText, rule.Pattern, RegexOptions.IgnoreCase))
            {
                var mapping = JsonSerializer.Deserialize<Dictionary<string, string>>(rule.MappingJson);
                if (mapping is null)
                   continue;

                // date and account are always set automatically

                if (mapping.TryGetValue("Category", out string? category) && int.TryParse(category, out int catID))
                    categoryID = catID;
                
                if (mapping.TryGetValue("Subcategory", out string? subcategory) && int.TryParse(subcategory, out int subcatID))
                    subcategoryID = subcatID;

                if (mapping.TryGetValue("Payee", out string? payeeString) && Payees.Contains(payeeString))
                    payee = payeeString;

                if (mapping.TryGetValue("TransferAccount", out string? transferAccount) && int.TryParse(transferAccount, out int transferAccountID))
                    transferID = transferAccountID;

                // once all properties are set, mark as complete and break
                if (categoryID != -1 && subcategoryID != -1 && !string.IsNullOrEmpty(payee) && (!isTransfer || transferID != null))
                {   
                    match = true;
                    break;
                }
            }
        }

        // build transaction from available rules
        transaction.SetData(date, accountID, record.Amount, categoryID, subcategoryID, payee, null);

        // if it's not complete, add to the verify list, otherwise, add it to the complete list
        if (!match)
            pair.Status = ImportStatus.NeedsVerification;
        else
            pair.Status = ImportStatus.Verified;
    }

    [RelayCommand]
    void DecrementTransaction()
    {
        if (SelectedTransactionIndex > 0)
        {
            SelectedTransactionIndex--;
            SelectedTangerineTransactionPair = uploadedRecords[SelectedTransactionIndex];   
        }
    }

    [RelayCommand]
    void IncrementTransaction()
    {
        if (SelectedTransactionIndex < uploadedRecords.Count - 1)
        {
            SelectedTransactionIndex++;
            SelectedTangerineTransactionPair = uploadedRecords[SelectedTransactionIndex];
        }
    }

    [RelayCommand]
    async Task ApproveTransaction()
    {
        if (SelectedTangerineTransactionPair == null)
            return;

        if (SelectedTangerineTransactionPair.Transaction.CategoryID <= 0)
        {
            await Utilities.DisplayToast("Please select a category first.");
            return;
        }
        if (SelectedTangerineTransactionPair.Transaction.SubcategoryID <= 0)
        {
            await Utilities.DisplayToast("Please select a subcategory first.");
            return;
        }
        if (string.IsNullOrEmpty(SelectedTangerineTransactionPair.Transaction.Payee))
        {
            await Utilities.DisplayToast("Please select a payee first.");
            return;
        }

        SelectedTangerineTransactionPair.Status = ImportStatus.ManuallyApproved;
        SelectedTangerineTransactionPair.CanEdit = false;
        UpdateProgressMessage();
    }

    void UpdateProgressMessage()
    {
        int acceptedCount = uploadedRecords.Count(x => x.Status == ImportStatus.Verified);
        int manuallyApprovedCount = uploadedRecords.Count(x => x.Status == ImportStatus.ManuallyApproved);
        int needsVerificationCount = uploadedRecords.Count(x => x.Status == ImportStatus.NeedsVerification);
        StatusMessage = $"""
        {acceptedCount} transactions ready for import.
        {manuallyApprovedCount} transactions have been manually approved.
        {needsVerificationCount} transactions need to be verified.
        """;
    }
    
    [RelayCommand]
    async Task CommitImport()
    {
        
    }
}