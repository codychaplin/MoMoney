using SQLite;
using CsvHelper.Configuration.Attributes;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MoMoney.Core.Models;

public partial class Category : ObservableObject
{
    [PrimaryKey, AutoIncrement, CsvHelper.Configuration.Attributes.Ignore]
    public int CategoryID { get; set; }
    [Index(0), ObservableProperty]
    public partial string CategoryName { get; set; } = string.Empty;
    [CsvHelper.Configuration.Attributes.Ignore]
    public int? ParentCategoryID{ get; set; }
    [Index(1), SQLite.Ignore]
    public string ParentName { get; set; } = string.Empty;
    [Index(2), ObservableProperty]
    public partial string Colour { get; set; } = string.Empty;

    public Category() { }

    public Category(int categoryID, string categoryName, int? parentCategoryID, string colour)
    {
        CategoryID = categoryID;
        CategoryName = categoryName;
        ParentCategoryID = parentCategoryID;
        Colour = colour;
    }

    public Category(Category category)
    {
        CategoryID = category.CategoryID;
        CategoryName = category.CategoryName;
        ParentCategoryID = category.ParentCategoryID;
        Colour = category.Colour;
    }
}