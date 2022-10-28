using Microsoft.Maui.Controls;

namespace MoMoney;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		TabBar.CurrentItem = HomePage;
	}
}
