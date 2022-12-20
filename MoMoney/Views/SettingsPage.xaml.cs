using MoMoney.ViewModels;

namespace MoMoney.Views;

public partial class SettingsPage : ContentView
{
    SettingsViewModel vm;

    public SettingsPage()
    {
        InitializeComponent();
        vm = (SettingsViewModel)BindingContext;
    }
}