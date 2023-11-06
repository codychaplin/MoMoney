
namespace MoMoney.Core.Models;

public class CategoryGroup : List<Category>
{
    public string CategoryName { get; private set; }

    public CategoryGroup(IGrouping<string, Category> cat) : base(cat.ToList())
    {
        CategoryName = cat.Key;
    }
}