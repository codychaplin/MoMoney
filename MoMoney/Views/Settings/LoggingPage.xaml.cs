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

	/// <summary>
	/// Updates the DataTemplateSelector
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
    private void switch_Toggled(object sender, ToggledEventArgs e)
    {
		listview.RefreshView();
    }
}