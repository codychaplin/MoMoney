using MoMoney.Core.ViewModels;

namespace MoMoney.Components;

public partial class ColourPopup : ContentView
{
	public ColourPopup(ColourPopupViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}