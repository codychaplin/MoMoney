namespace MoMoney;

public partial class App : Application
{
	public App()
	{
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(Secret.SfLicenseKey);

        InitializeComponent();

		MainPage = new AppShell();
    }
}
