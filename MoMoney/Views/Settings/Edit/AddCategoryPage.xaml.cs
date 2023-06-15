using MoMoney.ViewModels.Settings.Edit;

namespace MoMoney.Views.Settings.Edit;

public partial class AddCategoryPage : ContentPage
{
    AddCategoryViewModel vm;

    public AddCategoryPage(AddCategoryViewModel _vm)
    {
        InitializeComponent();
        vm = _vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await vm.GetParents();
    }

    /// <summary>
    /// Clears input fields in view.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnClear_Clicked(object sender, EventArgs e)
    {
        txtName.Text = "";
        pckParent.SelectedIndex = -1;
    }
}