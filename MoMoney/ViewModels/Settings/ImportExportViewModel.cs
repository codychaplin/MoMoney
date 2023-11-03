﻿using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using SQLite;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MoMoney.Views;
using MoMoney.Models;
using MoMoney.Helpers;
using MoMoney.Services;
using MoMoney.Exceptions;
using MoMoney.Converters;

namespace MoMoney.ViewModels.Settings;

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
                string message = $"Account {i}: {ex.Text} is not a valid value for {ex.MemberMapData.Member.Name}";
                throw new InvalidAccountException(message);
            }

            // add accounts to db and update accounts on AddTransactionPage
            await accountService.AddAccounts(accounts);
            await Shell.Current.DisplayAlert("Success", $"{i} accounts have been added.", "OK");
            AddTransactionPage.UpdatePage?.Invoke(null, new EventArgs());
            await logger.LogInfo($"Imported {accounts.Count} accounts from '{result.FileName}'.");
        }
        catch (SQLiteException ex)
        {
            await logger.LogCritical(ex.Message, ex.GetType().Name);
            await Shell.Current.DisplayAlert("Database Error", ex.Message, "OK");
        }
        catch (InvalidAccountException ex)
        {
            await logger.LogWarning(ex.Message, ex.GetType().Name);
            await Shell.Current.DisplayAlert("Warning", ex.Message, "OK");
        }
        catch (DuplicateAccountException ex)
        {
            await logger.LogError(ex.Message, ex.GetType().Name);
            await Shell.Current.DisplayAlert("Duplicate Error", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await logger.LogError(ex.Message, ex.GetType().Name);
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
                string message = $"Category {i}: {ex.Text} is not a valid value for {ex.MemberMapData.Member.Name}";
                throw new InvalidCategoryException(message);
            }

            await categoryService.AddCategories(categories);
            await Shell.Current.DisplayAlert("Success", $"{i} categories have been added.", "OK");
            await logger.LogInfo($"Imported {categories.Count} categories from '{result.FileName}'.");
        }
        catch (SQLiteException ex)
        {
            await logger.LogCritical(ex.Message, ex.GetType().Name);
            await Shell.Current.DisplayAlert("Database Error", ex.Message, "OK");
        }
        catch (InvalidCategoryException ex)
        {
            await logger.LogWarning(ex.Message, ex.GetType().Name);
            await Shell.Current.DisplayAlert("Warning", ex.Message, "OK");
        }
        catch (DuplicateCategoryException ex)
        {
            await logger.LogError(ex.Message, ex.GetType().Name);
            await Shell.Current.DisplayAlert("Duplicate Error", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await logger.LogError(ex.Message, ex.GetType().Name);
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
                string message = $"Transaction {i}: '{ex.Text}' is not a valid value for '{ex.MemberMapData.Member.Name}'";
                throw new InvalidTransactionException(message);
            }

            await transactionService.AddTransactions(transactions);
            await CalculateAccountBalances();
            await Shell.Current.DisplayAlert("Success", $"{i} transactions have been added.", "OK");
            await logger.LogInfo($"Imported {transactions.Count} transactions from '{result.FileName}'.");
        }
        catch (SQLiteException ex)
        {
            await logger.LogCritical(ex.Message, ex.GetType().Name);
            await Shell.Current.DisplayAlert("Database Error", ex.Message, "OK");
        }
        catch (InvalidTransactionException ex)
        {
            await logger.LogWarning(ex.Message, ex.GetType().Name);
            await Shell.Current.DisplayAlert("Warning", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await logger.LogError(ex.Message, ex.GetType().Name);
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
                string message = $"Stock {i}: {ex.Text} is not a valid value for {ex.MemberMapData.Member.Name}";
                throw new InvalidStockException(message);
            }

            await stockService.AddStocks(stocks);
            await Shell.Current.DisplayAlert("Success", $"{i} stocks have been added.", "OK");
            await logger.LogInfo($"Imported {stocks.Count} stocks from '{result.FileName}'.");
        }
        catch (SQLiteException ex)
        {
            await logger.LogCritical(ex.Message, ex.GetType().Name);
            await Shell.Current.DisplayAlert("Database Error", ex.Message, "OK");
        }
        catch (InvalidStockException ex)
        {
            await logger.LogWarning(ex.Message, ex.GetType().Name);
            await Shell.Current.DisplayAlert("Warning", ex.Message, "OK");
        }
        catch (DuplicateStockException ex)
        {
            await logger.LogError(ex.Message, ex.GetType().Name);
            await Shell.Current.DisplayAlert("Duplicate Error", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await logger.LogError(ex.Message, ex.GetType().Name);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    async Task<FileResult> SelectFile()
    {
        var options = new PickOptions { PickerTitle = "Select a .CSV file" };
        var result = await FilePicker.Default.PickAsync(options);

        if (result == null)
            throw new ArgumentException("File not valid.");

        if (!result.FileName.EndsWith("csv", StringComparison.OrdinalIgnoreCase))
            throw new FormatException("Invalid file type. Must be a CSV");

        return result;
    }

    /// <summary>
    /// Exports Transactions from database to a CSV file.
    /// </summary>
    [RelayCommand]
    [Obsolete]
    async Task ExportTransactionsCSV()
    {
        try
        {
            // check if app has storage write permissions
            var status = await Permissions.RequestAsync<Permissions.StorageWrite>();
            if (status != PermissionStatus.Granted)
            {
                await Shell.Current.DisplayAlert("Permission Error", "Storage permissions are required in order to save to CSV", "OK");
                return;
            }

            // create file in downloads folder
            string path = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath;
            string name = "transactions.csv";
            string targetFile = Path.Combine(path, name);

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
            await logger.LogInfo($"Exported {count} transactions to '{name}'.");
            string message = $"Successfully downloaded file with {count} transactions to:\n'{targetFile}'";
            await Shell.Current.DisplayAlert("Success", message, "OK");
        }
        catch (Exception ex)
        {
            await logger.LogError(ex.Message, ex.GetType().Name);
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
            // check if app has storage write permissions
            var status = await Permissions.RequestAsync<Permissions.StorageWrite>();
            if (status != PermissionStatus.Granted)
            {
                await Shell.Current.DisplayAlert("Permission Error", "Storage permissions are required in order to save to CSV", "OK");
                return;
            }

            // create file in downloads folder
            string path = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath;
            string name = "logs.csv";
            string targetFile = Path.Combine(path, name);

            // open writer
            var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false };
            using var writer = new StreamWriter(targetFile);
            using var csv = new CsvWriter(writer, config);

            // get transactions from db and write to csv
            var logs = await logger.GetLogs();
            csv.WriteRecords(logs);

            // log/display success message
            int count = logs.Count();
            await logger.LogInfo($"Exported {count} logs to '{name}'.");
            string message = $"Successfully downloaded file with {count} logs to:\n'{targetFile}'";
            await Shell.Current.DisplayAlert("Success", message, "OK");
        }
        catch (Exception ex)
        {
            await logger.LogError(ex.Message, ex.GetType().Name);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Prompts the user to open a CSV file. Valid Categories are then added to the database.
    /// </summary>
    [RelayCommand]
    async Task CalculateAccountBalances()
    {
        try
        {
            var transactions = await transactionService.GetTransactions();
            var accounts = await accountService.GetAccounts();
            if (!transactions.Any())
                return;
            
            if (!accounts.Any())
                return;

            // group transactions by account, sum amounts, and convert to dictionary
            var currentBalances = transactions.GroupBy(t => t.AccountID)
                                              .Select(g => new {
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
        catch (SQLiteException ex)
        {
            await logger.LogCritical(ex.Message, ex.GetType().Name);
            await Shell.Current.DisplayAlert("Database Error", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await logger.LogError(ex.Message, ex.GetType().Name);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }
}