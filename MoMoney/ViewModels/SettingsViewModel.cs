using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Models;
using MoMoney.Services;
using MoMoney.Exceptions;
using MoMoney.Views.Settings;
using static Java.Util.Jar.Attributes;

namespace MoMoney.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        /// <summary>
        /// Goes to AccountsPage.xaml.
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        async Task GoToAccounts()
        {
            await Shell.Current.GoToAsync(nameof(AccountsPage));
        }

        /// <summary>
        /// Goes to CategoriesPage.xaml.
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        async Task GoToCategories()
        {
            await Shell.Current.GoToAsync(nameof(CategoriesPage));
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

                            if (!Enum.TryParse(typeof(Constants.AccountTypes),accountInfo[1], true, out var type))
                                throw new InvalidAccountException("'" + accountInfo[1] + "' is not a valid account type");

                            if (!decimal.TryParse(accountInfo[2], out decimal startingBalance))
                                throw new InvalidAccountException("'" + accountInfo[2] + "' is not a number");

                            Account account = new()
                            {
                                AccountName = name,
                                AccountType = type.ToString(),
                                StartingBalance = startingBalance,
                                CurrentBalance = startingBalance,
                                Enabled = true
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
                            string parent = categoryInfo[0];
                            if (!string.IsNullOrEmpty(parent))
                            {
                                var parentCat = await CategoryService.GetCategory(parent);
                                if (parentCat is null)
                                    throw new InvalidCategoryException("Parent Category does not exist");
                            }

                            string name = categoryInfo[1];
                            if (string.IsNullOrEmpty(name))
                                throw new InvalidCategoryException("Category name cannot be blank");

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
                // if invalid, display error
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}