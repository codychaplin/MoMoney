using MoMoney.ViewModels;

namespace MoMoney.Views;

public partial class SettingsPage : ContentView
{
    public SettingsPage(SettingsViewModel vm)
    {
        InitializeComponent();
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