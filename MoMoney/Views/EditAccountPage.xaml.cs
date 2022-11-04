using MoMoney.Services;

namespace MoMoney.Views;

[QueryProperty(nameof(ID), nameof(ID))]
public partial class EditAccountPage : ContentPage
{
    public string ID { get; set; }

    public EditAccountPage()
	{
		InitializeComponent();
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // converts string ID parameter to int, gets Account using id, sets that as BindingContext
        int.TryParse(ID, out int id);
        BindingContext = await AccountService.GetAccount(id);
    }

    private void btnClear_Clicked(object sender, EventArgs e)
	{
		txtName.Text = "";
        pckType.SelectedIndex = -1;
		txtStartingBalance.Text = "";
    }

    private async void btnEnter_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}