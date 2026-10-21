using MoMoney.Core.ViewModels.Tabs;

namespace MoMoney.Views.Tabs;

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
    }
}