using MoMoney.Components;
using MoMoney.Core.ViewModels.Settings.Edit;

namespace MoMoney.Views.Settings.Edit;

public partial class EditCategoryPage : CustomContentPage
{
    public EditCategoryPage(EditCategoryViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}