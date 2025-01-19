using CheckBox = UraniumUI.Material.Controls.CheckBox;

namespace MoMoney.Selectors;

class LogDataTemplateSelector : DataTemplateSelector
{
    public required DataTemplate SmallTemplate { get; set; }
    public required DataTemplate LargeTemplate { get; set; }
    public CheckBox? CheckBox { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        if (CheckBox != null && CheckBox.IsChecked)
        {
            return LargeTemplate;
        }
        else
        {
            return SmallTemplate;
        }
    }
}
