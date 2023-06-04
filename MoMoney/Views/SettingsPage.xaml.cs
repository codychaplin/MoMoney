using MoMoney.ViewModels;

namespace MoMoney.Views;

public partial class SettingsPage : ContentView
{
    SettingsViewModel vm;

    public SettingsPage(SettingsViewModel _vm)
    {
        InitializeComponent();
        vm = _vm;
        BindingContext = vm;
    }

    /// <summary>
    /// Updates static ShowValue property.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void swShowValues_Toggled(object sender, ToggledEventArgs e)
    {
        Constants.ShowValue = swShowValues.IsToggled;
    }
}