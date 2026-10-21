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

    public static readonly BindableProperty ClearIsEnabledProperty = BindableProperty.Create(
        nameof(ClearIsEnabled), typeof(bool), typeof(ActionButtons), true);

    public static readonly BindableProperty RemoveIsEnabledProperty = BindableProperty.Create(
        nameof(RemoveIsEnabled), typeof(bool), typeof(ActionButtons), true);

    public static readonly BindableProperty AddIsEnabledProperty = BindableProperty.Create(
        nameof(AddIsEnabled), typeof(bool), typeof(ActionButtons), true);

    public static readonly BindableProperty EditIsEnabledProperty = BindableProperty.Create(
        nameof(EditIsEnabled), typeof(bool), typeof(ActionButtons), true);

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

    public bool ClearIsEnabled
    {
        get => (bool)GetValue(ClearIsEnabledProperty);
        set => SetValue(ClearIsEnabledProperty, value);
    }

    public bool RemoveIsEnabled
    {
        get => (bool)GetValue(RemoveIsEnabledProperty);
        set => SetValue(RemoveIsEnabledProperty, value);
    }

    public bool AddIsEnabled
    {
        get => (bool)GetValue(AddIsEnabledProperty);
        set => SetValue(AddIsEnabledProperty, value);
    }

    public bool EditIsEnabled
    {
        get => (bool)GetValue(EditIsEnabledProperty);
        set => SetValue(EditIsEnabledProperty, value);
    }
}