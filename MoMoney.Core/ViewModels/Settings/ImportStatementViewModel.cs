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
    
    // transaction that is currently being reviewed
    [ObservableProperty] string progressText = "";
    [ObservableProperty] int selectedTransactionIndex = -1;
    partial void OnSelectedTransactionIndexChanged(int value) => UpdateProgressText(value);
    [ObservableProperty] RecordTransactionPair? selectedRecord;

    // source data fields
    [ObservableProperty] ObservableCollection<Category> categories = [];
    [ObservableProperty] Category? category;
    [ObservableProperty] ObservableCollection<Category> subcategories = [];
    [ObservableProperty] Category? subcategory;
    [ObservableProperty] ObservableCollection<string> payees = [];
    [ObservableProperty] string payee = string.Empty;
    [ObservableProperty] Account? transferAccount;

    // progress tracking info
    List<RecordTransactionPair> uploadedRecords = [];
    [ObservableProperty] ObservableCollection<RecordTransactionPair> filteredRecords = [];

    [ObservableProperty] bool showVerified = false;
    partial void OnShowVerifiedChanged(bool value) => ApplyFilter();
    [ObservableProperty] string verifiedCount = string.Empty;

    [ObservableProperty] bool showManuallyApproved = false;
    partial void OnShowManuallyApprovedChanged(bool value) => ApplyFilter();
    [ObservableProperty] string manuallyApprovedCount = string.Empty;

    [ObservableProperty] bool showNeedsVerification = false;
    partial void OnShowNeedsVerificationChanged(bool value) => ApplyFilter();
    [ObservableProperty] string needsVerificationCount = string.Empty;

    [ObservableProperty] bool showAutoSkipped = false;
    partial void OnShowAutoSkippedChanged(bool value) => ApplyFilter();
    [ObservableProperty] string autoSkippedCount = string.Empty;

    [ObservableProperty] bool showManuallySkipped = false;
    partial void OnShowManuallySkippedChanged(bool value) => ApplyFilter();
    [ObservableProperty] string manuallySkippedCount = string.Empty;

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

            var categories = await categoryService.GetAllParentCategories();
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
    async Task GoToEditMappingRules()
    {
        await Shell.Current.GoToAsync("EditMappingRulesPage");
    }

    [RelayCommand]
    async Task Clear(bool confirm = true)
    {
        if (confirm)
        {
            var result = await Shell.Current.DisplayAlertAsync("Clear", "Are you sure you want to clear the data?", "Yes", "No");
            if (!result)
                return;
        }

        uploadedRecords.Clear();
        SelectedRecord = null;
        SelectedTransactionIndex = -1;
        FileUploaded = false;
        IsCommitButtonEnabled = false;
        ProgressText = string.Empty;
        ShowVerified = false;
        ShowManuallyApproved = false;
        ShowNeedsVerification = false;
        ShowAutoSkipped = false;
        ShowManuallySkipped = false;
        VerifiedCount = string.Empty;
        ManuallyApprovedCount = string.Empty;
        NeedsVerificationCount = string.Empty;
        AutoSkippedCount = string.Empty;
        ManuallySkippedCount = string.Empty;
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

                await Clear(false);

                var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true };
                using var sr = new StreamReader(result.FullPath);
                using var csv = new CsvReader(sr, config);

                if (bankType == BankType.Tangerine)
                {
                    csv.Context.RegisterClassMap<TangerineClassMap>();
                    await foreach (var record in csv.GetRecordsAsync<Tangerine>())
                    {
                        RecordTransactionPair newRecord = new(record);
                        uploadedRecords.Add(newRecord);
                        TryMapTransaction(rules, newRecord, Account.AccountID);
                        i++;
                    }
                }
                else if (bankType == BankType.Wealthsimple)
                {
                    csv.Context.RegisterClassMap<WealthsimpleClassMap>();
                    await foreach (var record in csv.GetRecordsAsync<Wealthsimple>())
                    {
                        RecordTransactionPair newRecord = new(record);
                        uploadedRecords.Add(newRecord);
                        TryMapTransaction(rules, newRecord, Account.AccountID);
                        i++;
                    }
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
        
        UpdateProgress();

        if (uploadedRecords.Count > 0)
        {
            ShowVerified = false;
            ShowManuallyApproved = false;
            ShowNeedsVerification = false;
            ApplyFilter();
            FileUploaded = true;
        }
    }

    [RelayCommand]
    async Task CategoryChanged()
    {
        if (Category is null)
        {
            Subcategories.Clear();
            Subcategory = null;
            SelectedRecord?.IsTransfer = false;
            return;
        }

        SelectedRecord?.IsTransfer = Category.CategoryID == Constants.TRANSFER_ID;

        var subcategories = await categoryService.GetSubcategories(Category);
        Subcategories.Clear();
        foreach (var subcategory in subcategories)
            Subcategories.Add(subcategory);
    }

    async partial void OnSelectedRecordChanged(RecordTransactionPair? value)
    {
        if (value == null)
        {
            Category = null;
            Subcategory = null;
            Payee = string.Empty;
            TransferAccount = null;
            return;
        }

        var transaction = value.Transaction;
        Category = Categories.FirstOrDefault(c => c.CategoryID == transaction.CategoryID);
        if (Category != null)
        {
            await CategoryChanged();
            Subcategory = Subcategories.FirstOrDefault(c => c.CategoryID == transaction.SubcategoryID);
        }
        else
        {
            Subcategories.Clear();
            Subcategory = null;
        }

        Payee = transaction.Payee;
        TransferAccount = Accounts.FirstOrDefault(a => a.AccountID == transaction.TransferID);
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
    void TryMapTransaction(List<MappingRule> rules, RecordTransactionPair pair, int accountID)
    {
        // separate CSV record and transaction
        IStatement record = pair.RawData;
        Transaction transaction = pair.Transaction;

        // default values
        var date = record.Date;
        int categoryID = -1;
        int subcategoryID = -1;
        string payee = string.Empty;
        int? transferID = null;

        // tracking variables
        bool isTransfer = false;
        var searchText = record.SearchText;
        bool match = false;

        // loop through rules
        foreach (var rule in rules)
        {
            // if a pattern matches, check the mapping and set any applicable values
            if (Regex.IsMatch(searchText, rule.Pattern, RegexOptions.IgnoreCase))
            {
                var mapping = JsonSerializer.Deserialize<Dictionary<string, string>>(rule.MappingJson);
                if (mapping is null)
                   continue;

                // check for skip rule first
                if (mapping.TryGetValue("Skip", out string? skip) && skip == "true")
                {
                    pair.Status = ImportStatus.AutoSkipped;
                    return;
                }

                // date and account are always set automatically

                if (mapping.TryGetValue("Category", out string? category) && int.TryParse(category, out int catID))
                    categoryID = catID;

                if (mapping.TryGetValue("Subcategory", out string? subcategory) && int.TryParse(subcategory, out int subcatID))
                    subcategoryID = subcatID;

                if (mapping.TryGetValue("Payee", out string? payeeString) && Payees.Contains(payeeString))
                    payee = payeeString;

                if (mapping.TryGetValue("TransferAccount", out string? transferAccount) && int.TryParse(transferAccount, out int transferAccountID))
                {
                    isTransfer = true;
                    transferID = transferAccountID;
                }

                // once all properties are set, mark as complete and break
                if (categoryID != -1 && subcategoryID != -1 && ((isTransfer && transferID.HasValue) || (!isTransfer && !string.IsNullOrEmpty(payee))))
                {
                    match = true;
                    break;
                }
            }
        }

        // build transaction from available rules
        transaction.SetData(date, accountID, record.Amount, categoryID, subcategoryID, payee, transferID);

        // if it's not complete, add to the verify list, otherwise, add it to the complete list
        if (!match)
            pair.Status = ImportStatus.NeedsVerification;
        else
            pair.Status = ImportStatus.Verified;
    }

    void ApplyFilter()
    {
        bool applyFilters = (ShowVerified || ShowManuallyApproved || ShowNeedsVerification || ShowAutoSkipped || ShowManuallySkipped) &&
            (!ShowVerified || !ShowManuallyApproved || !ShowNeedsVerification || !ShowAutoSkipped || !ShowManuallySkipped);

        FilteredRecords = applyFilters
            ? [.. uploadedRecords.Where(MatchesFilter)]
            : [.. uploadedRecords];

        SelectedTransactionIndex = FilteredRecords.Count > 0 ? 0 : -1;
        SelectedRecord = SelectedTransactionIndex >= 0 ? FilteredRecords[SelectedTransactionIndex] : null;
        UpdateProgress();
        UpdateProgressText(0);
    }

    bool MatchesFilter(RecordTransactionPair pair)
    {
        return pair.Status switch
        {
            ImportStatus.Verified => ShowVerified,
            ImportStatus.ManuallyApproved => ShowManuallyApproved,
            ImportStatus.NeedsVerification => ShowNeedsVerification,
            ImportStatus.AutoSkipped => ShowAutoSkipped,
            ImportStatus.ManuallySkipped => ShowManuallySkipped,
            _ => false
        };
    }

    [RelayCommand]
    void DecrementTransaction()
    {
        if (SelectedTransactionIndex <= 0)
            return;
        
        SelectedTransactionIndex--;
        SelectedRecord = FilteredRecords[SelectedTransactionIndex];
    }

    [RelayCommand]
    void IncrementTransaction()
    {
        if (SelectedTransactionIndex >= FilteredRecords.Count - 1)
            return;
        
        SelectedTransactionIndex++;
        SelectedRecord = FilteredRecords[SelectedTransactionIndex];
    }

    [RelayCommand]
    async Task ApproveTransaction()
    {
        if (SelectedRecord == null)
            return;

        if (Category == null)
        {
            await Utilities.DisplayToast("Please select a category first.");
            return;
        }
        if (Subcategory == null)
        {
            await Utilities.DisplayToast("Please select a subcategory first.");
            return;
        }
        if (string.IsNullOrEmpty(Payee) && !SelectedRecord.IsTransfer)
        {
            await Utilities.DisplayToast("Please select a payee first.");
            return;
        }
        if (TransferAccount == null && SelectedRecord.IsTransfer)
        {
            await Utilities.DisplayToast("Please select a transfer account first.");
            return;
        }

        var transaction = SelectedRecord.Transaction;
        transaction.CategoryID = Category.CategoryID;
        transaction.SubcategoryID = Subcategory.CategoryID;
        transaction.Payee = Payee;
        transaction.TransferID = TransferAccount?.AccountID;

        SelectedRecord.Status = ImportStatus.ManuallyApproved;
        SelectedRecord.CanEdit = false;
        UpdateProgress();
    }

    [RelayCommand]
    void SkipTransaction()
    {
        if (SelectedRecord == null)
            return;

        SelectedRecord.Status = ImportStatus.ManuallySkipped;
        SelectedRecord.CanEdit = false;
        UpdateProgress();
    }

    void UpdateProgressText(int value) =>ProgressText = FilteredRecords.Count > 0 ? $"{value + 1}/{FilteredRecords.Count}" : "";

    void UpdateProgress()
    {
        int verifiedCount = uploadedRecords.Count(x => x.Status == ImportStatus.Verified);
        int manuallyApprovedCount = uploadedRecords.Count(x => x.Status == ImportStatus.ManuallyApproved);
        int needsVerificationCount = uploadedRecords.Count(x => x.Status == ImportStatus.NeedsVerification);
        int autoSkippedCount = uploadedRecords.Count(x => x.Status == ImportStatus.AutoSkipped);
        int manuallySkippedCount = uploadedRecords.Count(x => x.Status == ImportStatus.ManuallySkipped);

        VerifiedCount = $"{verifiedCount} transactions ready for import";
        ManuallyApprovedCount = $"{manuallyApprovedCount} transactions have been manually approved";
        NeedsVerificationCount = $"{needsVerificationCount} transactions need to be verified";
        AutoSkippedCount = $"{autoSkippedCount} transactions have been auto skipped";
        ManuallySkippedCount = $"{manuallySkippedCount} transactions have been manually skipped";

        IsCommitButtonEnabled = needsVerificationCount == 0;
    }
    
    [RelayCommand]
    async Task CommitImport()
    {
        try
        {
            var transactions = uploadedRecords
                .Where(x => x.Status == ImportStatus.Verified || x.Status == ImportStatus.ManuallyApproved)
                .Select(x => x.Transaction)
                .ToList();

            if (transactions.Count <= 0)
            {
                await Utilities.DisplayToast("No transactions to import");
                return;
            }

            var minDate = transactions.Min(t => t.Date);
            var maxDate = transactions.Max(t => t.Date);

            var existingTransactions = await transactionService.GetTransactionsFromTo(minDate, maxDate, false);

            var duplicates = transactions
                .Where(t => existingTransactions.Any(e =>
                    e.Date == t.Date &&
                    e.Amount == t.Amount &&
                    e.CategoryID == t.CategoryID &&
                    e.SubcategoryID == t.SubcategoryID &&
                    e.AccountID == t.AccountID &&
                    e.Payee == t.Payee))
                .ToList();

            if (duplicates.Count > 0)
            {
                var skipDuplicates = await Shell.Current.DisplayAlertAsync(
                    "Duplicate Transactions",
                    $"Found {duplicates.Count} duplicate transaction(s). Skip duplicates or add them as new transations?",
                    "Skip Duplicates",
                    "Add new");

                if (skipDuplicates)
                    transactions = [.. transactions.Where(t => !duplicates.Contains(t))];
            }

            if (transactions.Count <= 0)
            {
                await Utilities.DisplayToast("No new transactions to import");
                return;   
            }
            
            await transactionService.AddTransactions(transactions);
            await Clear();
            var msg = duplicates.Count > 0
                ? $"Imported {transactions.Count} transactions (skipped {duplicates.Count} duplicates)"
                : $"Imported {transactions.Count} transactions successfully";
            await Utilities.DisplayToast(msg);
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(CommitImport), ex);
            await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }
}