using SQLite;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Models;
using MoMoney.Services;
using MoMoney.Exceptions;
using MoMoney.Views;
using Kotlin.Contracts;

namespace MoMoney.ViewModels.Settings;

[QueryProperty(nameof(ID), nameof(ID))]
public partial class EditAccountViewModel : ObservableObject
{
    readonly IAccountService accountService;

    [ObservableProperty]
    public Account account = new();

    public string ID { get; set; } // account ID

    public EditAccountViewModel(IAccountService _accountService)
    {
        accountService = _accountService;
    }

    /// <summary>
    /// Gets Account using ID.
    /// </summary>
    public async Task GetAccount()
    {
        if (int.TryParse(ID, out int id))
        {
            try
            {
                Account = await accountService.GetAccount(id);
                AddTransactionPage.UpdatePage?.Invoke(this, new EventArgs()); // update accounts on AddTransactionPage
            }
            catch (AccountNotFoundException ex)
            {
                await Shell.Current.DisplayAlert("Account Not Found Error", ex.Message, "OK");
            }

            return;
        }
        
        await Shell.Current.DisplayAlert("Account Not Found Error", $"{ID} is not a valid ID", "OK");
    }

    /// <summary>
    /// Edits Account in database using input fields from view.
    /// </summary>
    [RelayCommand]
    async Task Edit()
    {
        if (Account is null ||
            string.IsNullOrEmpty(Account.AccountName) ||
            string.IsNullOrEmpty(Account.AccountType))
        {
            // if invalid, display error
            await Shell.Current.DisplayAlert("Validation Error", "Information not valid", "OK");
            return;
        }

        try
        {
            await accountService.UpdateAccount(Account);
            await Shell.Current.GoToAsync("..");
        }
        catch (SQLiteException ex)
        {
            await Shell.Current.DisplayAlert("Database Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Removes the Account from the database.
    /// </summary>
    [RelayCommand]
    async Task Remove()
    {
        bool flag = await Shell.Current.DisplayAlert("", $"Are you sure you want to delete \"{Account.AccountName}\"?", "Yes", "No");

        if (!flag)
            return;

        try
        {
            await accountService.RemoveAccount(Account.AccountID);
            await Shell.Current.GoToAsync("..");
        }
        catch (SQLiteException ex)
        {
            await Shell.Current.DisplayAlert("Database Error", ex.Message, "OK");
        }
    }
}
