using MoMoney.ViewModels;

namespace MoMoney.Views;

public partial class TransactionsPage : ContentPage
{
    TransactionsViewModel vm;

    public TransactionsPage()
	{
		InitializeComponent();
        vm = (TransactionsViewModel)BindingContext;
    }

    private async void ContentPage_Loaded(object sender, EventArgs e)
    {
        await vm.Refresh();
    }
}