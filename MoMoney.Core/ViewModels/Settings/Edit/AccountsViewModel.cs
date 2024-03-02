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
    /// Goes to AddAccountPage.xaml.
    /// </summary>
    [RelayCommand]
    async Task GoToAddAccount()
    {
        await Shell.Current.GoToAsync("AddAccountPage");
    }

    /// <summary>
    /// Goes to EditAccountPage.xaml with an Account ID as a parameter.
    /// </summary>
    [RelayCommand]
    async Task GoToEditAccount(int ID)
    {
        await Shell.Current.GoToAsync($"EditAccountPage?ID={ID}");
    }

    /// <summary>
    /// Gets updated accounts from database, orders them, and refreshes Accounts collection.
    /// </summary>
    public async void Init(object s, EventArgs e)
    {
        try
        {
            var accounts = await accountService.GetAccounts();
            if (!accounts.Any())
            {
                Accounts.Clear();
                return;
            }

            accounts = accounts.OrderByDescending(a => a.Enabled)
                               .ThenBy(a => a.AccountName);
            Accounts.Clear();
            foreach (var acc in accounts)
                Accounts.Add(acc);
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(Init), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }
}