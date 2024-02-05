using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Models;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels.Settings.Edit;

public partial class AccountsViewModel : ObservableObject
{
    readonly IAccountService accountService;

    [ObservableProperty]
    public ObservableCollection<Account> accounts = new();

    public AccountsViewModel(IAccountService _accountService)
    {
        accountService = _accountService;
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
}