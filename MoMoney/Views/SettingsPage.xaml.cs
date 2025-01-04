using CommunityToolkit.Mvvm.Messaging;
using MoMoney.Core.Helpers;
using MoMoney.Core.ViewModels;

namespace MoMoney.Views;

public partial class SettingsPage : ContentView
{
    public SettingsPage()
    {
        InitializeComponent();

        HandlerChanged += (s, e) =>
        {
            BindingContext = Handler?.MauiContext?.Services.GetService<SettingsViewModel>();
        };

        lblVersion.Text = $"MoMoney ({AppInfo.Current.VersionString})";

        // switch toggle to avoid ThumbColor bug
        swShowValues.IsToggled = false;
        swShowValues.IsToggled = true;
    }

    /// <summary>
    /// Updates static ShowValue property.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void swShowValues_Toggled(object sender, ToggledEventArgs e)
    {
        Utilities.ShowValue = swShowValues.IsToggled;
        WeakReferenceMessenger.Default.Send(new UpdateHomePageMessage());
    }
}