using MoMoney.ViewModels.Settings;

namespace MoMoney.Views.Settings;

public partial class AdminPage : ContentPage
{
	public AdminPage(AdminViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}