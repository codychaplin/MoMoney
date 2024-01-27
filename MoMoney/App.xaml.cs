using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Maui.Alerts;
using MoMoney.Views;

namespace MoMoney;

public partial class App : Application
{
	public App()
	{
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(Secret.SfLicenseKey);

        InitializeComponent();
		MainPage = new AppShell();
    }

    /// <summary>
    /// Goes to EditTransactionsPage with a transaction ID as the parameter.
    /// </summary>
    /// <param name="ID"></param>
    [RelayCommand]
    async Task GoToEditTransaction(int ID)
    {
        await Shell.Current.GoToAsync($"{nameof(EditTransactionPage)}?ID={ID}");
    }

    /// <summary>
    /// Displays a toast message to the user.
    /// </summary>
    /// <param name="message"></param>
    public static async Task DisplayToast(string message, ToastDuration duration = ToastDuration.Long, double fontSize = 14)
    {
        var toast = Toast.Make(message, duration, fontSize);
        await toast.Show();
    }
}