using MoMoney.ViewModels.Stats;

namespace MoMoney.Views.Stats;

public partial class AccountSummaryPage : ContentPage
{
    AccountSummaryViewModel vm;

    public AccountSummaryPage()
	{
		InitializeComponent();
        vm = (AccountSummaryViewModel)BindingContext;
        Loaded += Refresh;
    }

    private async void Refresh(object s, EventArgs e)
    {
        await vm.GetAccounts();
    }
}