using MoMoney.ViewModels.Settings;

namespace MoMoney.Views.Settings;

public partial class CategoriesPage : ContentPage
{
	CategoriesViewModel vm;

    public CategoriesPage()
	{
		InitializeComponent();
		vm = (CategoriesViewModel)BindingContext;
	}

    protected override async void OnAppearing()
	{
		base.OnAppearing();

		await vm.Refresh();
    }
}