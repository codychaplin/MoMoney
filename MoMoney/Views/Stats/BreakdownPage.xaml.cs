using MoMoney.Core.Helpers;
using MoMoney.Core.ViewModels.Stats;

namespace MoMoney.Views.Stats;

public partial class BreakdownPage : ContentPage
{
    BreakdownViewModel vm;
    public BreakdownPage(BreakdownViewModel _vm)
    {
        InitializeComponent();
        vm = _vm;
        BindingContext = _vm;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await PageLoader.Load(vm.LoadBreakdown);
    }
}