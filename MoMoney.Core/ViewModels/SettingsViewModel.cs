using CommunityToolkit.Mvvm.Input;

namespace MoMoney.Core.ViewModels;

public partial class SettingsViewModel
{
    /// <summary>
    /// Goes to AccountsPage.xaml.
    /// </summary>
    [RelayCommand]
    async Task GoToAccounts()
    {
        await Shell.Current.GoToAsync("AccountsPage");
    }

    /// <summary>
    /// Goes to CategoriesPage.xaml.
    /// </summary>
    [RelayCommand]
    async Task GoToCategories()
    {
        await Shell.Current.GoToAsync("CategoriesPage");
    }

    /// <summary>
    /// Goes to StockSettingsPage.xaml.
    /// </summary>
    [RelayCommand]
    async Task GoToStocks()
    {
        await Shell.Current.GoToAsync("StocksPage");
    }

    /// <summary>
    /// Goes to ImportExportPage.xaml.
    /// </summary>
    [RelayCommand]
    async Task GoToImportExport()
    {
        await Shell.Current.GoToAsync("ImportExportPage");
    }

    /// <summary>
    /// Goes to AdminPage.xaml.
    /// </summary>
    [RelayCommand]
    async Task GoToAdmin()
    {
        await Shell.Current.GoToAsync("AdminPage");
    }
}