using System.Collections.ObjectModel;
using System.Windows.Input;
using MoMoney.Core.Models;

namespace MoMoney.Components;

public partial class TransactionFormView : ContentView
{
    // Date
    public static readonly BindableProperty DateProperty =
        BindableProperty.Create(nameof(Date), typeof(DateTime), typeof(TransactionFormView), DateTime.Today, BindingMode.TwoWay);

    // Account
    public static readonly BindableProperty AccountsProperty =
        BindableProperty.Create(nameof(Accounts), typeof(ObservableCollection<Account>), typeof(TransactionFormView), null);
    public static readonly BindableProperty AccountProperty =
        BindableProperty.Create(nameof(Account), typeof(Account), typeof(TransactionFormView), null, BindingMode.TwoWay);

    // Amount
    public static readonly BindableProperty AmountProperty =
        BindableProperty.Create(nameof(Amount), typeof(decimal?), typeof(TransactionFormView), null, BindingMode.TwoWay);

    // Category
    public static readonly BindableProperty CategoriesProperty =
        BindableProperty.Create(nameof(Categories), typeof(ObservableCollection<Category>), typeof(TransactionFormView), null);
    public static readonly BindableProperty CategoryProperty =
        BindableProperty.Create(nameof(Category), typeof(Category), typeof(TransactionFormView), null, BindingMode.TwoWay);
    public static readonly BindableProperty CategoryChangedCommandProperty =
        BindableProperty.Create(nameof(CategoryChangedCommand), typeof(ICommand), typeof(TransactionFormView), null);

    // Subcategory
    public static readonly BindableProperty SubcategoriesProperty =
        BindableProperty.Create(nameof(Subcategories), typeof(ObservableCollection<Category>), typeof(TransactionFormView), null);
    public static readonly BindableProperty SubcategoryProperty =
        BindableProperty.Create(nameof(Subcategory), typeof(Category), typeof(TransactionFormView), null, BindingMode.TwoWay);

    // Payee
    public static readonly BindableProperty PayeesProperty =
        BindableProperty.Create(nameof(Payees), typeof(ObservableCollection<string>), typeof(TransactionFormView), null);
    public static readonly BindableProperty PayeeTextProperty =
        BindableProperty.Create(nameof(PayeeText), typeof(string), typeof(TransactionFormView), string.Empty, BindingMode.TwoWay);

    // Transfer Account
    public static readonly BindableProperty TransferAccountProperty =
        BindableProperty.Create(nameof(TransferAccount), typeof(Account), typeof(TransactionFormView), null, BindingMode.TwoWay);

    // Flags
    public static readonly BindableProperty AreFieldsEnabledProperty =
        BindableProperty.Create(nameof(AreFieldsEnabled), typeof(bool), typeof(TransactionFormView), true);
    public static readonly BindableProperty IsCategoryEnabledProperty =
        BindableProperty.Create(nameof(IsCategoryEnabled), typeof(bool), typeof(TransactionFormView), false);
    public static readonly BindableProperty IsSubcategoryEnabledProperty =
        BindableProperty.Create(nameof(IsSubcategoryEnabled), typeof(bool), typeof(TransactionFormView), false);
    public static readonly BindableProperty IsPayeeVisibleProperty =
        BindableProperty.Create(nameof(IsPayeeVisible), typeof(bool), typeof(TransactionFormView), true);
    public static readonly BindableProperty IsTransferAccountVisibleProperty =
        BindableProperty.Create(nameof(IsTransferAccountVisible), typeof(bool), typeof(TransactionFormView), false);

    public DateTime Date
    {
        get => (DateTime)GetValue(DateProperty);
        set => SetValue(DateProperty, value);
    }

    public ObservableCollection<Account>? Accounts
    {
        get => (ObservableCollection<Account>?)GetValue(AccountsProperty);
        set => SetValue(AccountsProperty, value);
    }

    public Account? Account
    {
        get => (Account?)GetValue(AccountProperty);
        set => SetValue(AccountProperty, value);
    }

    public decimal? Amount
    {
        get => (decimal?)GetValue(AmountProperty);
        set => SetValue(AmountProperty, value);
    }

    public ObservableCollection<Category>? Categories
    {
        get => (ObservableCollection<Category>?)GetValue(CategoriesProperty);
        set => SetValue(CategoriesProperty, value);
    }

    public Category? Category
    {
        get => (Category?)GetValue(CategoryProperty);
        set => SetValue(CategoryProperty, value);
    }

    public ICommand? CategoryChangedCommand
    {
        get => (ICommand?)GetValue(CategoryChangedCommandProperty);
        set => SetValue(CategoryChangedCommandProperty, value);
    }

    public ObservableCollection<Category>? Subcategories
    {
        get => (ObservableCollection<Category>?)GetValue(SubcategoriesProperty);
        set => SetValue(SubcategoriesProperty, value);
    }

    public Category? Subcategory
    {
        get => (Category?)GetValue(SubcategoryProperty);
        set => SetValue(SubcategoryProperty, value);
    }

    public ObservableCollection<string>? Payees
    {
        get => (ObservableCollection<string>?)GetValue(PayeesProperty);
        set => SetValue(PayeesProperty, value);
    }

    public string PayeeText
    {
        get => (string)GetValue(PayeeTextProperty);
        set => SetValue(PayeeTextProperty, value);
    }

    public Account? TransferAccount
    {
        get => (Account?)GetValue(TransferAccountProperty);
        set => SetValue(TransferAccountProperty, value);
    }

    public bool AreFieldsEnabled
    {
        get => (bool)GetValue(AreFieldsEnabledProperty);
        set => SetValue(AreFieldsEnabledProperty, value);
    }

    public bool IsCategoryEnabled
    {
        get => (bool)GetValue(IsCategoryEnabledProperty);
        set => SetValue(IsCategoryEnabledProperty, value);
    }

    public bool IsSubcategoryEnabled
    {
        get => (bool)GetValue(IsSubcategoryEnabledProperty);
        set => SetValue(IsSubcategoryEnabledProperty, value);
    }

    public bool IsPayeeVisible
    {
        get => (bool)GetValue(IsPayeeVisibleProperty);
        set => SetValue(IsPayeeVisibleProperty, value);
    }
    
    public bool IsTransferAccountVisible
    {
        get => (bool)GetValue(IsTransferAccountVisibleProperty);
        set => SetValue(IsTransferAccountVisibleProperty, value);
    }

    public TransactionFormView()
    {
        InitializeComponent();
    }
}
