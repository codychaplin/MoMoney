using MoMoney.ViewModels;

namespace MoMoney.Views;

public partial class SettingsPage : ContentView
{
    public SettingsPage(SettingsViewModel _vm)
    {
        InitializeComponent();
        BindingContext = _vm;
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