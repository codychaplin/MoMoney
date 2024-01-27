using SQLite;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Models;
using MoMoney.Core.Exceptions;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels.Settings.Edit;

[QueryProperty(nameof(ID), nameof(ID))]
public partial class EditAccountViewModel : ObservableObject
{
    readonly IAccountService accountService;
    readonly ILoggerService<EditAccountViewModel> logger;

    [ObservableProperty]
    public Account account = new();

    public string ID { get; set; } // account ID

    public EditAccountViewModel(IAccountService _accountService, ILoggerService<EditAccountViewModel> _logger)
    {
        accountService = _accountService;
        logger = _logger;
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
            }
            catch (AccountNotFoundException ex)
            {
                await logger.LogError(ex.Message, ex.GetType().Name);
                await Shell.Current.DisplayAlert("Account Not Found Error", ex.Message, "OK");
            }

            return;
        }

        string message = $"{ID} is not a valid ID";
        await logger.LogError(message);
        await Shell.Current.DisplayAlert("Account Not Found Error", message, "OK");
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
            await logger.LogCritical(ex.Message, ex.GetType().Name);
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
        if (!flag) return;

        try
        {
            await accountService.RemoveAccount(Account.AccountID);
            await Shell.Current.GoToAsync("..");
        }
        catch (SQLiteException ex)
        {
            await logger.LogCritical(ex.Message, ex.GetType().Name);
            await Shell.Current.DisplayAlert("Database Error", ex.Message, "OK");
        }
    }
}
