using CommunityToolkit.Mvvm.Messaging;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.ViewModels;
using UraniumUI.Material.Controls;

namespace MoMoney.Views;

public partial class AddTransactionPage : ContentView
{
    AddTransactionViewModel vm;

    public AddTransactionPage()
    {
        InitializeComponent();

        HandlerChanged += (s, e) =>
        {
            vm = Handler.MauiContext.Services.GetService<AddTransactionViewModel>();
            BindingContext = vm;

            Loaded += vm.GetPayees;
            Loaded += vm.GetAccounts;

            // register to receive messages when accounts and categories are updated
            WeakReferenceMessenger.Default.Register<UpdateAccountsMessage>(this, (r, m) => { vm.GetAccounts(r, default); });
            WeakReferenceMessenger.Default.Register<UpdateCategoriesMessage>(this, (r, m) =>
            {
                if (vm.transactionType == TransactionType.Income)
                    vm.GetIncomeCategoryCommand.Execute(r);
                else if (vm.transactionType == TransactionType.Expense)
                    vm.GetExpenseCategoriesCommand.Execute(r);
                else if (vm.transactionType == TransactionType.Transfer)
                    vm.GetTransferCategoryCommand.Execute(r);
            });

            // initialize fields
            dtpDate.Date = DateTime.Today;
            txtAmount.ClearValue(TextField.TextProperty);
            pckCategory.ClearValue(PickerField.SelectedItemProperty);
            pckSubcategory.ClearValue(PickerField.SelectedItemProperty);
            EnableEntries(false, false, false); // disable fields on start
            vm.transactionType = TransactionType.None;
        };
    }

    /// <summary>
    /// Highlights Income button, enables income input fields, and shows Payee field.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnIncome_Clicked(object sender, EventArgs e)
    {
        ChangeButtonColour(sender as Button);
        pckSubcategory.ClearValue(PickerField.SelectedItemProperty);
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
        pckCategory.ClearValue(PickerField.SelectedItemProperty);
        pckSubcategory.ClearValue(PickerField.SelectedItemProperty);
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
                if (Application.Current.Resources.TryGetValue("Primary", out var primary))
                    btn.Background = (Color)primary;
                else
                    btn.Background = Color.FromArgb("#42ba96");
            }
            else
            {
                if (Application.Current.RequestedTheme == AppTheme.Dark)
                {
                    if (Application.Current.Resources.TryGetValue("Gray900", out var gray))
                        btn.Background = (Color)gray;
                    else
                        btn.Background = Color.FromArgb("#212121");
                }
                else
                {
                    if (Application.Current.Resources.TryGetValue("Gray200", out var gray))
                        btn.Background = (Color)gray;
                    else
                        btn.Background = Color.FromArgb("#C8C8C8");
                }
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
        entPayee.IsVisible = payee;
        pckTransferAccount.IsVisible = !payee;
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
        txtAmount.ClearValue();
        entPayee.SelectedText = null;
        entPayee.Text = "";
        MakePayeeVisible(true);
    }

    private void btnEnter_Clicked(object sender, EventArgs e)
    {
        vm.ClearAfterAdd();
        txtAmount.Text = "";
    }
}