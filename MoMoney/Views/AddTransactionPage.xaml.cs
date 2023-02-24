using MoMoney.Models;
using MoMoney.ViewModels;

namespace MoMoney.Views;

public partial class AddTransactionPage : ContentView
{
    AddTransactionViewModel vm;

    public static EventHandler<EventArgs> UpdatePage { get; set; }

    public AddTransactionPage()
	{
		InitializeComponent();
        vm = (AddTransactionViewModel)BindingContext;

        // initialize fields
        dtDate.Date = DateTime.Today;
        txtAmount.Text = "";
        Allowed(false, false, false); // disable fields on start
        
        Loaded += vm.GetAccounts;
        UpdatePage += vm.GetAccounts;
    }

    /// <summary>
    /// enabled/disables input fields.
    /// </summary>
    /// <param name="flag"></param>
    /// <param name="category"></param>
    /// <param name="subcategory"></param>
    void Allowed(bool flag, bool category, bool subcategory)
    {
        dtDate.IsEnabled = flag;
        pckAccount.IsEnabled = flag;
        txtAmount.IsEnabled = flag;
        pckCategory.IsEnabled = category;
        pckSubcategory.IsEnabled = subcategory;
        txtPayee.IsEnabled = flag;
    }

    /// <summary>
    /// Highlights button, enables Income input fields, and populates corresponding Category picker.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void btnIncome_Clicked(object sender, EventArgs e)
    {
        ChangeButtonColour(sender as Button);

        Allowed(true, false, true);
        frPayee.IsVisible = true;
        frTransferTo.IsVisible = false;

        await vm.GetIncomeCategory();
        pckCategory.SelectedIndex = 0;
    }

    /// <summary>
    /// Highlights button, enables Expense input fields, and populates corresponding Category picker.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void btnExpense_Clicked(object sender, EventArgs e)
    {
        ChangeButtonColour(sender as Button);

        Allowed(true, true, true);
        frPayee.IsVisible = true;
        frTransferTo.IsVisible = false;

        await vm.GetExpenseCategories();
    }

    /// <summary>
    /// Highlights button, enables Transfer input fields, and populates corresponding Category picker.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void btnTransfer_Clicked(object sender, EventArgs e)
    {
        ChangeButtonColour(sender as Button);

        Allowed(true, false, false);

        frPayee.IsVisible = false;
        frTransferTo.IsVisible = true;

        await vm.GetTransferCategory();
        pckCategory.SelectedIndex = 0;
    }

    /// <summary>
    /// Changes selected button colour to dark gray and changes others to background colour.
    /// </summary>
    /// <param name="button"></param>
    void ChangeButtonColour(Button button)
    {
        foreach (Button btn in grdTransactionTypeButtons.Children.Cast<Button>())
        {
            if (btn == button)
                btn.BackgroundColor = Color.FromArgb("212121");
            else
                btn.BackgroundColor = Color.FromArgb("303030");
        }
    }

    /// <summary>
    /// Updates selected Category and subcategories for subcategory picker.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void pckCategory_SelectedIndexChanged(object sender, EventArgs e)
    {
        var parentCategory = (Category)pckCategory.SelectedItem;
        if (parentCategory != null)
        {
            pckSubcategory.IsEnabled = true;
            await vm.GetSubcategories(parentCategory);

            // if transfer, autoselect "Debit" as subcategory
            if (parentCategory.CategoryID == Constants.TRANSFER_ID)
                pckSubcategory.SelectedIndex = 0;
        }
    }

    /// <summary>
    /// Clears all input fields.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnClear_Clicked(object sender, EventArgs e)
    {
        Clear(false);
        vm.Categories.Clear();
        pckCategory.IsEnabled = false;
        pckSubcategory.IsEnabled = false;
        ChangeButtonColour(new Button());
    }

    /// <summary>
    /// Resets input fields on click.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnEnter_Clicked(object sender, EventArgs e)
    {
        Clear(true);
    }

    /// <summary>
    /// Resets input fields.
    /// </summary>
    void Clear(bool partial)
    {
        vm.Amount = 0;
        txtAmount.Text = "";
        vm.Payee = "";
        pckTransferTo.SelectedIndex = -1;

        if (partial) return;

        dtDate.Date = DateTime.Now;
        pckAccount.SelectedIndex = -1;
        vm.Subcategories.Clear();
        frPayee.IsVisible = true;
        frTransferTo.IsVisible = false;
    }
}