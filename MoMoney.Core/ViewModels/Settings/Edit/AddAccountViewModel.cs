using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Helpers;
using MoMoney.Core.Exceptions;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels.Settings.Edit;

public partial class AddAccountViewModel : ObservableObject
{
    readonly IAccountService accountService;
    readonly ILoggerService<AddAccountViewModel> logger;

    [ObservableProperty] string name; // account name
    [ObservableProperty] string type; // account type
    [ObservableProperty] decimal startingBalance; // starting balance

    public AddAccountViewModel(IAccountService _accountService, ILoggerService<AddAccountViewModel> _logger)
    {
        accountService = _accountService;
        logger = _logger;
    }

    /// <summary>
    /// adds Account to database using input fields from view.
    /// </summary>
    [RelayCommand]
    async Task AddAccount()
    {
        try
        {
            await accountService.AddAccount(Name, Type, StartingBalance);
            logger.LogFirebaseEvent(FirebaseParameters.EVENT_ADD_ACCOUNT, FirebaseParameters.GetFirebaseParameters());
            await Shell.Current.GoToAsync("..");
        }
        catch (DuplicateAccountException ex)
        {
            await logger.LogWarning(nameof(AddAccount), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(AddAccount), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }
}