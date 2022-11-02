using Syncfusion.Maui.TabView;

namespace MoMoney.Views;

public partial class MainPage : ContentPage
{
	public static SfTabView TabView { get; private set; }

	public MainPage()
	{
		InitializeComponent();
		TabView = tvTabBar;
	}
}