using SQLite;

namespace MoMoney.Models;

public class Category
{
    [PrimaryKey, AutoIncrement]
    public int CategoryID { get; set; }
    public string CategoryName { get; set; }
    public string ParentName { get; set; }

    public Category() { }

    public Category(int categoryID, string categoryName, string parentName)
    {
        CategoryID = categoryID;
        CategoryName = categoryName;
        ParentName = parentName;
    }

    public Category(Category category)
    {
        CategoryID = category.CategoryID;
        CategoryName = category.CategoryName;
        ParentName = category.ParentName;
    }
}