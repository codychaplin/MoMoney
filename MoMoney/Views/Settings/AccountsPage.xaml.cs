using MoMoney.ViewModels.Settings;

namespace MoMoney.Views.Settings;

public partial class AccountsPage : ContentPage
{
	AccountsViewModel vm;

	public AccountsPage()
	{
		InitializeComponent();
		vm = (AccountsViewModel)BindingContext;
		NavigatedTo += vm.Init;
	}
}