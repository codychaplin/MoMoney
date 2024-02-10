using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
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
                await logger.LogError(nameof(GetAccount), ex);
                await Shell.Current.DisplayAlert("Account Not Found Error", ex.Message, "OK");
            }
            catch (Exception ex)
            {
                await logger.LogError(nameof(GetAccount), ex);
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }

            return;
        }

        string message = $"{ID} is not a valid ID";
        await logger.LogError(nameof(GetAccount), new Exception(message));
        await Shell.Current.DisplayAlert("Account Not Found Error", message, "OK");
    }

    /// <summary>
    /// Edits Account in database using input fields from view.
    /// </summary>
    [RelayCommand]
    async Task EditAccount()
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
            logger.LogFirebaseEvent(FirebaseParameters.EVENT_EDIT_ACCOUNT, FirebaseParameters.GetFirebaseParameters());
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(EditAccount), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Removes the Account from the database.
    /// </summary>
    [RelayCommand]
    async Task RemoveAccount()
    {
        bool flag = await Shell.Current.DisplayAlert("", $"Are you sure you want to delete \"{Account.AccountName}\"?", "Yes", "No");
        if (!flag) return;

        try
        {
            await accountService.RemoveAccount(Account.AccountID);
            logger.LogFirebaseEvent(FirebaseParameters.EVENT_DELETE_ACCOUNT, FirebaseParameters.GetFirebaseParameters());
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(RemoveAccount), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }
}
