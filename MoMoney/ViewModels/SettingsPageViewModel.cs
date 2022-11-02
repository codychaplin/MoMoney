using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoMoney.Views;

namespace MoMoney.ViewModels
{
    public partial class SettingsPageViewModel : ObservableObject
    {

        [RelayCommand]
        async Task Accounts()
        {
            await Shell.Current.GoToAsync(nameof(AccountsPage));
        }
    }
}