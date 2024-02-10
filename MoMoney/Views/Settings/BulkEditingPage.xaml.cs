using MoMoney.Core.ViewModels.Settings;

namespace MoMoney.Views.Settings;

public partial class BulkEditingPage : ContentPage
{
	public BulkEditingPage(BulkEditingViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
		Loaded += vm.Init;
		pckFindCategory.SelectedIndexChanged += vm.GetFindSubcategories;
		pckReplaceCategory.SelectedIndexChanged += vm.GetReplaceSubcategories;
	}
}