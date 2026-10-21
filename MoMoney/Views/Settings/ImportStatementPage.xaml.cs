using MoMoney.Components;
using MoMoney.Core.Helpers;
using MoMoney.Core.ViewModels.Settings;

namespace MoMoney.Views;

public partial class ImportStatementPage : CustomContentPage
{
	ImportStatementViewModel vm;

	public ImportStatementPage(ImportStatementViewModel _vm)
	{
		InitializeComponent();
		vm = _vm;
        BindingContext = vm;
	}

	protected override async void OnAppearing()
    {
        base.OnAppearing();
        await PageLoader.Load(vm.LoadData);
    }
}