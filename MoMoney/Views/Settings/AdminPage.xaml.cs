using MoMoney.Components;
using MoMoney.Core.ViewModels.Settings;

namespace MoMoney.Views.Settings;

public partial class AdminPage : CustomContentPage
{
	public AdminPage(AdminViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}