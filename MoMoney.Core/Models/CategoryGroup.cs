
namespace MoMoney.Core.Models;

public class CategoryGroup(Category parent, IEnumerable<Category> cat) : List<Category>(cat)
{
    public Category Parent { get; private set; } = parent;
}