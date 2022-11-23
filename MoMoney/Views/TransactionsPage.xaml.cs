using MoMoney.ViewModels;

namespace MoMoney.Views;

public partial class TransactionsPage : ContentPage
{
    TransactionsViewModel vm;

    public static EventHandler TabSelectionChanged;

    public TransactionsPage()
	{
		InitializeComponent();
        vm = (TransactionsViewModel)BindingContext;
        TabSelectionChanged += Refresh;
    }

    private async void Refresh(object sender, EventArgs s)
    {
        await Task.Delay(100);
        await vm.Refresh();
    }
}