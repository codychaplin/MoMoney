using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Views.Settings;

namespace MoMoney.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    /// <summary>
    /// Goes to AccountsPage.xaml.
    /// </summary>
    [RelayCommand]
    async Task GoToAccounts()
    {
        await Shell.Current.GoToAsync(nameof(AccountsPage));
    }

    /// <summary>
    /// Goes to CategoriesPage.xaml.
    /// </summary>
    [RelayCommand]
    async Task GoToCategories()
    {
        await Shell.Current.GoToAsync(nameof(CategoriesPage));
    }

    /// <summary>
    /// Goes to StockSettingsPage.xaml.
    /// </summary>
    [RelayCommand]
    async Task GoToStocks()
    {
        await Shell.Current.GoToAsync(nameof(StockSettingsPage));
    }

    /// <summary>
    /// Goes to ImportExportPage.xaml.
    /// </summary>
    [RelayCommand]
    async Task GoToImportExport()
    {
        await Shell.Current.GoToAsync(nameof(ImportExportPage));
    }

    /// <summary>
    /// Goes to AdminPage.xaml.
    /// </summary>
    [RelayCommand]
    async Task GoToAdmin()
    {
        await Shell.Current.GoToAsync(nameof(AdminPage));
    }
}