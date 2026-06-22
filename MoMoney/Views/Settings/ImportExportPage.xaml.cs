using MoMoney.Components;
using MoMoney.Core.ViewModels.Settings;

namespace MoMoney.Views.Settings;

public partial class ImportExportPage : CustomContentPage
{
	public ImportExportPage(ImportExportViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}