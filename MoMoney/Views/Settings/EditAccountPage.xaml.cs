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

    private void btnClear_Clicked(object sender, EventArgs e)
	{
		txtName.Text = "";
        pckType.SelectedIndex = -1;
		txtStartingBalance.Text = "";
    }
}