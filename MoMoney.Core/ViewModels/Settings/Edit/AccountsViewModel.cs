using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Models;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels.Settings.Edit;

public partial class AccountsViewModel : ObservableObject
{
    readonly IAccountService accountService;
    readonly ILoggerService<AccountsViewModel> logger;

    [ObservableProperty] ObservableCollection<Account> accounts = [];

    public AccountsViewModel(IAccountService _accountService, ILoggerService<AccountsViewModel> _logger)
    {
        accountService = _accountService;
        logger = _logger;
    }

    /// <summary>
    /// Gets updated accounts from database, orders them, and refreshes Accounts collection.
    /// </summary>
    public async void RefreshAccounts(object sender, EventArgs e)
    {
        try
        {
            await Task.Delay(1);
            var accounts = await accountService.GetOrderedAccounts();
            if (!accounts.Any())
            {
                Accounts.Clear();
                return;
            }

            Accounts.Clear();
            foreach (var account in accounts)
                Accounts.Add(account);
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(RefreshAccounts), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Goes to AddAccountPage.xaml.
    /// </summary>
    [RelayCommand]
    async Task GoToAddAccount()
    {
        await Shell.Current.GoToAsync("EditAccountPage", new ShellNavigationQueryParameters() { { "Account", null } });
    }

    /// <summary>
    /// Goes to EditAccountPage.xaml with an Account ID as a parameter.
    /// </summary>
    [RelayCommand]
    async Task GoToEditAccount(Account account)
    {
        await Shell.Current.GoToAsync($"EditAccountPage", new ShellNavigationQueryParameters() { { "Account", account } });
    }
}