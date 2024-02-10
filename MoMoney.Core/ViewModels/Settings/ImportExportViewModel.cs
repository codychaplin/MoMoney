using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Exceptions;
using MoMoney.Core.Converters;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels.Settings;

public partial class ImportExportViewModel
{
    readonly IStockService stockService;
    readonly IAccountService accountService;
    readonly ICategoryService categoryService;
    readonly ITransactionService transactionService;
    readonly ILoggerService<ImportExportViewModel> logger;

    public ImportExportViewModel(ITransactionService _transactionService, IAccountService _accountService,
        ICategoryService _categoryService, IStockService _stockService, ILoggerService<ImportExportViewModel> _logger)
    {
        transactionService = _transactionService;
        accountService = _accountService;
        categoryService = _categoryService;
        stockService = _stockService;
        logger = _logger;
    }

    /// <summary>
    /// Prompts the user to open a CSV file. Valid Accounts are then added to the database.
    /// </summary>
    [RelayCommand]
    async Task ImportAccountsCSV()
    {
        try
        {
            var result = await SelectFile();
            if (result == null) return;

            List<Account> accounts = new();
            int i = 1;

            // read CSV and add each element to above list
            try
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false };
                using var sr = new StreamReader(result.FullPath);
                using var csv = new CsvReader(sr, config);
                await foreach (var account in csv.GetRecordsAsync<Account>())
                {
                    accounts.Add(account);
                    i++;
                }
            }
            catch (TypeConverterException ex)
            {
                string errorMessage = $"Account {i}: {ex.Text} is not a valid value for {ex.MemberMapData.Member.Name}";
                throw new InvalidAccountException(errorMessage);
            }

            await accountService.AddAccounts(accounts);
            string message = i == 2 ? "1 account has been added." : $"{i - 1} accounts have been added";
            _ = Shell.Current.DisplayAlert("Success", message, "OK");

            await logger.LogInfo($"Imported {accounts.Count} accounts from '{result.FileName}'.");
            logger.LogFirebaseEvent(FirebaseParameters.EVENT_IMPORT_ACCOUNTS, FirebaseParameters.GetFirebaseParameters());
        }
        catch (InvalidAccountException ex)
        {
            await logger.LogWarning(nameof(ImportAccountsCSV), ex);
            await Shell.Current.DisplayAlert("Warning", ex.Message, "OK");
        }
        catch (DuplicateAccountException ex)
        {
            await logger.LogError(nameof(ImportAccountsCSV), ex);
            await Shell.Current.DisplayAlert("Duplicate Error", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(ImportAccountsCSV), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Prompts the user to open a CSV file. Valid Categories are then added to the database.
    /// </summary>
    [RelayCommand]
    async Task ImportCategoriesCSV()
    {
        try
        {
            var result = await SelectFile();
            if (result == null) return;

            List<Category> categories = new();
            int i = 1;

            // read CSV and add each element to above list
            try
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false };
                using var sr = new StreamReader(result.FullPath);
                using var csv = new CsvReader(sr, config);
                await foreach (var category in csv.GetRecordsAsync<Category>())
                {
                    // check if parent category exists
                    string parent = category.ParentName;
                    if (!string.IsNullOrEmpty(parent))
                    {
                        var parentCat = await categoryService.GetParentCategory(parent, true);
                        if (parentCat == null && !categories.Select(c => c.CategoryName).Contains(parent))
                            throw new InvalidCategoryException($"'{parent}' is not an existing parent category.");
                    }

                    categories.Add(category);
                    i++;
                }
            }
            catch (TypeConverterException ex)
            {
                string errorMessage = $"Category {i}: {ex.Text} is not a valid value for {ex.MemberMapData.Member.Name}";
                throw new InvalidCategoryException(errorMessage);
            }

            await categoryService.AddCategories(categories);
            string message = i == 2 ? "1 category has been added." : $"{i - 1} categories have been added";
            _ = Shell.Current.DisplayAlert("Success", message, "OK");

            await logger.LogInfo($"Imported {categories.Count} categories from '{result.FileName}'.");
            logger.LogFirebaseEvent(FirebaseParameters.EVENT_IMPORT_CATEGORIES, FirebaseParameters.GetFirebaseParameters());
        }
        catch (InvalidCategoryException ex)
        {
            await logger.LogWarning(nameof(ImportCategoriesCSV), ex);
            await Shell.Current.DisplayAlert("Warning", ex.Message, "OK");
        }
        catch (DuplicateCategoryException ex)
        {
            await logger.LogError(nameof(ImportCategoriesCSV), ex);
            await Shell.Current.DisplayAlert("Duplicate Error", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(ImportCategoriesCSV), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Prompts the user to open a CSV file. Valid Transactions are then added to the database.
    /// </summary>
    [RelayCommand]
    async Task ImportTransactionsCSV()
    {
        try
        {
            var result = await SelectFile();
            if (result == null) return;

            // initial list and counter
            List<Transaction> transactions = new();
            int i = 1;

            // read CSV and add each element to above list
            try
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false };
                using var sr = new StreamReader(result.FullPath);
                using var csv = new CsvReader(sr, config);
                TransactionImportConverter.accounts = await accountService.GetAccountsAsNameDict();
                TransactionImportConverter.categories = await categoryService.GetCategoriesAsNameDict();
                csv.Context.TypeConverterCache.AddConverter<Transaction>(new TransactionImportConverter());
                csv.Context.RegisterClassMap<TransactionImportMap>();
                await foreach (var transaction in csv.GetRecordsAsync<Transaction>())
                {
                    // transfer Id
                    if (transaction.SubcategoryID == Constants.CREDIT_ID)
                    {
                        transactions[i - 2].TransferID = transaction.AccountID; // -2 because i started at 1, not 0
                        transaction.TransferID = transactions[i - 2].AccountID;
                    }

                    transactions.Add(transaction);
                    i++;
                }
            }
            catch (TypeConverterException ex)
            {
                string errorMessage = $"Transaction {i}: '{ex.Text}' is not a valid value for '{ex.MemberMapData.Member.Name}'";
                throw new InvalidTransactionException(errorMessage);
            }

            await transactionService.AddTransactions(transactions);
            await CalculateAccountBalances();
            string message = i == 2 ? "1 transaction has been added." : $"{i - 1} transactions have been added";
            _ = Shell.Current.DisplayAlert("Success", message, "OK");

            await logger.LogInfo($"Imported {transactions.Count} transactions from '{result.FileName}'.");
            logger.LogFirebaseEvent(FirebaseParameters.EVENT_IMPORT_TRANSACTIONS, FirebaseParameters.GetFirebaseParameters());
        }
        catch (InvalidTransactionException ex)
        {
            await logger.LogWarning(nameof(ImportTransactionsCSV), ex);
            await Shell.Current.DisplayAlert("Warning", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(ImportTransactionsCSV), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Prompts the user to open a CSV file. Valid Stocks are then added to the database.
    /// </summary>
    [RelayCommand]
    async Task ImportStocksCSV()
    {
        try
        {
            var result = await SelectFile();
            if (result == null) return;

            List<Stock> stocks = new();
            int i = 1;

            // read CSV and add each element to above list
            try
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false };
                using var sr = new StreamReader(result.FullPath);
                using var csv = new CsvReader(sr, config);
                await foreach (var stock in csv.GetRecordsAsync<Stock>())
                {
                    stocks.Add(stock);
                    i++;
                }
            }
            catch (TypeConverterException ex)
            {
                string errorMessage = $"Stock {i}: {ex.Text} is not a valid value for {ex.MemberMapData.Member.Name}";
                throw new InvalidStockException(errorMessage);
            }

            await stockService.AddStocks(stocks);
            string message = i == 2 ? "1 stock has been added." : $"{i - 1} stocks have been added";
            _ = Shell.Current.DisplayAlert("Success", message, "OK");

            await logger.LogInfo($"Imported {stocks.Count} stocks from '{result.FileName}'.");
            logger.LogFirebaseEvent(FirebaseParameters.EVENT_IMPORT_STOCKS, FirebaseParameters.GetFirebaseParameters());
        }
        catch (InvalidStockException ex)
        {
            await logger.LogWarning(nameof(ImportStocksCSV), ex);
            await Shell.Current.DisplayAlert("Warning", ex.Message, "OK");
        }
        catch (DuplicateStockException ex)
        {
            await logger.LogError(nameof(ImportStocksCSV), ex);
            await Shell.Current.DisplayAlert("Duplicate Error", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(ImportStocksCSV), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    async Task<FileResult> SelectFile()
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
    /// Exports Transactions from database to a CSV file.
    /// </summary>
    [RelayCommand]
    async Task ExportTransactionsCSV()
    {
        try
        {
            string name = "transactions.csv";
            string targetFile = await CheckPermissionsAndGetFilePath(name);
            if (string.IsNullOrEmpty(targetFile)) return;

            // open writer
            var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false };
            using var writer = new StreamWriter(targetFile);
            using var csv = new CsvWriter(writer, config);

            // configure converter and map
            TransactionExportConverter.accounts = accountService.Accounts;
            TransactionExportConverter.categories = categoryService.Categories;
            csv.Context.TypeConverterCache.AddConverter<Transaction>(new TransactionExportConverter());
            csv.Context.RegisterClassMap<TransactionExportMap>();

            // get transactions from db and write to csv
            var transactions = await transactionService.GetTransactions();
            csv.WriteRecords(transactions);

            // log/display success message
            int count = transactions.Count();
            string message = $"Successfully downloaded file with {count} " + (count == 1 ? "transaction" : "transactions") + $" to:\n'{targetFile}'";
            _ = Shell.Current.DisplayAlert("Success", message, "OK");

            await logger.LogInfo($"Exported {count} transactions to '{name}'.");
            logger.LogFirebaseEvent(FirebaseParameters.EVENT_EXPORT_TRANSACTIONS, FirebaseParameters.GetFirebaseParameters());
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(ExportTransactionsCSV), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Exports Accounts from database to a CSV file.
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    async Task ExportAccountsCSV()
    {
        try
        {
            string name = "accounts.csv";
            string targetFile = await CheckPermissionsAndGetFilePath(name);
            if (string.IsNullOrEmpty(targetFile)) return;

            // open writer
            var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false };
            using var writer = new StreamWriter(targetFile);
            using var csv = new CsvWriter(writer, config);

            // get accounts from db and write to csv
            var accounts = await accountService.GetAccounts();
            csv.WriteRecords(accounts);

            // log/display success message
            int count = accounts.Count();
            string message = $"Successfully downloaded file with {count} " + (count == 1 ? "account" : "accounts") + $" to:\n'{targetFile}'";
            _ = Shell.Current.DisplayAlert("Success", message, "OK");

            await logger.LogInfo($"Exported {count} accounts to '{name}'.");
            logger.LogFirebaseEvent(FirebaseParameters.EVENT_EXPORT_ACCOUNTS, FirebaseParameters.GetFirebaseParameters());
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(ExportAccountsCSV), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Exports Categories from database to a CSV file.
    /// </summary>
    [RelayCommand]
    async Task ExportCategoriesCSV()
    {
        try
        {
            string name = "categories.csv";
            string targetFile = await CheckPermissionsAndGetFilePath(name);
            if (string.IsNullOrEmpty(targetFile)) return;

            // open writer
            var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false };
            using var writer = new StreamWriter(targetFile);
            using var csv = new CsvWriter(writer, config);

            // get categories from db and write to csv
            var categories = await categoryService.GetCategories();
            csv.WriteRecords(categories);

            // log/display success message
            int count = categories.Count();
            string message = $"Successfully downloaded file with {count} " + (count == 1 ? "category" : "categories") + $" to:\n'{targetFile}'";
            _ = Shell.Current.DisplayAlert("Success", message, "OK");

            await logger.LogInfo($"Exported {count} categories to '{name}'.");
            logger.LogFirebaseEvent(FirebaseParameters.EVENT_EXPORT_CATEGORIES, FirebaseParameters.GetFirebaseParameters());
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(ExportCategoriesCSV), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Exports Logs from database to a CSV file.
    /// </summary>
    [RelayCommand]
    async Task ExportLogsCSV()
    {
        try
        {
            string name = "logs.csv";
            string targetFile = await CheckPermissionsAndGetFilePath(name);
            if (string.IsNullOrEmpty(targetFile)) return;

            // open writer
            var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false };
            using var writer = new StreamWriter(targetFile);
            using var csv = new CsvWriter(writer, config);

            // get logs from db and write to csv
            var logs = await logger.GetLogs();
            csv.WriteRecords(logs);

            // log/display success message
            int count = logs.Count();
            string message = $"Successfully downloaded file with {count} " + (count == 1 ? "log" : "logs") + $" to:\n'{targetFile}'";
            _ = Shell.Current.DisplayAlert("Success", message, "OK");

            await logger.LogInfo($"Exported {count} logs to '{name}'.");
            logger.LogFirebaseEvent(FirebaseParameters.EVENT_EXPORT_LOGS, FirebaseParameters.GetFirebaseParameters());
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(ExportLogsCSV), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    [RelayCommand]
    async Task ExportStocksCSV()
    {
        try
        {
            string name = "stocks.csv";
            string targetFile = await CheckPermissionsAndGetFilePath(name);
            if (string.IsNullOrEmpty(targetFile)) return;

            // open writer
            var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false };
            using var writer = new StreamWriter(targetFile);
            using var csv = new CsvWriter(writer, config);

            // get stocks from db and write to csv
            var stocks = await stockService.GetStocks();
            csv.WriteRecords(stocks);

            // log/display success message
            int count = stocks.Count;
            string message = $"Successfully downloaded file with {count} " + (count == 1 ? "stock" : "stocks") + $" to:\n'{targetFile}'";
            _ = Shell.Current.DisplayAlert("Success", message, "OK");

            await logger.LogInfo($"Exported {count} stocks to '{name}'.");
            logger.LogFirebaseEvent(FirebaseParameters.EVENT_EXPORT_STOCKS, FirebaseParameters.GetFirebaseParameters());
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(ExportStocksCSV), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Checks for storage permissions and returns the path to the file.
    /// </summary>
    /// <param name="name"></param>
    /// <returns>file path if permission is granted, otherwise null</returns>
    async Task<string> CheckPermissionsAndGetFilePath(string name)
    {
        // create file in documents folder
        string path = "";
#if ANDROID
        if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Tiramisu)
        {
            // check if app has storage write permissions
            if (await Permissions.CheckStatusAsync<Permissions.StorageWrite>() != PermissionStatus.Granted)
            {
                var status = await Permissions.RequestAsync<Permissions.StorageWrite>();
                if (status != PermissionStatus.Granted)
                {
                    await Shell.Current.DisplayAlert("Permission Error", "Storage permissions are required in order to save to CSV", "OK");
                    return null;
                }
            }
        }

        path = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDocuments).AbsolutePath;
#endif

        return Path.Combine(path, name);
    }

    /// <summary>
    /// Calculates the current balance of each account and updates the database.
    /// </summary>
    async Task CalculateAccountBalances()
    {
        try
        {
            var transactions = await transactionService.GetTransactions();
            var accounts = await accountService.GetAccounts();
            if (!transactions.Any() || !accounts.Any())
                return;

            // group transactions by account, sum amounts, and convert to dictionary
            var currentBalances = transactions.GroupBy(t => t.AccountID)
                                              .Select(g => new
                                              {
                                                  g.First().AccountID,
                                                  Balance = g.Sum(t => t.Amount)
                                              })
                                              .ToDictionary(a => a.AccountID, a => a.Balance);

            // get accounts that only exist in currentBalances
            var matchingAccounts = accounts.Where(a => currentBalances.ContainsKey(a.AccountID));

            // update current balance in db
            foreach (var account in matchingAccounts)
            {
                account.CurrentBalance = account.StartingBalance + currentBalances[account.AccountID];
                await accountService.UpdateAccount(account);
            }
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(CalculateAccountBalances), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }
}