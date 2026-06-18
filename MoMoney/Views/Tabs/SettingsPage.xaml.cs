using CommunityToolkit.Mvvm.Messaging;
using MoMoney.Core.Helpers;
using MoMoney.Core.ViewModels;

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