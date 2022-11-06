using MoMoney.Models;
using MoMoney.ViewModels;

namespace MoMoney.Views;

public partial class AddTransactionPage : ContentPage
{
    AddTransactionViewModel vm;

	public AddTransactionPage()
	{
		InitializeComponent();
        vm = (AddTransactionViewModel)BindingContext;
    }

    /// <summary>
    /// Updates subcategories for picker
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void pckCategory_SelectedIndexChanged(object sender, EventArgs e)
    {
        var parentCategory = (Category)pckCategory.SelectedItem;
        await vm.GetSubcategories(parentCategory);
    }

    private void btnIncome_Clicked(object sender, EventArgs e)
	{
        ChangeButtonColour(sender as Button);
    }

	private void btnExpense_Clicked(object sender, EventArgs e)
	{
        ChangeButtonColour(sender as Button);
    }

	private void btnTransfer_Clicked(object sender, EventArgs e)
	{
        ChangeButtonColour(sender as Button);
    }

    /// <summary>
    /// Changes selected button colour to light gray and changes others to background colour
    /// </summary>
    /// <param name="button"></param>
    void ChangeButtonColour(Button button)
    {
        foreach (Button btn in grdTransactionTypeButtons.Children)
        {
            if (btn == button)
                btn.BackgroundColor = Color.FromArgb("212121");
            else
                btn.BackgroundColor = Color.FromArgb("303030");

        }
    }

    private void btnClear_Clicked(object sender, EventArgs e)
    {
        // clear all fields
        dtDate.Date = DateTime.Now;
        pckAccount.SelectedItem = null;
        txtAmount.Text = "";
        pckCategory.SelectedItem = null;
        pckSubcategory.SelectedItem = null;
        txtPayee.Text = "";
    }

    private void btnEnter_Clicked(object sender, EventArgs e)
    {
        // enter transaction into database
    }

    private async void ContentPage_Loaded(object sender, EventArgs e)
    {
        await vm.Refresh();
    }
}