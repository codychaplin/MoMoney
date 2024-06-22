using MoMoney.Core.ViewModels.Settings.Edit;

namespace MoMoney.Views.Settings.Edit;

public partial class EditAccountPage : ContentPage
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

    /// <summary>
    /// Clears input fields in view.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void BtnClear_Clicked(object sender, EventArgs e)
    {
        txtName.Text = "";
        pckType.SelectedIndex = -1;
        txtStartingBalance.Text = "";
    }
}