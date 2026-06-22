using MoMoney.Components;
using MoMoney.Core.ViewModels.Settings.Edit;

namespace MoMoney.Views.Settings.Edit;

public partial class EditStockPage : CustomContentPage
{
    public EditStockPage(EditStockViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}