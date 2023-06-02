using MoMoney.ViewModels.Settings;

namespace MoMoney.Views.Settings;

public partial class CategoriesPage : ContentPage
{
	public CategoriesPage(CategoriesViewModel _vm)
	{
		InitializeComponent();
		BindingContext = _vm;
		NavigatedTo += _vm.Refresh;
	}
}