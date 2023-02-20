using MoMoney.Models;
using MoMoney.Exceptions;

namespace MoMoney.Services;

public static class CategoryService
{
    public static Dictionary<int, string> Categories { get; set; } = new();

    /// <summary>
    /// Calls db Init.
    /// </summary>
    public static async Task Init()
    {
        await MoMoneydb.Init();
        if (!Categories.Any())
        {
            Categories = await GetCategoriesAsDictWithID();
        }
    }

    /// <summary>
    /// Creates new Category object and inserts into Category table.
    /// </summary>
    /// <param name="categoryName"></param>
    /// <param name="parentName"></param>
    /// <exception cref="DuplicateCategoryException"></exception>
    public static async Task AddCategory(string categoryName, string parentName)
    {
        await Init();

        var res = await MoMoneydb.db.Table<Category>().CountAsync(c => c.CategoryName == categoryName && c.ParentName == parentName);
        if (res > 0)
            throw new DuplicateCategoryException("Category named '" + categoryName + "' already exists");

        var category = new Category
        {
            CategoryName = categoryName,
            ParentName = string.IsNullOrEmpty(parentName) ? "" : parentName
        };

        // adds Category to db and dictionary
        await MoMoneydb.db.InsertAsync(category);
        Categories.Add(category.CategoryID, category.CategoryName);
    }

    /// <summary>
    /// Inserts multiple Category objects into Categories table.
    /// </summary>
    /// <param name="categories"></param>
    public static async Task AddCategories(List<Category> categories)
    {
        await Init();

        var dbCategories = await MoMoneydb.db.Table<Category>().ToListAsync(); // gets accounts from db
        // gets names of all categories where name matches any names of categories in parameter categories
        var cats = dbCategories.Select(a => a.CategoryName)
                             .Where(a1 => categories.Any(a2 => a1.Contains(a2.CategoryName))).ToList();

        // displays duplicate accounts on screen, if any
        string names = "";
        if (cats.Count() > 0)
        {
            names = cats[0];
            if (cats.Count() > 1)
            {
                for (int i = 1; i < cats.Count(); i++)
                    names += ", " + cats[i];
                names += " are duplicates, ";
            }
            else
                names += " is a duplicate, ";

            await Shell.Current.DisplayAlert("Attention", names + "all other categories were added", "OK");
        }

        categories.RemoveAll(a => cats.Contains(names));

        // adds Categories to db and dictionary
        await MoMoneydb.db.InsertAllAsync(categories);
        foreach (var cat in categories)
            Categories.Add(cat.CategoryID, cat.CategoryName);
    }

    /// <summary>
    /// Given an Category object, updates the corresponding category in the Categories table.
    /// </summary>
    /// <param name="updatedCategory"></param>
    public static async Task UpdateCategory(Category updatedCategory)
    {
        await Init();

        // updates Category in db and dictionary
        await MoMoneydb.db.UpdateAsync(updatedCategory);
        Categories[updatedCategory.CategoryID] = updatedCategory.CategoryName;
    }

    /// <summary>
    /// Removes Category from Categories table.
    /// </summary>
    /// <param name="ID"></param>
    public static async Task RemoveCategory(int ID)
    {
        await Init();

        // removes Category from db and dictionary
        await MoMoneydb.db.DeleteAsync<Category>(ID);
        Categories.Remove(ID);
    }

    /// <summary>
    /// Removes ALL Categories from Categories table.
    /// </summary>
    public static async Task RemoveAllCategories()
    {
        await Init();

        await MoMoneydb.db.DeleteAllAsync<Category>();
        await MoMoneydb.db.DropTableAsync<Category>();
        Categories.Clear();
    }

    /// <summary>
    /// Gets an category from the Categories table using an ID.
    /// </summary>
    /// <param name="id"></param>
    /// <returns>Category object</returns>
    public static async Task<Category>GetCategory(int id)
    {
        await Init();

        return await MoMoneydb.db.Table<Category>().FirstOrDefaultAsync(c => c.CategoryID == id);
    }

    /// <summary>
    /// Gets an category from the Categories table using a name and parent name.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="parent"></param>
    /// <returns>Category object</returns>
    public static async Task<Category> GetCategory(string name, string parent)
    {
        await Init();

        return await MoMoneydb.db.Table<Category>().FirstOrDefaultAsync(c => c.CategoryName == name && c.ParentName == parent);
    }

    /// <summary>
    /// Gets Category from the Categories table using name. Only works for parent categories.
    /// </summary>
    /// <param name="parentName"></param>
    /// <returns>Category object</returns>
    public static async Task<Category> GetParentCategory(string parentName)
    {
        await Init();

        return await MoMoneydb.db.Table<Category>().FirstOrDefaultAsync(c => c.CategoryName == parentName && c.ParentName == "");
    }

    /// <summary>
    /// Gets all Categories from Categories table as a list.
    /// </summary>
    /// <returns>List of Category objects</returns>
    public static async Task<IEnumerable<Category>> GetCategories()
    {
        await Init();

        return await MoMoneydb.db.Table<Category>().Where(c => c.CategoryID >= Constants.EXPENSE_ID).ToListAsync();
    }

    /// <summary>
    /// Gets all Categories from Categories table as a dictionary with concatenated Category/Subcategory names as Key.
    /// </summary>
    /// <returns>Dictionary of Category objects</returns>
    public static async Task<Dictionary<string, int>> GetCategoriesAsDictWithName()
    {
        await Init();

        var categories = await MoMoneydb.db.Table<Category>().ToListAsync();
        return categories.ToDictionary(c => c.CategoryName + "," + c.ParentName, c => c.CategoryID);
    }

    /// <summary>
    /// Gets all Categories from Categories table as a dictionary with Category ID as Key.
    /// </summary>
    /// <returns>Dictionary of Category objects</returns>
    public static async Task<Dictionary<int, string>> GetCategoriesAsDictWithID()
    {
        var categories = await MoMoneydb.db.Table<Category>().ToListAsync();
        var categoriesDict = categories.ToDictionary(c => c.CategoryID, c => c.CategoryName);
        return categoriesDict;
    }

    /// <summary>
    /// Gets all parent Categories from Categories table as a list.
    /// </summary>
    /// <returns>List of parent Category objects</returns>
    public static async Task<IEnumerable<Category>> GetAllParentCategories()
    {
        await Init();

        return await MoMoneydb.db.Table<Category>().Where(c => c.ParentName == "").ToListAsync();
    }

    /// <summary>
    /// Gets all parent Categories, minus transfers, from Categories table as a list.
    /// </summary>
    /// <returns>List of parent Category objects</returns>
    public static async Task<IEnumerable<Category>> GetParentCategories()
    {
        await Init();

        return await MoMoneydb.db.Table<Category>().Where(c => c.ParentName == "" &&
                                                          c.CategoryID != Constants.TRANSFER_ID).ToListAsync();
    }

    /// <summary>
    /// Gets all expense Categories from Categories table as a list.
    /// </summary>
    /// <returns>List of parent Category objects</returns>
    public static async Task<IEnumerable<Category>> GetExpenseCategories()
    {
        await Init();

        return await MoMoneydb.db.Table<Category>().Where(c => c.ParentName == "" &&
                                                          c.CategoryID >= Constants.EXPENSE_ID).ToListAsync();
    }

    /// <summary>
    /// Gets all parent Categories from Categories table as a list.
    /// </summary>
    /// <returns>List of parent Category objects</returns>
    public static async Task<IEnumerable<Category>> GetSubcategories(Category parentCategory)
    {
        await Init();

        return await MoMoneydb.db.Table<Category>().Where(c => c.ParentName == parentCategory.CategoryName).ToListAsync();
    }
}
