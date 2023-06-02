using MoMoney.ViewModels.Settings;

namespace MoMoney.Views.Settings;

public partial class AddAccountPage : ContentPage
{
	public AddAccountPage(AddAccountViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
		txtStartingBalance.Text = "";
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