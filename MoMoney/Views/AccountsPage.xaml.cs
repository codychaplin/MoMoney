using MoMoney.ViewModels;

namespace MoMoney.Views;

public partial class AccountsPage : ContentPage
{
	AccountsViewModel vm;

    public AccountsPage()
	{
		InitializeComponent();
		vm = (AccountsViewModel)BindingContext;
	}

    protected override async void OnAppearing()
	{
		base.OnAppearing();

		await vm.Refresh();
    }

	private void ImageButton_Clicked(object sender, EventArgs e)
	{

	}
}