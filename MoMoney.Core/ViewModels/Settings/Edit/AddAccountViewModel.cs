using SQLite;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Helpers;
using MoMoney.Core.Services;
using MoMoney.Core.Exceptions;

namespace MoMoney.Core.ViewModels.Settings.Edit;

public partial class AddAccountViewModel : ObservableObject
{
    readonly IAccountService accountService;
    readonly ILoggerService<AddAccountViewModel> logger;

    [ObservableProperty]
    public string name; // account name

    [ObservableProperty]
    public string type; // account type

    [ObservableProperty]
    public decimal startingBalance; // starting balance

    public AddAccountViewModel(IAccountService _accountService, ILoggerService<AddAccountViewModel> _logger)
    {
        accountService = _accountService;
        logger = _logger;
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
            WeakReferenceMessenger.Default.Send(new UpdateAccountsMessage()); // update accounts on AddTransactionPage
            await Shell.Current.GoToAsync("..");
        }
        catch (SQLiteException ex)
        {
            await logger.LogCritical(ex.Message, ex.GetType().Name);
            await Shell.Current.DisplayAlert("Database Error", ex.Message, "OK");
        }
        catch (DuplicateAccountException ex)
        {
            await logger.LogWarning(ex.Message, ex.GetType().Name);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }
}
