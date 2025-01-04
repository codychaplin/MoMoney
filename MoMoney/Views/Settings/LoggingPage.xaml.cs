using Microsoft.Extensions.Logging;
using MoMoney.Core.Helpers;
using MoMoney.Core.ViewModels.Settings;

namespace MoMoney.Views.Settings;

public partial class LoggingPage : ContentPage
{
	LoggingViewModel vm;

    public LoggingPage(LoggingViewModel _vm)
	{
		InitializeComponent();
		vm = _vm;
        BindingContext = vm;

		PckLevels.SelectedValueChanged += (s, e) =>
		{
			vm.UpdateLevelCommand.Execute(e ?? LogLevel.None);
        };
		PckClasses.SelectedValueChanged += (s, e) =>
		{
			vm.UpdateClassCommand.Execute(e);
        };
		PckExceptions.SelectedValueChanged += (s, e) =>
		{
			vm.UpdateExceptionCommand.Execute(e);
		};
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
		await PageLoader.Load(vm.LoadLogs);
    }
}