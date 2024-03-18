using MoMoney.Core.ViewModels.Settings.Edit;

namespace MoMoney.Views.Settings.Edit;

public partial class AccountsPage : ContentPage
{
	public AccountsPage(AccountsViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
		NavigatedTo += vm.RefreshAccounts;
	}
}