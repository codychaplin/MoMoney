using CheckBox = UraniumUI.Material.Controls.CheckBox;

namespace MoMoney.Selectors;

class LogDataTemplateSelector : DataTemplateSelector
{
    public DataTemplate SmallTemplate { get; set; } = new();
    public DataTemplate LargeTemplate { get; set; } = new();
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
