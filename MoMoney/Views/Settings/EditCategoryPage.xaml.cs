using MoMoney.Models;
using MoMoney.ViewModels.Settings;

namespace MoMoney.Views.Settings;

public partial class EditCategoryPage : ContentPage
{
    EditCategoryViewModel vm;

    public EditCategoryPage()
	{
		InitializeComponent();

        vm = (EditCategoryViewModel)BindingContext;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await vm.GetParents();
        await vm.GetCategory();

        // sets value in picker, if not null
        if (vm.Parent != null)
        {
            pckParent.SelectedItem = vm.Parent;
            for (int i = 0; i < pckParent.Items.Count; i++)
            {
                if (pckParent.Items[i] == vm.Parent.CategoryName)
                {
                    pckParent.SelectedIndex = i;
                    break;
                }
            }
        }
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