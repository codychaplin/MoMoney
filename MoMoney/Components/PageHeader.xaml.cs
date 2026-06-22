namespace MoMoney.Components;

public partial class PageHeader : Grid
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(PageHeader), string.Empty);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly BindableProperty RightContentProperty =
        BindableProperty.Create(nameof(RightContent), typeof(View), typeof(PageHeader), null,
            propertyChanged: (bindable, _, newValue) =>
            {
                var header = (PageHeader)bindable;
                header.RightContentContainer.Content = (View?)newValue;
            });

    public View? RightContent
    {
        get => (View?)GetValue(RightContentProperty);
        set => SetValue(RightContentProperty, value);
    }

    public static readonly BindableProperty ShowBackButtonProperty =
        BindableProperty.Create(nameof(ShowBackButton), typeof(bool), typeof(PageHeader), true);

    public bool ShowBackButton
    {
        get => (bool)GetValue(ShowBackButtonProperty);
        set => SetValue(ShowBackButtonProperty, value);
    }

    public PageHeader()
    {
        InitializeComponent();
    }

    private async void OnBackButtonTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
