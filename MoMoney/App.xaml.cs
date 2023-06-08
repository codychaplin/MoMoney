using CommunityToolkit.Mvvm.Input;
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
}