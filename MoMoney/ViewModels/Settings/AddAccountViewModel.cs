﻿using SQLite;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Views;
using MoMoney.Services;
using MoMoney.Exceptions;

namespace MoMoney.ViewModels.Settings;

public partial class AddAccountViewModel : ObservableObject
{
    readonly IAccountService accountService;

    [ObservableProperty]
    public string name; // account name

    [ObservableProperty]
    public string type; // account type

    [ObservableProperty]
    public decimal startingBalance; // starting balance

    public AddAccountViewModel(IAccountService _accountService)
    {
        accountService = _accountService;
    }

    /// <summary>
    /// adds Account to database using input fields from view.
    /// </summary>
    [RelayCommand]
    async Task Add()
    {
        try
        {
            await accountService.AddAccount(Name, Type, StartingBalance);
            AddTransactionPage.UpdatePage?.Invoke(this, new EventArgs()); // update accounts on AddTransactionPage
            await Shell.Current.GoToAsync("..");
        }
        catch (SQLiteException ex)
        {
            await Shell.Current.DisplayAlert("Database Error", ex.Message, "OK");
        }
        catch (DuplicateAccountException ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }
}
