using MoMoney.Services;

namespace MoMoney;

public partial class App : Application
{
	public App()
	{
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(Secret.SfLicenseKey);

        InitializeComponent();

		MainPage = new AppShell();

        Task.Run(async () =>
        {
            await MoMoneydb.Init();
            await AccountService.Init();
            await CategoryService.Init();
            await StockService.Init();
        }).Wait();
    }
}
