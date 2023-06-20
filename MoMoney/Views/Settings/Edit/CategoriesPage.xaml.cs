using MoMoney.ViewModels.Settings.Edit;

namespace MoMoney.Views.Settings.Edit;

public partial class CategoriesPage : ContentPage
{
    public CategoriesPage(CategoriesViewModel _vm)
    {
        InitializeComponent();
        BindingContext = _vm;
        NavigatedTo += _vm.Refresh;
    }
}