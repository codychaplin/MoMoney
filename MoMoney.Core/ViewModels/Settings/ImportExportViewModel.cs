using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Exceptions;
using MoMoney.Core.Converters;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels.Settings;

public partial class ImportExportViewModel : ObservableObject
{
    readonly IStockService stockService;
    readonly IAccountService accountService;
    readonly ICategoryService categoryService;
    readonly ITransactionService transactionService;
    readonly ILoggerService<ImportExportViewModel> logger;

    readonly IFileSaver fileSaver;

    public ImportExportViewModel(ITransactionService _transactionService, IAccountService _accountService, ICategoryService _categoryService,
        IStockService _stockService, ILoggerService<ImportExportViewModel> _logger, IFileSaver _fileSaver)
    {
        transactionService = _transactionService;
        accountService = _accountService;
        categoryService = _categoryService;
        stockService = _stockService;
        logger = _logger;
        fileSaver = _fileSaver;
    }

    [ObservableProperty]
    bool isBusy;

    /// <summary>
    /// Prompts the user to open a CSV file. Valid Accounts are then added to the database.
    /// </summary>
    [RelayCommand]
    async Task ImportAccountsCSV()
    {
        try
        {
            IsBusy = true;
            var result = await SelectFile();
            if (result == null)
                return;

            List<Account> accounts = [];
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
                string errorMessage = $"Account {i}: {ex.Text} is not a valid value for {ex.MemberMapData.Member?.Name}";
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
        finally
        {
            IsBusy = false;
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
            IsBusy = true;
            var result = await SelectFile();
            if (result == null) 
                return;

            List<Category> categories = [];
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
                string errorMessage = $"Category {i}: {ex.Text} is not a valid value for {ex.MemberMapData.Member?.Name}";
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
        finally
        {
            IsBusy = false;
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
            IsBusy = true;
            var result = await SelectFile();
            if (result == null)
                return;

            // initial list and counter
            List<Transaction> transactions = [];
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
                string errorMessage = $"Transaction {i}: '{ex.Text}' is not a valid value for '{ex.MemberMapData.Member?.Name}'";
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
        finally
        {
            IsBusy = false;
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
            IsBusy = true;
            var result = await SelectFile();
            if (result == null)
                return;

            List<Stock> stocks = [];
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
                string errorMessage = $"Stock {i}: {ex.Text} is not a valid value for {ex.MemberMapData.Member?.Name}";
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
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Prompts the user to open a CSV file. Logs are then added to the database.
    /// </summary>
    [RelayCommand]
    async Task ImportLogsCSV()
    {
        try
        {
            IsBusy = true;
            var result = await SelectFile();
            if (result == null)
                return;

            List<Log> logs = [];
            int i = 1;

            // read CSV and add each element to above list
            try
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false };
                using var sr = new StreamReader(result.FullPath);
                using var csv = new CsvReader(sr, config);
                await foreach (var log in csv.GetRecordsAsync<Log>())
                {
                    logs.Add(log);
                    i++;
                }
            }
            catch (TypeConverterException ex)
            {
                string errorMessage = $"Log {i}: {ex.Text} is not a valid value for {ex.MemberMapData.Member?.Name}";
                throw new InvalidStockException(errorMessage);
            }

            await logger.AddLogs(logs);
            string message = i == 2 ? "1 log has been added." : $"{i - 1} logs have been added";
            _ = Shell.Current.DisplayAlert("Success", message, "OK");

            await logger.LogInfo($"Imported {logs.Count} logs from '{result.FileName}'.");
            logger.LogFirebaseEvent(FirebaseParameters.EVENT_IMPORT_LOGS, FirebaseParameters.GetFirebaseParameters());
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(ImportLogsCSV), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
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
    /// Exports Transactions from database to a CSV file.
    /// </summary>
    [RelayCommand]
    async Task ExportTransactionsCSV()
    {
        try
        {
            IsBusy = true;
            var transactions = await transactionService.GetTransactions();
            await ExportData(transactions, "transactions.csv", "transaction", "transactions", FirebaseParameters.EVENT_EXPORT_TRANSACTIONS);
        }
        catch (TaskCanceledException)
        {
            // user backed out of file picker, do nothing
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(ExportTransactionsCSV), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Exports Accounts from database to a CSV file.
    /// </summary>
    [RelayCommand]
    async Task ExportAccountsCSV()
    {
        try
        {
            IsBusy = true;
            var accounts = await accountService.GetAccounts();
            await ExportData(accounts, "accounts.csv", "account", "accounts", FirebaseParameters.EVENT_EXPORT_ACCOUNTS);
        }
        catch (TaskCanceledException) { }
        catch (Exception ex)
        {
            await logger.LogError(nameof(ExportAccountsCSV), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
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
            IsBusy = true;
            var categories = await categoryService.GetCategories();
            await ExportData(categories, "categories.csv", "category", "categories", FirebaseParameters.EVENT_EXPORT_CATEGORIES);
        }
        catch (TaskCanceledException) { }
        catch (Exception ex)
        {
            await logger.LogError(nameof(ExportCategoriesCSV), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
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
            IsBusy = true;
            var logs = await logger.GetLogs();
            await ExportData(logs, "logs.csv", "log", "logs", FirebaseParameters.EVENT_EXPORT_LOGS);
        }
        catch (TaskCanceledException) { }
        catch (Exception ex)
        {
            await logger.LogError(nameof(ExportLogsCSV), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Exports Stocks from database to a CSV file.
    /// </summary>
    [RelayCommand]
    async Task ExportStocksCSV()
    {
        try
        {
            IsBusy = true;
            var stocks = await stockService.GetStocks();
            await ExportData(stocks, "stocks.csv", "stock", "stocks", FirebaseParameters.EVENT_EXPORT_STOCKS);
        }
        catch (TaskCanceledException) { }
        catch (Exception ex)
        {
            await logger.LogError(nameof(ExportStocksCSV), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Unified logic for exporting data to a CSV file.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="records"></param>
    /// <param name="name"></param>
    /// <param name="typeSingular"></param>
    /// <param name="typePlural"></param>
    /// <param name="firebaseEvent"></param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "What are you gonna do about it")]
    async Task ExportData<T>(IEnumerable<T> records, string name, string typeSingular, string typePlural, string firebaseEvent)
    {
#if ANDROID
        if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Tiramisu)
        {
            // check if app has storage write permissions
            if (await Permissions.CheckStatusAsync<Permissions.StorageWrite>() != PermissionStatus.Granted)
            {
                var status = await Permissions.RequestAsync<Permissions.StorageWrite>();
                if (status != PermissionStatus.Granted)
                {
                    await Shell.Current.DisplayAlert("Error", "Storage permissions are required in order to save to CSV", "OK");
                    return;
                }
            }
        }
#endif

        // setup CSV writer
        var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false };
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream);
        using var csv = new CsvWriter(writer, config);

        // write records to CSV
        if (typeof(T) == typeof(Transaction))
        {
            // if exporting transactions, add converters and mappings
            TransactionExportConverter.accounts = accountService.Accounts;
            TransactionExportConverter.categories = categoryService.Categories;
            csv.Context.TypeConverterCache.AddConverter<Transaction>(new TransactionExportConverter());
            csv.Context.RegisterClassMap<TransactionExportMap>();
        }
        csv.WriteRecords(records);
        writer.Flush();
        memoryStream.Position = 0;

        // save CSV file using FileSaver API
        var result = await fileSaver.SaveAsync(name, memoryStream);
        result.EnsureSuccess();

        // log/display success message
        int count = records.Count();
        string message = $"Successfully downloaded file with {count} " + (count == 1 ? typeSingular : typePlural) + $" to:\n'{result.FilePath}'";
        _ = Shell.Current.DisplayAlert("Success", message, "OK");

        await logger.LogInfo($"Exported {count} {typePlural} to '{name}'.");
        logger.LogFirebaseEvent(firebaseEvent, FirebaseParameters.GetFirebaseParameters());
    }

    /// <summary>
    /// Calculates the current balance of each account and updates the database.
    /// </summary>
    [RelayCommand]
    async Task CalculateAccountBalances()
    {
        try
        {
            await transactionService.CalculateAccountBalances();
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(CalculateAccountBalances), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }
}