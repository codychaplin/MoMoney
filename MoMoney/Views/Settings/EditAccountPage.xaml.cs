using MoMoney.ViewModels.Settings;

namespace MoMoney.Views.Settings;

public partial class EditAccountPage : ContentPage
{
    EditAccountViewModel vm;

    public EditAccountPage()
	{
		InitializeComponent();
        vm = (EditAccountViewModel)BindingContext;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await vm.GetAccount();
    }

    /// <summary>
    /// Clears input fields in view.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnClear_Clicked(object sender, EventArgs e)
	{
		txtName.Text = "";
        pckType.SelectedIndex = -1;
		txtStartingBalance.Text = "";
    }
}