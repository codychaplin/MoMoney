using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoMoney.Services;
using MoMoney.Models;
using MoMoney.Views;
using System.Collections.ObjectModel;

namespace MoMoney.ViewModels
{
    public partial class AccountsViewModel : ObservableObject
    {
        [ObservableProperty]
        public ObservableCollection<Account> accounts = new();

        /// <summary>
        /// Goes to AddAccountPage.xaml
        /// </summary>
        [RelayCommand]
        async Task GoToAddAccount()
        {
            await Shell.Current.GoToAsync(nameof(AddAccountPage));
        }

        /// <summary>
        /// Goes to EditAccountPage.xaml with an Account object as a parameter
        /// </summary>
        [RelayCommand]
        async Task GoToEditAccount(int ID)
        {
            await Shell.Current.GoToAsync($"{nameof(EditAccountPage)}?ID={ID}");
        }

        /// <summary>
        /// Gets updated accounts from database and refreshes Accounts collection
        /// </summary>
        [RelayCommand]
        public async Task Refresh()
        {
            Accounts.Clear();
            var accounts = await AccountService.GetAccounts();
            foreach (var acc in accounts)
                Accounts.Add(acc);
        }
    }
}
