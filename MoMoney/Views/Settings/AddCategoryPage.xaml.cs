using MoMoney.ViewModels.Settings;

namespace MoMoney.Views.Settings;

public partial class AddCategoryPage : ContentPage
{
    AddCategoryViewModel vm;

	public AddCategoryPage()
	{
		InitializeComponent();

        vm = (AddCategoryViewModel)BindingContext;
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