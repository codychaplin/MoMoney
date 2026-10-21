namespace MoMoney.Components;

public class CustomContentPage : ContentPage
{
    private readonly PageHeader _header;
    private readonly Grid _grid;
    private readonly ActivityIndicator _activityIndicator;

    public static readonly BindableProperty HeaderTitleProperty =
        BindableProperty.Create(nameof(HeaderTitle), typeof(string), typeof(CustomContentPage), string.Empty,
            propertyChanged: (b, _, v) => ((CustomContentPage)b)._header.Title = (string)v);

    public static readonly BindableProperty ShowBackButtonProperty =
        BindableProperty.Create(nameof(ShowBackButton), typeof(bool), typeof(CustomContentPage), true,
            propertyChanged: (b, _, v) => ((CustomContentPage)b)._header.ShowBackButton = (bool)v);

    public static readonly BindableProperty HeaderRightContentProperty =
        BindableProperty.Create(nameof(HeaderRightContent), typeof(View), typeof(CustomContentPage), null,
            propertyChanged: (b, _, v) => ((CustomContentPage)b)._header.RightContent = (View?)v);

    public string HeaderTitle
    {
        get => (string)GetValue(HeaderTitleProperty);
        set => SetValue(HeaderTitleProperty, value);
    }

    public bool ShowBackButton
    {
        get => (bool)GetValue(ShowBackButtonProperty);
        set => SetValue(ShowBackButtonProperty, value);
    }

    public static readonly BindableProperty IsBusyOverlayProperty =
        BindableProperty.Create(nameof(IsBusyOverlay), typeof(bool), typeof(CustomContentPage), false,
            propertyChanged: (b, _, v) =>
            {
                var page = (CustomContentPage)b;
                page._activityIndicator.IsVisible = (bool)v;
                page._activityIndicator.IsRunning = (bool)v;
            });

    public View? HeaderRightContent
    {
        get => (View?)GetValue(HeaderRightContentProperty);
        set => SetValue(HeaderRightContentProperty, value);
    }

    public bool IsBusyOverlay
    {
        get => (bool)GetValue(IsBusyOverlayProperty);
        set => SetValue(IsBusyOverlayProperty, value);
    }

    public CustomContentPage()
    {
        Shell.SetNavBarIsVisible(this, false);
        _header = new PageHeader();
        _activityIndicator = new ActivityIndicator {
            IsVisible = false,
            IsRunning = false,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center
        };
        _grid = new Grid
        {
            RowDefinitions = [new(GridLength.Auto), new(GridLength.Star)]
        };
        _grid.Add(_header, 0, 0);
        _grid.Children.Add(_activityIndicator);
        Grid.SetRowSpan(_activityIndicator, 2);
        Content = _grid;
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == ContentProperty.PropertyName && Content != _grid && Content is View view)
        {
            Grid.SetRow(view, 1);
            _grid.Children.Add(view);
            Content = _grid;
        }
    }
}
