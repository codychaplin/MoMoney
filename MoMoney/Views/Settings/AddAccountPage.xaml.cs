namespace MoMoney.Views.Settings;

public partial class AddAccountPage : ContentPage
{
	public AddAccountPage()
	{
		InitializeComponent();
		txtStartingBalance.Text = "";
	}

	private void btnClear_Clicked(object sender, EventArgs e)
	{
		txtName.Text = "";
        pckType.SelectedIndex = -1;
		txtStartingBalance.Text = "";
    }
}