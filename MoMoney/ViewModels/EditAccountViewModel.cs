using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoMoney.Services;
using MoMoney.Models;

namespace MoMoney.ViewModels
{
    [QueryProperty(nameof(ID), nameof(ID))]
    public partial class EditAccountViewModel : ObservableObject
    {
        public string ID { get; set; } // account ID
        int id = 0;

        [ObservableProperty]
        public Account account = new();

        public async Task GetAccount()
        {
            if (int.TryParse(ID, out id))
                Account = await AccountService.GetAccount(id);
            else
                await Shell.Current.DisplayAlert("Error", "Could not find account", "OK");
        }

        /// <summary>
        /// Edits Account in database using input fields from view
        /// </summary>
        [RelayCommand]
        async Task Edit()
        {
            if (Account is null ||
                string.IsNullOrEmpty(Account.AccountName) ||
                Account.AccountType is null)
            {
                // if invalid, display error
                await Shell.Current.DisplayAlert("Error", "Information not valid", "OK");
            }
            else
            {
                // if valid, update Account
                Account = new Account
                {
                    AccountID = id,
                    AccountName = Account.AccountName,
                    AccountType = Account.AccountType,
                    StartingBalance = Account.StartingBalance,
                    CurrentBalance = Account.StartingBalance,
                    Enabled = Account.Enabled
                };

                await AccountService.UpdateAccount(Account);
                await Shell.Current.GoToAsync("..");
            }
        }

        /// <summary>
        /// Removes the Account from the database
        /// </summary>
        [RelayCommand]
        async Task Remove()
        {
            bool flag = await Shell.Current.DisplayAlert("Error", $"Are you sure you want to delete {Account.AccountName}", "Yes", "No");

            if (flag)
            {
                await AccountService.RemoveAccount(Account.AccountID);
                await Shell.Current.GoToAsync("..");
            }
        }
    }
}
