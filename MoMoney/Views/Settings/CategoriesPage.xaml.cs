using MoMoney.ViewModels.Settings;

namespace MoMoney.Views.Settings;

public partial class CategoriesPage : ContentPage
{
	CategoriesViewModel vm;

	public CategoriesPage()
	{
		InitializeComponent();
		vm = (CategoriesViewModel)BindingContext;
		NavigatedTo += vm.Refresh;
	}
}