using MoMoney.ViewModels.Settings;

namespace MoMoney.Views.Settings;

public partial class LoggingPage : ContentPage
{
	public LoggingPage(LoggingViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
		Loaded += vm.Init;
	}
}