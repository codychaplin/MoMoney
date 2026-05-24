using MoMoney.Core.Helpers;
using MoMoney.Core.ViewModels;

namespace MoMoney.Views;

public partial class EditTransactionPage : ContentPage
{
    EditTransactionViewModel vm;

	public EditTransactionPage(EditTransactionViewModel _vm)
	{
		InitializeComponent();
        vm = _vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await PageLoader.Load(vm.Init);
    }
}
