using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Models;
using MoMoney.Services;

namespace MoMoney.ViewModels.Settings;

[QueryProperty(nameof(ID), nameof(ID))]
public partial class EditAccountViewModel : ObservableObject
{
    [ObservableProperty]
    public Account account = new();

    public string ID { get; set; } // account ID

    /// <summary>
    /// Gets Account using ID.
    /// </summary>
    public async Task GetAccount()
    {
        if (int.TryParse(ID, out int id))
            Account = await AccountService.GetAccount(id);
        else
            await Shell.Current.DisplayAlert("Error", "Could not find account", "OK");
    }

    /// <summary>
    /// Edits Account in database using input fields from view.
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
            await AccountService.UpdateAccount(Account);
            await Shell.Current.GoToAsync("..");
        }
    }

    /// <summary>
    /// Removes the Account from the database.
    /// </summary>
    [RelayCommand]
    async Task Remove()
    {
        bool flag = await Shell.Current.DisplayAlert("", $"Are you sure you want to delete \"{Account.AccountName}\"?", "Yes", "No");

        if (flag)
        {
            await AccountService.RemoveAccount(Account.AccountID);
            await Shell.Current.GoToAsync("..");
        }
    }
}
