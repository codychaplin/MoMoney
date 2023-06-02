using MoMoney.ViewModels;

namespace MoMoney.Views;

public partial class EditTransactionPage : ContentPage
{
    EditTransactionViewModel vm;

	public EditTransactionPage(EditTransactionViewModel _vm)
	{
		InitializeComponent();
        vm = _vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // initialize ViewModel
        await vm.GetTransaction();
        await vm.GetAccounts();
        await vm.GetPayees();

        // get corresponding categories
        switch (vm.InitialCategory.CategoryID)
        {
            case Constants.INCOME_ID:
                await vm.GetIncomeCategory();
                break;
            case Constants.TRANSFER_ID:
                await vm.GetTransferCategory();
                break;
            default:
                await vm.GetExpenseCategories();
                break;
        }

        // update SelectedIndex
        pckAccount.SelectedIndex = pckAccount.Items.IndexOf(vm.InitialAccount.AccountName);
        pckCategory.SelectedIndex = pckCategory.Items.IndexOf(vm.InitialCategory.CategoryName);
        await Task.Delay(200); // delay needed to give 'pckCategory_SelectedIndexChanged' time to process
        pckSubcategory.SelectedIndex = pckSubcategory.Items.IndexOf(vm.InitialSubcategory.CategoryName);

        // if income, disable category change
        // if transfer, select index, disable category/subcategory change, and make frPayeeAccount visible
        if (vm.InitialCategory.CategoryID == Constants.INCOME_ID)
        {
            pckCategory.IsEnabled = false;
        }
        else if (vm.InitialCategory.CategoryID == Constants.TRANSFER_ID)
        {
            pckPayeeAccount.SelectedIndex = pckPayeeAccount.Items.IndexOf(vm.InitialPayeeAccount.AccountName);
            pckCategory.IsEnabled = false;
            pckSubcategory.IsEnabled = false;
            frPayee.IsVisible = false;
            frPayeeAccount.IsVisible = true;
        }
    }

    /// <summary>
    /// Updates Subcategories for Subcategory picker.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void pckCategory_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (vm.Category != null)
            await vm.GetSubcategories(vm.Category);
    }

    /// <summary>
    /// Clears all input fields.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnClear_Clicked(object sender, EventArgs e)
    {
        Clear();
    }

    /// <summary>
    /// Resets input fields on click.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnEnter_Clicked(object sender, EventArgs e)
    {
        Clear();
    }

    /// <summary>
    /// Resets input fields.
    /// </summary>
    void Clear()
    {
        if (vm.Category != null && vm.Category.CategoryID == Constants.TRANSFER_ID)
        {
            pckCategory.IsEnabled = false;
            pckSubcategory.IsEnabled = false;
            return;
        }

        pckCategory.IsEnabled = true;
        pckSubcategory.IsEnabled = true;
        pckCategory.SelectedIndex = -1;
        pckSubcategory.SelectedIndex = -1;

        // reset payee in order to clear text in Payee SfAutocomplete
        vm.Transaction.Payee = "";
        vm.Transaction = new();
        dtDate.Date = DateTime.Now;
        pckAccount.SelectedIndex = -1;
        pckPayeeAccount.SelectedIndex = -1;
    }
}