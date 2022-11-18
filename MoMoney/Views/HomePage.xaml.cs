using MoMoney.ViewModels;
using Syncfusion.Maui.Charts;

namespace MoMoney.Views;

public partial class HomePage : ContentPage
{
    HomePageViewModel vm;

    public HomePage()
	{
		InitializeComponent();
        vm = (HomePageViewModel)BindingContext;
    }

	private void btnViewAllTransactions_Clicked(object sender, EventArgs e)
	{
		MainPage.TabView.SelectedIndex = 1;
    }

	private async void ContentPage_Loaded(object sender, EventArgs e)
	{
		await vm.Refresh();
	}
}