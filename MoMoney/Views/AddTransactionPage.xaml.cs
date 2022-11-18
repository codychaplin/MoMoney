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
        dtDate.Date = DateTime.Today;
        Allowed(false); // disable fields on start
    }

    /// <summary>
    /// enabled/disables input fields
    /// </summary>
    /// <param name="flag"></param>
    void Allowed(bool flag)
    {
        dtDate.IsEnabled = flag;
        pckAccount.IsEnabled = flag;
        txtAmount.IsEnabled = flag;
        pckCategory.IsEnabled = flag;
        pckSubcategory.IsEnabled = flag;
        txtPayee.IsEnabled = flag;
    }

    /// <summary>
    /// Fill Account picker with Accounts from database
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void ContentPage_Loaded(object sender, EventArgs e)
    {
        await vm.GetAccounts();
    }

    /// <summary>
    /// Highlights button, enables input fields, and populates corresponding Category picker
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void btnIncome_Clicked(object sender, EventArgs e)
    {
        ChangeButtonColour(sender as Button);

        Allowed(true);
        frPayee.IsVisible = true;
        frTransferTo.IsVisible = false;

        await vm.GetIncomeCategory();
        pckCategory.SelectedIndex = 0;
    }

    /// <summary>
    /// Highlights button, enables input fields, and populates corresponding Category picker
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void btnExpense_Clicked(object sender, EventArgs e)
    {
        ChangeButtonColour(sender as Button);

        Allowed(true);
        frPayee.IsVisible = true;
        frTransferTo.IsVisible = false;

        await vm.GetExpenseCategories();
    }

    /// <summary>
    /// Highlights button, enables input fields, and populates corresponding Category picker
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void btnTransfer_Clicked(object sender, EventArgs e)
    {
        ChangeButtonColour(sender as Button);

        Allowed(true);
        frPayee.IsVisible = false;
        frTransferTo.IsVisible = true;

        await vm.GetTransferCategory();
        pckCategory.SelectedIndex = 0;
    }

    /// <summary>
    /// Changes selected button colour to dark gray and changes others to background colour
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

    /// <summary>
    /// Updates selected Account
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void pckAccount_SelectedIndexChanged(object sender, EventArgs e)
    {
        var account = (Account)pckAccount.SelectedItem;
        vm.AccountID = account != null ? account.AccountID : 0;
    }

    /// <summary>
    /// Updates selected Transfer Account
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void pckTransferTo_SelectedIndexChanged(object sender, EventArgs e)
    {
        var account = (Account)pckTransferTo.SelectedItem;
        vm.TransferID = account != null ? account.AccountID : 0;
    }

    /// <summary>
    /// Updates selected Category and subcategories for subcategory picker
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void pckCategory_SelectedIndexChanged(object sender, EventArgs e)
    {
        var parentCategory = (Category)pckCategory.SelectedItem;
        if (parentCategory != null)
        {
            vm.CategoryID = parentCategory.CategoryID;
            pckSubcategory.IsEnabled = true;
            await vm.GetSubcategories(parentCategory);

            // if transfer, autoselect "Debit" as subcategory
            if (parentCategory.CategoryID == Constants.TRANSFER_ID)
                pckSubcategory.SelectedIndex = 0;
        }
        else
            vm.CategoryID = 0;
    }

    /// <summary>
    /// Updates selected Subcategory
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void pckSubcategory_SelectedIndexChanged(object sender, EventArgs e)
    {
        var subcategory = (Category)pckSubcategory.SelectedItem;
        vm.SubcategoryID = subcategory != null ? subcategory.CategoryID : 0;
    }

    /// <summary>
    /// Clears all input fields
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnClear_Clicked(object sender, EventArgs e)
    {
        Clear();
        pckCategory.IsEnabled = false;
        pckSubcategory.IsEnabled = false;
        ChangeButtonColour(new Button());
    }

    /// <summary>
    /// Resets input fields on click
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnEnter_Clicked(object sender, EventArgs e)
    {
        Clear();
    }

    /// <summary>
    /// Resets input fields
    /// </summary>
    void Clear()
    {
        dtDate.Date = DateTime.Now;
        pckAccount.SelectedItem = null;
        txtAmount.Text = "";
        txtPayee.Text = "";
        vm.Amount = 0;
        vm.Categories.Clear();
        vm.Subcategories.Clear();
    }
}