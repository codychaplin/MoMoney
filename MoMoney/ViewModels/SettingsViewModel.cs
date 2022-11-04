using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoMoney.Views;

namespace MoMoney.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {

        [RelayCommand]
        async Task GoToAccounts()
        {
            await Shell.Current.GoToAsync(nameof(AccountsPage));
        }
    }
}