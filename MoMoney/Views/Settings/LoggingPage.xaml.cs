using Microsoft.Extensions.Logging;
using MoMoney.Core.ViewModels.Settings;

namespace MoMoney.Views.Settings;

public partial class LoggingPage : ContentPage
{
	public LoggingPage(LoggingViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
		vm.listview = listview;
		Loaded += async (s, e) => await vm.Init();

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
}