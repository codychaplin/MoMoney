using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MvvmHelpers;
using MoMoney.Core.Models;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels.Settings.Edit;

public partial class AccountsViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    readonly IAccountService accountService;
    readonly ILoggerService<AccountsViewModel> logger;

    [ObservableProperty] ObservableRangeCollection<Account> accounts = [];

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

            Accounts.ReplaceRange(accounts);
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
}