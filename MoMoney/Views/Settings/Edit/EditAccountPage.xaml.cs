using MoMoney.Components;
using MoMoney.Core.ViewModels.Settings.Edit;

namespace MoMoney.Views.Settings.Edit;

public partial class EditAccountPage : CustomContentPage
{
    public EditAccountPage(EditAccountViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // workaround for switch thumbcolor not updating on load
        swEnabled.IsToggled = !swEnabled.IsToggled;
        swEnabled.IsToggled = !swEnabled.IsToggled;
    }
}