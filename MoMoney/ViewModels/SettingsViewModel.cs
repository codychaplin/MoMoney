using SQLite;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Syncfusion.Maui.DataSource.Extensions;
using MoMoney.Views;
using MoMoney.Models;
using MoMoney.Services;
using MoMoney.Exceptions;
using MoMoney.Views.Settings;

namespace MoMoney.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    readonly IStockService stockService;
    readonly IAccountService accountService;
    readonly ICategoryService categoryService;
    readonly ITransactionService transactionService;

    public SettingsViewModel(ITransactionService _transactionService, IAccountService _accountService,
        ICategoryService _categoryService, IStockService _stockService)
    {
        transactionService = _transactionService;
        accountService = _accountService;
        categoryService = _categoryService;
        stockService = _stockService;
    }

    /// <summary>
    /// Goes to AccountsPage.xaml.
    /// </summary>
    [RelayCommand]
    async Task GoToAccounts()
    {
        await Shell.Current.GoToAsync(nameof(AccountsPage));
    }

    /// <summary>
    /// Goes to CategoriesPage.xaml.
    /// </summary>
    [RelayCommand]
    async Task GoToCategories()
    {
        await Shell.Current.GoToAsync(nameof(CategoriesPage));
    }

    /// <summary>
    /// Goes to StockSettingsPage.xaml.
    /// </summary>
    [RelayCommand]
    async Task GoToStocks()
    {
        await Shell.Current.GoToAsync(nameof(StockSettingsPage));
    }

    /// <summary>
    /// Prompts the user to open a CSV file. Valid Accounts are then added to the database.
    /// </summary>
    [RelayCommand]
    async Task ImportAccountsCSV()
    {
        try
        {
            var options = new PickOptions { PickerTitle = "Select a .CSV file" };
            var result = await FilePicker.Default.PickAsync(options);

            if (result != null)
            {
                if (result.FileName.EndsWith("csv", StringComparison.OrdinalIgnoreCase))
                {
                    List<Account> accounts = new();

                    using var sr = new StreamReader(result.FullPath);
                    try
                    {
                        while (sr.Peek() != -1)
                        {
                            // format line
                            string row = sr.ReadLine();
                            string[] accountInfo = row.Split(',');

                            // split line into Account parameters and create new account
                            string name = accountInfo[0];
                            if (string.IsNullOrEmpty(name))
                                throw new InvalidAccountException("Account name cannot be blank");

                            if (!Enum.TryParse(typeof(AccountType), accountInfo[1], true, out var type))
                                throw new InvalidAccountException("'" + accountInfo[1] + "' is not a valid account type");

                            if (!decimal.TryParse(accountInfo[2], out decimal startingBalance))
                                throw new InvalidAccountException("'" + accountInfo[2] + "' is not a number");

                            if (!bool.TryParse(accountInfo[3], out bool enabled))
                                throw new InvalidAccountException("'" + accountInfo[2] + "' is not a number");

                            // if valid, create new Account object and add to accounts
                            Account account = new()
                            {
                                AccountName = name,
                                AccountType = type.ToString(),
                                StartingBalance = startingBalance,
                                CurrentBalance = startingBalance,
                                Enabled = enabled
                            };
                            accounts.Add(account);
                        }

                        await accountService.AddAccounts(accounts);
                        AddTransactionPage.UpdatePage?.Invoke(null, new EventArgs()); // update accounts on AddTransactionPage
                    }
                    catch (SQLiteException ex)
                    {
                        await Shell.Current.DisplayAlert("Database Error", ex.Message, "OK");
                    }
                    catch (InvalidAccountException ex)
                    {
                        await Shell.Current.DisplayAlert("Validation Error", ex.Message, "OK");
                    }
                    catch (DuplicateAccountException ex)
                    {
                        await Shell.Current.DisplayAlert("Import Aborted", ex.Message, "OK");
                    }
                    finally
                    {
                        sr.Close(); // close stream reader regardless
                    }
                }
                else
                {
                    throw new FormatException("Invalid file type. Must be a CSV");
                }
            }
        }
        catch (Exception ex)
        {
            // if invalid, display error
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
            var options = new PickOptions { PickerTitle = "Select a .CSV file" };
            var result = await FilePicker.Default.PickAsync(options);

            if (result != null)
            {
                if (result.FileName.EndsWith("csv", StringComparison.OrdinalIgnoreCase))
                {
                    List<Category> categories = new();

                    using var sr = new StreamReader(result.FullPath);
                    try
                    {
                        while (sr.Peek() != -1)
                        {
                            // format line
                            string row = sr.ReadLine();
                            string[] categoryInfo = row.Split(',');

                            // split line into Category parameters and create new Category
                            string name = categoryInfo[1];
                            if (string.IsNullOrEmpty(name))
                                throw new InvalidCategoryException("Category name cannot be blank");

                            string parent = categoryInfo[0];
                            if (!string.IsNullOrEmpty(parent))
                            {
                                try
                                {
                                    // check if parent exists in db
                                    var parentCat = await categoryService.GetParentCategory(parent);
                                }
                                catch (CategoryNotFoundException)
                                {
                                    // if doesn't exist in db, check if exists in categories list
                                    if (!categories.Select(c => c.CategoryName).Contains(parent))
                                        throw new InvalidCategoryException("Parent Category does not exist");
                                }
                            }

                            Category category = new()
                            {
                                CategoryName = name,
                                ParentName = parent
                            };

                            categories.Add(category);
                        }

                        await categoryService.AddCategories(categories);
                    }
                    catch (InvalidCategoryException ex)
                    {
                        await Shell.Current.DisplayAlert("Validation Error", ex.Message, "OK");
                    }
                    catch (DuplicateCategoryException ex)
                    {
                        await Shell.Current.DisplayAlert("Import Aborted", ex.Message, "OK");
                    }
                    finally
                    {
                        sr.Close(); // close stream reader regardless
                    }
                }
                else
                    throw new FormatException("Invalid file type. Must be a CSV");
            }
        }
        catch (Exception ex)
        {
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
            var options = new PickOptions { PickerTitle = "Select a .CSV file" };
            var result = await FilePicker.Default.PickAsync(options);

            if (result != null)
            {
                if (result.FileName.EndsWith("csv", StringComparison.OrdinalIgnoreCase))
                {
                    List<Transaction> transactions = new();
                    var accountsDict = await accountService.GetAccountsAsNameDict();
                    var categoriesDict = await categoryService.GetCategoriesAsNameDict();

                    using var sr = new StreamReader(result.FullPath);
                    int i = 1;
                    try
                    {
                        var file = await sr.ReadToEndAsync();
                        var rows = file.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

                        foreach (var row in rows)
                        {
                            // format line
                            string[] transactionsInfo = row.Split(',');

                            if (transactionsInfo.Length != 6)
                                throw new FormatException($"Transaction {i} does not have the correct amount of fields");

                            // split line into Transaction parameters and create new Transaction

                            // date
                            if (!DateTime.TryParse(transactionsInfo[0], out var date))
                                throw new InvalidTransactionException($"Transaction {i}: Invalid date");

                            // account
                            if (!accountsDict.TryGetValue(transactionsInfo[1], out var accountID))
                                throw new InvalidAccountException($"Transaction {i}: '{transactionsInfo[1]}' is not a valid Account");

                            // amount
                            if (!decimal.TryParse(transactionsInfo[2], out decimal amount))
                                throw new InvalidTransactionException($"Transaction {i}: '{transactionsInfo[2]}' is not a valid number");

                            // category
                            if (!categoriesDict.TryGetValue($"{transactionsInfo[3]},", out var categoryID))
                                throw new InvalidCategoryException($"Transaction {i}: '{transactionsInfo[3]}' is not a valid Category");

                            // subcategory
                            if (!categoriesDict.TryGetValue($"{transactionsInfo[4]},{transactionsInfo[3]}", out var subcategoryID))
                                throw new InvalidCategoryException($"Transaction {i}: '{transactionsInfo[4]}' is not a valid Category");

                            // payee
                            string payee = "";
                            if (categoryID != Constants.TRANSFER_ID && string.IsNullOrEmpty(transactionsInfo[5]))
                                throw new InvalidTransactionException($"Transaction {i}: Payee cannot be blank");
                            else
                                payee = transactionsInfo[5].Trim();

                            // transfer ID
                            int? transferID;
                            if (subcategoryID == Constants.CREDIT_ID)
                            {
                                transactions[i - 2].TransferID = accountID; // -2 because i started at 1, not 0
                                transferID = transactions[i - 2].AccountID;
                            }
                            else
                                transferID = null;

                            Transaction transaction = new()
                            {
                                Date = date,
                                AccountID = accountID,
                                Amount = amount,
                                CategoryID = categoryID,
                                SubcategoryID = subcategoryID,
                                Payee = payee,
                                TransferID = transferID
                            };

                            transactions.Add(transaction);
                            i++;
                        }

                        await transactionService.AddTransactions(transactions);

                        // update account balances after transactions are successfully added
                        await CalculateAccountBalances();
                    }
                    catch (CategoryNotFoundException ex)
                    {
                        await Shell.Current.DisplayAlert("Category Not Found Error", $"Transaction {i}: {ex.Message}", "OK");
                    }
                    catch (AccountNotFoundException ex)
                    {
                        await Shell.Current.DisplayAlert("Account Not Found Error", $"Transaction {i}: {ex.Message}", "OK");
                    }
                    catch (Exception ex)
                    {
                        await Shell.Current.DisplayAlert("Error", $"Transaction {i}: {ex.Message}", "OK");
                    }
                    finally
                    {
                        sr.Close();
                    }
                }
                else
                    throw new FormatException("Invalid file type. Must be a CSV");
            }
        }
        catch (Exception ex)
        {
            // if invalid, display error
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Prompts the user to open a CSV file. Valid Accounts are then added to the database.
    /// </summary>
    [RelayCommand]
    async Task ImportStocksCSV()
    {
        try
        {
            var options = new PickOptions { PickerTitle = "Select a .CSV file" };
            var result = await FilePicker.Default.PickAsync(options);

            if (result != null)
            {
                if (result.FileName.EndsWith("csv", StringComparison.OrdinalIgnoreCase))
                {
                    List<Stock> stocks = new();

                    using var sr = new StreamReader(result.FullPath);
                    try
                    {
                        while (sr.Peek() != -1)
                        {
                            // format line
                            string row = sr.ReadLine();
                            string[] stockInfo = row.Split(',');

                            // split line into Account parameters and create new account
                            string symbol = stockInfo[0];
                            if (string.IsNullOrEmpty(symbol))
                                throw new InvalidStockException("Stock Symbol cannot be blank");

                            if (!int.TryParse(stockInfo[1], out int quantity))
                                throw new InvalidStockException("'" + stockInfo[1] + "' is not a number");

                            if (!decimal.TryParse(stockInfo[2], out decimal cost))
                                throw new InvalidStockException("'" + stockInfo[2] + "' is not a number");

                            if (!decimal.TryParse(stockInfo[3], out decimal marketPrice))
                                throw new InvalidStockException("'" + stockInfo[3] + "' is not a number");

                            if (!decimal.TryParse(stockInfo[4], out decimal bookValue))
                                throw new InvalidStockException("'" + stockInfo[4] + "' is not a number");

                            // if valid, create new Account object and add to accounts
                            Stock stock = new()
                            {
                                Symbol = symbol,
                                Quantity = quantity,
                                Cost = cost,
                                MarketPrice = marketPrice,
                                BookValue = bookValue
                            };
                            stocks.Add(stock);
                        }

                        await stockService.AddStocks(stocks);
                    }
                    catch (SQLiteException ex)
                    {
                        await Shell.Current.DisplayAlert("Database Error", ex.Message, "OK");
                    }
                    catch (InvalidStockException ex)
                    {
                        await Shell.Current.DisplayAlert("Validation Error", ex.Message, "OK");
                    }
                    catch (DuplicateStockException ex)
                    {
                        await Shell.Current.DisplayAlert("Import Aborted", ex.Message, "OK");
                    }
                    finally
                    {
                        sr.Close(); // close stream reader regardless
                    }
                }
                else
                {
                    throw new FormatException("Invalid file type. Must be a CSV");
                }
            }
        }
        catch (Exception ex)
        {
            // if invalid, display error
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Exports Transactions from database to a CSV file.
    /// </summary>
    [RelayCommand]
    [Obsolete]
    async Task ExportTransactionsCSV()
    {
        // check if app has storage write permissions
        var status = await Permissions.RequestAsync<Permissions.StorageWrite>();
        if (status == PermissionStatus.Granted)
        {
            // create file in downloads folder
            string path = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath;
            string targetFile = Path.Combine(path, "transactions.csv");
            using FileStream stream = File.OpenWrite(targetFile);
            using StreamWriter streamWriter = new(stream);

            try
            {
                // get data
                var transactions = await transactionService.GetTransactions();
                transactions = transactions.OrderBy(t => t.Date)
                                           .ThenBy(t => t.TransactionID);

                foreach (var trans in transactions)
                {
                    // formats transaction parameters in CSV format
                    var date = trans.Date.ToString("yyyy-MM-dd");
                    var account = accountService.Accounts[trans.AccountID].AccountName;
                    var amount = trans.Amount;
                    var category = categoryService.Categories[trans.CategoryID].CategoryName;
                    var subcategory = categoryService.Categories[trans.SubcategoryID].CategoryName;
                    var payee = trans.Payee;
                    string line = $"{date},{account},{amount},{category},{subcategory},{payee}";

                    // prints line to file
                    await streamWriter.WriteLineAsync(line);
                }

                await Shell.Current.DisplayAlert("Success", $"File has been successfully downloaded to:\n'{targetFile}'", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                // close stream regardless
                streamWriter.Close();
                stream.Close();
            }
        }
        else
            await Shell.Current.DisplayAlert("Permission Error", "Storage permissions are required in order to save to CSV", "OK");
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
            {
                return;
            }
            if (!accounts.Any())
            {
                await Shell.Current.DisplayAlert("Attention", "No Accounts in Database", "OK");
                return;
            }

            // group transactions by account, sum amounts, and convert to dictionary
            var currentBalances = transactions.GroupBy(t => t.AccountID)
                                              .Select(g => new { g.First().AccountID,
                                                                 Balance = g.Sum(t => t.Amount) })
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
            await Shell.Current.DisplayAlert("Database Error", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Removes all Transactions from database.
    /// </summary>
    [RelayCommand]
    async Task RemoveAllTransactions()
    {
        bool flag = await Shell.Current.DisplayAlert("", "Are you sure you want to delete ALL transactions?", "Yes", "No");

        if (flag)
        {
            try
            {
                await transactionService.ResetTransactions();
            }
            catch (SQLiteException ex)
            {
                await Shell.Current.DisplayAlert("Database Error", ex.Message, "OK");
            }
        }
    }

    /// <summary>
    /// Removes all Accounts from database.
    /// </summary>
    [RelayCommand]
    async Task RemoveAllAccounts()
    {
        bool flag = await Shell.Current.DisplayAlert("", "Are you sure you want to delete ALL Accounts?", "Yes", "No");

        if (flag)
        {
            try
            {
                await accountService.RemoveAllAccounts();
            }
            catch (SQLiteException ex)
            {
                await Shell.Current.DisplayAlert("Database Error", ex.Message, "OK");
            }
        }
    }

    /// <summary>
    /// Removes all Categories from database.
    /// </summary>
    [RelayCommand]
    async Task RemoveAllCategories()
    {
        bool flag = await Shell.Current.DisplayAlert("", "Are you sure you want to delete ALL Categories?", "Yes", "No");

        if (flag)
            await categoryService.RemoveAllCategories();
    }

    /// <summary>
    /// Removes all Categories from database.
    /// </summary>
    [RelayCommand]
    async Task RemoveAllStocks()
    {
        bool flag = await Shell.Current.DisplayAlert("", "Are you sure you want to delete ALL Stocks?", "Yes", "No");

        if (flag)
            await stockService.RemoveStocks();
    }
}