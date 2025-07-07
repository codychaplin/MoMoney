using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MoMoney.Core.Helpers;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels;

public partial class SettingsViewModel
{
    readonly ILoggerService<SettingsViewModel> logger;

    public SettingsViewModel(ILoggerService<SettingsViewModel> _logger)
    {
        logger = _logger;
    }

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

    [RelayCommand]
    void ToggleDeveloperMode()
    {
        bool isAdmin = Utilities.IsAdmin;
        bool newIsAdmin = !isAdmin;
        Utilities.IsAdmin = newIsAdmin;

        // disable AI feature when disabling developer mode
        if (!newIsAdmin)
        {
            Utilities.TransactionDictationEnabled = false;
            WeakReferenceMessenger.Default.Send(new UpdateTransactionDictationMessage(false));
        }

        logger.LogFirebaseEvent(FirebaseParameters.EVENT_DEVELOPER_MODE_TOGGLED, FirebaseParameters.GetFirebaseParameters());
        _ = Utilities.DisplayToast($"Developer mode {(newIsAdmin ? "enabled" : "disabled")}");
    }
}