using MoMoney.ViewModels;

namespace MoMoney.Views;

public partial class SettingsPage : ContentPage
{
    SettingsViewModel vm;

    public SettingsPage()
    {
        InitializeComponent();
        vm = (SettingsViewModel)BindingContext;
    }
}