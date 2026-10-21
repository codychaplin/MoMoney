using System.Collections.ObjectModel;
using System.Windows.Input;
using MoMoney.Core.Models;

namespace MoMoney.Components;

public partial class PayeeField : ContentView
{
    public static readonly BindableProperty PayeesProperty =
        BindableProperty.Create(nameof(Payees), typeof(ObservableCollection<string>), typeof(PayeeField), null);

    public static readonly BindableProperty PayeeTextProperty =
        BindableProperty.Create(nameof(PayeeText), typeof(string), typeof(PayeeField), string.Empty, BindingMode.TwoWay);

    public static readonly BindableProperty AccountsProperty =
        BindableProperty.Create(nameof(Accounts), typeof(ObservableCollection<Account>), typeof(PayeeField), null);

    public static readonly BindableProperty TransferAccountProperty =
        BindableProperty.Create(nameof(TransferAccount), typeof(Account), typeof(PayeeField), null, BindingMode.TwoWay);

    public static readonly BindableProperty IsTransferAccountVisibleProperty =
        BindableProperty.Create(nameof(IsTransferAccountVisible), typeof(bool), typeof(PayeeField), false);

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

    public ObservableCollection<Account>? Accounts
    {
        get => (ObservableCollection<Account>?)GetValue(AccountsProperty);
        set => SetValue(AccountsProperty, value);
    }

    public Account? TransferAccount
    {
        get => (Account?)GetValue(TransferAccountProperty);
        set => SetValue(TransferAccountProperty, value);
    }

    public bool IsTransferAccountVisible
    {
        get => (bool)GetValue(IsTransferAccountVisibleProperty);
        set => SetValue(IsTransferAccountVisibleProperty, value);
    }

    public PayeeField()
    {
        InitializeComponent();
    }
}
