using SQLite;
using CsvHelper.Configuration.Attributes;

namespace MoMoney.Core.Models;

public class Category
{
    [PrimaryKey, AutoIncrement, CsvHelper.Configuration.Attributes.Ignore]
    public int CategoryID { get; set; }
    [Index(0)]
    public string CategoryName { get; set; } = string.Empty;
    [Index(1)]
    public string ParentName { get; set; } = string.Empty;

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