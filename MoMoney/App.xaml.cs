namespace MoMoney;

public partial class App : Application
{
	public App()
	{
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(Constants.SfLicenseKey);

        InitializeComponent();

		MainPage = new AppShell();
	}
}
