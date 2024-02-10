using MoMoney.Core.ViewModels.Settings;

namespace MoMoney.Views.Settings;

public partial class ImportExportPage : ContentPage
{
	public ImportExportPage(ImportExportViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}