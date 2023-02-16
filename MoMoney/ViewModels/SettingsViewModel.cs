using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Models;
using MoMoney.Services;
using MoMoney.Exceptions;
using MoMoney.Views.Settings;
using Syncfusion.Maui.DataSource.Extensions;

namespace MoMoney.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
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
                    while (sr.Peek() != -1)
                    {
                        // format line
                        string row = sr.ReadLine();
                        string[] accountInfo = row.Split(',');

                        // split line into Account parameters and create new account
                        string name = accountInfo[0];
                        if (string.IsNullOrEmpty(name))
                            throw new InvalidAccountException("Account name cannot be blank");

                        if (!Enum.TryParse(typeof(Constants.AccountTypes), accountInfo[1], true, out var type))
                            throw new InvalidAccountException("'" + accountInfo[1] + "' is not a valid account type");

                        if (!decimal.TryParse(accountInfo[2], out decimal startingBalance))
                            throw new InvalidAccountException("'" + accountInfo[2] + "' is not a number");

                        if (!bool.TryParse(accountInfo[3], out bool enabled))
                            throw new InvalidAccountException("'" + accountInfo[2] + "' is not a number");

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

                    await AccountService.AddAccounts(accounts);

                    sr.Close();
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
                            // check if parent exists in db
                            var parentCat = await CategoryService.GetCategory(parent, "");
                            if (parentCat is null)
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

                    await CategoryService.AddCategories(categories);

                    sr.Close();
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

            // get data
            var transactions = await TransactionService.GetTransactions();
            transactions = transactions.OrderBy(t => t.Date)
                                       .ThenBy(t => t.TransactionID);
            foreach (var trans in transactions)
            {
                // formats transaction parameters in CSV format
                var date = trans.Date.ToString("yyyy-MM-dd");
                var account = AccountService.Accounts[trans.AccountID];
                var amount = trans.Amount;
                var category = CategoryService.Categories[trans.CategoryID];
                var subcategory = CategoryService.Categories[trans.SubcategoryID];
                var payee = trans.Payee;
                string line = $"{date},{account},{amount},{category},{subcategory},{payee}";

                // prints line to file
                await streamWriter.WriteLineAsync(line);
            }

            streamWriter.Close();
            await Shell.Current.DisplayAlert("Success", $"File has been successfully downloaded to:\n'{targetFile}'", "OK");
        }
        else
            await Shell.Current.DisplayAlert("Error", "Storage permissions are required in order to save to CSV", "OK");
    }

    /// <summary>
    /// Prompts the user to open a CSV file. Valid Categories are then added to the database.
    /// </summary>
    [RelayCommand]
    async Task CalculateAccountBalances()
    {
        var transactions = await TransactionService.GetTransactions();
        var accounts = await AccountService.GetAccounts();

        // group transactions by account, sum amounts, and convert to dictionary
        var currentBalances = transactions.GroupBy(t => t.AccountID)
                                           .Select(g => new { g.First().AccountID,
                                                              Balance = g.Sum(t => t.Amount) })
                                           .ToDictionary(a => a.AccountID, a => a.Balance);

        // update current balance in db
        foreach (var account in accounts)
        {
            account.CurrentBalance = account.StartingBalance + currentBalances[account.AccountID];
            await AccountService.UpdateAccount(account);
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
            await TransactionService.ResetTransactions();
    }

    /// <summary>
    /// Removes all Accounts from database.
    /// </summary>
    [RelayCommand]
    async Task RemoveAllAccounts()
    {
        bool flag = await Shell.Current.DisplayAlert("", "Are you sure you want to delete ALL Accounts?", "Yes", "No");

        if (flag)
            await AccountService.RemoveAllAccounts();
    }

    /// <summary>
    /// Removes all Categories from database.
    /// </summary>
    [RelayCommand]
    async Task RemoveAllCategories()
    {
        bool flag = await Shell.Current.DisplayAlert("", "Are you sure you want to delete ALL Categories?", "Yes", "No");

        if (flag)
            await CategoryService.RemoveAllCategories();
    }
}