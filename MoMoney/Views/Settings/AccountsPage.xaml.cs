using MoMoney.ViewModels.Settings;

namespace MoMoney.Views.Settings;

public partial class AccountsPage : ContentPage
{
	public AccountsPage(AccountsViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
		NavigatedTo += vm.Init;
	}
}