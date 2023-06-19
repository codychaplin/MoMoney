using MoMoney.ViewModels.Settings;

namespace MoMoney.Views.Settings;

public partial class LoggingPage : ContentPage
{
	public LoggingPage(LoggingViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
		Loaded += vm.Init;
		vm.listview = listview;

		pckLevels.SelectedIndexChanged += vm.UpdateFilter;
		pckClasses.SelectedIndexChanged += vm.UpdateFilter;
		pckExceptions.SelectedIndexChanged += vm.UpdateFilter;
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