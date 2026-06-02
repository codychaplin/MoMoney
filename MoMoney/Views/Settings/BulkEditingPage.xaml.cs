using MoMoney.Core.Helpers;
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
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
		await PageLoader.Load(vm.LoadBulkData);
    }
}