using Syncfusion.Maui.ListView;

namespace MoMoney.Selectors;

class LogDataTemplateSelector : DataTemplateSelector
{
    public DataTemplate SmallTemplate { get; set; }
    public DataTemplate LargeTemplate { get; set; }
    public SfListView ListView { get; set; }
    public Switch Switch { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        if (Switch.IsToggled)
        {
            ListView.ItemSize = 60;
            return LargeTemplate;
        }
        else
        {
            ListView.ItemSize = 40;
            return SmallTemplate;
        }
    }
}
