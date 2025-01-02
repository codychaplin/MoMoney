using CheckBox = UraniumUI.Material.Controls.CheckBox;

namespace MoMoney.Selectors;

class LogDataTemplateSelector : DataTemplateSelector
{
    public DataTemplate SmallTemplate { get; set; }
    public DataTemplate LargeTemplate { get; set; }
    public CheckBox CheckBox { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        if (CheckBox.IsChecked)
        {
            return LargeTemplate;
        }
        else
        {
            return SmallTemplate;
        }
    }
}
