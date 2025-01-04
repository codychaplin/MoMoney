using MoMoney.Core.ViewModels.Settings;

namespace MoMoney.Views.Settings;

public partial class BulkEditingPage : ContentPage
{
	BulkEditingViewModel vm;
	public BulkEditingPage(BulkEditingViewModel _vm)
	{
		InitializeComponent();
		vm = _vm;
		BindingContext = vm;
		Loaded += vm.Init;

		PckFindAccount.SelectedValueChanged += OnClear;
		PckFindCategory.SelectedValueChanged += OnClear;
		FindPayee.SelectionChanged += OnClear;

		PckReplaceAccount.SelectedValueChanged += OnClear;
        PckReplaceCategory.SelectedValueChanged += OnClear;
		ReplacePayee.TextChanged += OnClear;

		PckFindCategory.SelectedValueChanged += vm.GetFindSubcategories;
		PckReplaceCategory.SelectedValueChanged += vm.GetReplaceSubcategories;
	}

	void OnClear(object? sender, object newValue)
	{
		if (newValue == null)
		{
			vm.Clear();
        }
	}
}