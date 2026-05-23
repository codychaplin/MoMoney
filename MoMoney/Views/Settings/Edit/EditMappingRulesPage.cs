using MoMoney.Core.ViewModels.Settings.Edit;

namespace MoMoney.Views.Settings.Edit;

[QueryProperty(nameof(Bank), "bank")]
public partial class EditMappingRulesPage : ContentPage
{
    EditMappingRulesViewModel vm;

    public string? Bank { get; set; }

	public EditMappingRulesPage(EditMappingRulesViewModel _vm)
	{
		InitializeComponent();
        vm = _vm;
        BindingContext = vm;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await vm.LoadData(Bank);
    }

    protected override async void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);
        if (args.NavigationType == NavigationType.Pop)
            await vm.ReloadRules();
    }
}