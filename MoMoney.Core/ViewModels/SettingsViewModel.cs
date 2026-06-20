using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Helpers;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    readonly ILoggerService<SettingsViewModel> logger;
    
    [ObservableProperty] bool materialYouEnabled = false;
    [ObservableProperty] bool showSensitiveValuesEnabled = true;

    public List<ThemeInfo> Themes { get; } =
    [
        new("green",  Color.FromArgb("#63DBA2")),
        new("blue",   Color.FromArgb("#5B9BD5")),
        new("teal",   Color.FromArgb("#4AC4C4")),
        new("purple", Color.FromArgb("#9B6DD5")),
        new("yellow", Color.FromArgb("#FFDE3F")),
        new("gray", Color.FromArgb("#898989"))
    ];

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
    /// Toggles Material You and persists to Preferences
    /// </summary>
    /// <param name="value"></param>
    partial void OnMaterialYouEnabledChanged(bool value)
    {
        Utilities.MaterialYouEnabled = value;
        Utilities.ApplyTheme(Utilities.CurrentTheme);

        logger.LogFirebaseEvent(FirebaseParameters.EVENT_MATERIAL_YOU_TOGGLED, FirebaseParameters.GetFirebaseParameters());
    }

    /// <summary>
    /// Toggles Show Sensitive Values
    /// </summary>
    /// <param name="value"></param>
    partial void OnShowSensitiveValuesEnabledChanged(bool value)
    {
        Utilities.ShowValue = value;
        WeakReferenceMessenger.Default.Send(new UpdateHomePageMessage());
    }

    /// <summary>
    /// Goes to AdminPage.xaml.
    /// </summary>
    [RelayCommand]
    async Task GoToAdmin()
    {
        await Shell.Current.GoToAsync("AdminPage");
    }

    /// <summary>
    /// Toggles developer mode
    /// </summary>
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