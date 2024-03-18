using MoMoney.Core.ViewModels.Settings;

namespace MoMoney.Views.Settings;

public partial class LoggingPage : ContentPage
{
	public LoggingPage(LoggingViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
		Loaded += vm.Init;
		vm.listview = listview;
	}

	/// <summary>
	/// Updates the DataTemplateSelector
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
    private void Check_Toggled(object sender, EventArgs e)
    {
		listview.RefreshView();
    }
}