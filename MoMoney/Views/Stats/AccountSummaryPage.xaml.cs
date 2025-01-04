using MoMoney.Core.Helpers;
using MoMoney.Core.ViewModels.Stats;

namespace MoMoney.Views.Stats;

public partial class AccountSummaryPage : ContentPage
{
    AccountSummaryViewModel vm;
    public AccountSummaryPage(AccountSummaryViewModel _vm)
    {
        InitializeComponent();
        vm = _vm;
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await PageLoader.Load(vm.LoadAccountsSummary);
    }
}