using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MoMoney.Core.Helpers;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels;

public partial class SettingsViewModel
{
    readonly ILoggerService<SettingsViewModel> logger;

    public List<ThemeInfo> Themes { get; } =
    [
        new("green",  Color.FromArgb("#63DBA2")),
        new("blue",   Color.FromArgb("#5B9BD5")),
        new("teal",   Color.FromArgb("#4AC4C4")),
        new("purple", Color.FromArgb("#9B6DD5")),
        new("yellow", Color.FromArgb("#FFDE3F")),
        new("red",    Color.FromArgb("#E85A5A")),
    ];

    public SettingsViewModel(ILoggerService<SettingsViewModel> _logger)
    {
        logger = _logger;
    }

    /// <summary>
    /// Selects a theme and applies it immediately.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    [RelayCommand]
    static async Task SelectTheme(string name)
    {
        if (name == Utilities.CurrentTheme)
            return;
        
        Utilities.ApplyTheme(name);
        Utilities.CurrentTheme = name;
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

    [RelayCommand]
    async Task GoToMappingRules()
    {
        await Shell.Current.GoToAsync("EditMappingRulesPage");
    }

    /// <summary>
    /// Goes to GoToImportStatement.xaml.
    /// </summary>
    [RelayCommand]
    async Task GoToImportStatement()
    {
        await Shell.Current.GoToAsync("ImportStatementPage");
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