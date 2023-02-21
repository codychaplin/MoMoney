using SQLite;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Services;
using MoMoney.Exceptions;

namespace MoMoney.ViewModels.Settings;

public partial class AddAccountViewModel : ObservableObject
{
    [ObservableProperty]
    public string name; // account name

    [ObservableProperty]
    public string type; // account type

    [ObservableProperty]
    public decimal startingBalance; // starting balance

    /// <summary>
    /// adds Account to database using input fields from view.
    /// </summary>
    [RelayCommand]
    async Task Add()
    {
        try
        {
            await AccountService.AddAccount(Name, Type, StartingBalance);
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
