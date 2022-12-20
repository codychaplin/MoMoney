using MoMoney.ViewModels;

namespace MoMoney.Views;

public partial class StatsPage : ContentView
{
    StatsPageViewModel vm;

    public static EventHandler AccountsChanged;

    public StatsPage()
	{
		InitializeComponent();
        vm = (StatsPageViewModel)BindingContext;
        AccountsChanged += Refresh;
    }

    private async void Refresh(object s, EventArgs e)
    {
        await vm.getAccounts();
    }
}