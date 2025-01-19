using System.Windows.Input;

namespace MoMoney.Components;

public partial class ActionButtons : Grid
{
    public ActionButtons()
    {
        InitializeComponent();
    }

    public static readonly BindableProperty IsEditModeProperty = BindableProperty.Create(
        nameof(IsEditMode), typeof(bool), typeof(ActionButtons), false);

    public static readonly BindableProperty ClearCommandProperty = BindableProperty.Create(
        nameof(ClearCommand), typeof(ICommand), typeof(ActionButtons));

    public static readonly BindableProperty RemoveCommandProperty = BindableProperty.Create(
        nameof(RemoveCommand), typeof(ICommand), typeof(ActionButtons));

    public static readonly BindableProperty AddCommandProperty = BindableProperty.Create(
        nameof(AddCommand), typeof(ICommand), typeof(ActionButtons));

    public static readonly BindableProperty EditCommandProperty = BindableProperty.Create(
        nameof(EditCommand), typeof(ICommand), typeof(ActionButtons));

    public bool IsEditMode
    {
        get => (bool)GetValue(IsEditModeProperty);
        set => SetValue(IsEditModeProperty, value);
    }

    public ICommand ClearCommand
    {
        get => (ICommand)GetValue(ClearCommandProperty);
        set => SetValue(ClearCommandProperty, value);
    }

    public ICommand RemoveCommand
    {
        get => (ICommand)GetValue(RemoveCommandProperty);
        set => SetValue(RemoveCommandProperty, value);
    }

    public ICommand AddCommand
    {
        get => (ICommand)GetValue(AddCommandProperty);
        set => SetValue(AddCommandProperty, value);
    }

    public ICommand EditCommand
    {
        get => (ICommand)GetValue(EditCommandProperty);
        set => SetValue(EditCommandProperty, value);
    }

    public event EventHandler? ClearClicked;

    private void BtnClear_Clicked(object sender, EventArgs e)
    {
        ClearClicked?.Invoke(this, EventArgs.Empty);
    }
}