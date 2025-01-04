
namespace MoMoney.Core.Models;

public class CategoryGroup(IGrouping<string, Category> cat) : List<Category>([.. cat])
{
    public string CategoryName { get; private set; } = cat.Key;
}