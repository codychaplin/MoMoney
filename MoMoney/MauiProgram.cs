using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using Syncfusion.Maui.Core.Hosting;

namespace MoMoney;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
            .ConfigureSyncfusionCore()
            .UseMauiCommunityToolkit()
			.UseMauiCommunityToolkitCore()
            .ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("Metropolis-Regular.otf", "Metropolis");
				fonts.AddFont("Metropolis-Bold.otf", "MetropolisBold");
			});

		return builder.Build();
	}
}
