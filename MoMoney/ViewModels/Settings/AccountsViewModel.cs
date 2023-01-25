using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoMoney.Services;
using MoMoney.Models;
using MoMoney.Views.Settings;
using System.Collections.ObjectModel;

namespace MoMoney.ViewModels.Settings
{
    public partial class AccountsViewModel : ObservableObject
    {
        [ObservableProperty]
        public ObservableCollection<Account> accounts = new();

        /// <summary>
        /// Goes to AddAccountPage.xaml.
        /// </summary>
        [RelayCommand]
        async Task GoToAddAccount()
        {
            await Shell.Current.GoToAsync(nameof(AddAccountPage));
        }

        /// <summary>
        /// Goes to EditAccountPage.xaml with an Account ID as a parameter.
        /// </summary>
        [RelayCommand]
        async Task GoToEditAccount(int ID)
        {
            await Shell.Current.GoToAsync($"{nameof(EditAccountPage)}?ID={ID}");
        }

        /// <summary>
        /// Gets updated accounts from database, orders them, and refreshes Accounts collection.
        /// </summary>
        public async Task Refresh()
        {
            Accounts.Clear();
            var accounts = await AccountService.GetAccounts();
            accounts = accounts.OrderByDescending(a => a.Enabled)
                               .ThenBy(a => a.AccountName);
            foreach (var acc in accounts)
                Accounts.Add(acc);
        }
    }
}
