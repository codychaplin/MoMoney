using MoMoney.Core.ViewModels.Stats;

namespace MoMoney.Views.Stats;

public partial class AccountSummaryPage : ContentPage
{
    public AccountSummaryPage(AccountSummaryViewModel _vm)
    {
        InitializeComponent();
        BindingContext = _vm;
        Loaded += _vm.Init;
    }
}