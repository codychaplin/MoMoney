using MoMoney.Core.ViewModels.Settings.Edit;

namespace MoMoney.Views.Settings.Edit;

public partial class EditMappingRulesPage : ContentPage
{
    EditMappingRulesViewModel vm;

	public EditMappingRulesPage(EditMappingRulesViewModel _vm)
	{
		InitializeComponent();
        vm = _vm;
        BindingContext = vm;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await vm.LoadData();
    }

    protected override async void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);
        if (args.NavigationType == NavigationType.Pop)
            await vm.ReloadRules();
    }
}