using MoMoney.Core.ViewModels.Settings.Edit;

namespace MoMoney.Views.Settings.Edit;

public partial class EditCategoryPage : ContentPage
{
    public EditCategoryPage(EditCategoryViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    /// <summary>
    /// Clears input fields in view.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void BtnClear_Clicked(object sender, EventArgs e)
    {
        txtName.Text = "";
        pckParent.SelectedIndex = -1;
    }
}