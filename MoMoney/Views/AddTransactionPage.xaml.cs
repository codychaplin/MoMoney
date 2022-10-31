namespace MoMoney.Views;

public partial class AddTransactionPage : ContentPage
{
	public AddTransactionPage()
	{
		InitializeComponent();
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
                btn.BackgroundColor = Color.FromArgb("404040");
            else
                btn.BackgroundColor = Color.FromArgb("212121");

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
}