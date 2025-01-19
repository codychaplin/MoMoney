using MoMoney.Core.Helpers;
using MoMoney.Core.ViewModels.Settings.Edit;

namespace MoMoney.Views.Settings.Edit;

public partial class AccountsPage : ContentPage
{
	AccountsViewModel vm;
	public AccountsPage(AccountsViewModel _vm)
	{
		InitializeComponent();
        vm = _vm;
        BindingContext = vm;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await PageLoader.Load(vm.LoadAccounts);
    }
}