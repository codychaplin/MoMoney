using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Exceptions;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels.Settings.Edit;

public partial class EditAccountViewModel : BaseEditViewModel<IAccountService, EditAccountViewModel>
{
    [ObservableProperty] Account account = new();

    public EditAccountViewModel(IAccountService _accountService, ILoggerService<EditAccountViewModel> _logger) : base(_accountService, _logger) { }

    /// <summary>
    /// Controls whether the view is in edit mode or not.
    /// </summary>
    /// <param name="query"></param>
    public override void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query["Account"] is not Account account)
            return;

        IsEditMode = true;
        Account = new(account);
    }

    /// <summary>
    /// Adds Account to database using input fields from view.
    /// </summary>
    [RelayCommand]
    protected override async Task Add()
    {
        try
        {
            await service.AddAccount(Account.AccountName, Account.AccountType, Account.StartingBalance);
            logger.LogFirebaseEvent(FirebaseParameters.EVENT_ADD_ACCOUNT, FirebaseParameters.GetFirebaseParameters());
            await Shell.Current.GoToAsync("..");
        }
        catch (DuplicateAccountException ex)
        {
            await logger.LogWarning(nameof(Add), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(Add), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Edits Account in database using input fields from view.
    /// </summary>
    [RelayCommand]
    protected override async Task Edit()
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
            await service.UpdateAccount(Account);
            logger.LogFirebaseEvent(FirebaseParameters.EVENT_EDIT_ACCOUNT, FirebaseParameters.GetFirebaseParameters());
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(Edit), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Removes the Account from the database.
    /// </summary>
    [RelayCommand]
    protected override async Task Remove()
    {
        bool flag = await Shell.Current.DisplayAlert("", $"Are you sure you want to delete \"{Account.AccountName}\"?", "Yes", "No");
        if (!flag)
            return;

        try
        {
            await service.RemoveAccount(Account.AccountID);
            logger.LogFirebaseEvent(FirebaseParameters.EVENT_DELETE_ACCOUNT, FirebaseParameters.GetFirebaseParameters());
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(Remove), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }
}