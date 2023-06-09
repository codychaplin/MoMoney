using MoMoney.ViewModels;

namespace MoMoney.Views;

public partial class AddTransactionPage : ContentView
{
    AddTransactionViewModel vm;

    public static EventHandler<EventArgs> UpdatePage { get; set; }

    public AddTransactionPage(AddTransactionViewModel _vm)
	{
		InitializeComponent();
        vm = _vm;
        BindingContext = vm;

        // initialize fields
        dtpDate.Date = DateTime.Today;
        txtAmount.Text = "";
        EnableEntries(false, false, false); // disable fields on start
        
        Loaded += vm.GetPayees;
        Loaded += vm.GetAccounts;
        UpdatePage += vm.GetAccounts;
        pckCategory.SelectedIndexChanged += vm.CategoryChanged;
    }

    /// <summary>
    /// Highlights Income button, enables income input fields, and shows Payee field.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnIncome_Clicked(object sender, EventArgs e)
    {
        ChangeButtonColour(sender as Button);
        EnableEntries(false, true, true);
        MakePayeeVisible(true);
    }

    /// <summary>
    /// Highlights Expense button, enables expense input fields, and shows Payee field.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnExpense_Clicked(object sender, EventArgs e)
    {
        ChangeButtonColour(sender as Button);
        EnableEntries(true, true, true);
        MakePayeeVisible(true);
    }

    /// <summary>
    /// Highlights Transfer button, enables transfer input fields, and shows TransferAccount field.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnTransfer_Clicked(object sender, EventArgs e)
    {
        ChangeButtonColour(sender as Button);
        EnableEntries(false, false, true);
        MakePayeeVisible(false);
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
            {
                if (Application.Current.Resources.TryGetValue("Green", out var green))
                    btn.BackgroundColor = (Color)green;
                else
                    btn.BackgroundColor = Color.FromArgb("#42ba96");
            }    
            else
            {
                if (Application.Current.Resources.TryGetValue("Gray900", out var gray))
                    btn.BackgroundColor = (Color)gray;
                else
                    btn.BackgroundColor = Color.FromArgb("#212121");
            }
        }
    }

    /// <summary>
    /// enables/disables input fields.
    /// </summary>
    /// <param name="category"></param>
    /// <param name="subcategory"></param>
    /// <param name="other"></param>
    void EnableEntries(bool category, bool subcategory, bool other)
    {
        dtpDate.IsEnabled = other;
        pckAccount.IsEnabled = other;
        txtAmount.IsEnabled = other;
        pckCategory.IsEnabled = category;
        pckSubcategory.IsEnabled = subcategory;
        entPayee.IsEnabled = other;
        pckTransferAccount.IsEnabled = other;
    }

    void MakePayeeVisible(bool payee)
    {
        frPayee.IsVisible = payee;
        frTransferAccount.IsVisible = !payee;
    }

    /// <summary>
    /// Clears all input fields.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnClear_Clicked(object sender, EventArgs e)
    {
        Clear();
        EnableEntries(false, false, false);
        ChangeButtonColour(new Button());
    }

    /// <summary>
    /// Clears binded values and clears/shows payee entry.
    /// </summary>
    void Clear()
    {
        vm.Clear();
        entPayee.SelectedItem = null;
        MakePayeeVisible(true);
    }
}