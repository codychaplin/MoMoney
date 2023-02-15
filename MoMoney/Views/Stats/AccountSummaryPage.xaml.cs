using MoMoney.ViewModels.Stats;

namespace MoMoney.Views.Stats;

public partial class AccountSummaryPage : ContentPage
{
    AccountSummaryViewModel vm;

    public AccountSummaryPage()
    {
        InitializeComponent();
        vm = (AccountSummaryViewModel)BindingContext;
        Loaded += vm.Init;
    }
}