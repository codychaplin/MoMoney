using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoMoney.Views.Settings;

namespace MoMoney.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {

        [RelayCommand]
        async Task GoToAccounts()
        {
            await Shell.Current.GoToAsync(nameof(AccountsPage));
        }

        [RelayCommand]
        async Task GoToCategories()
        {
            await Shell.Current.GoToAsync(nameof(CategoriesPage));
        }

        [RelayCommand]
        async Task CalculateBalance()
        {
            
        }
    }
}