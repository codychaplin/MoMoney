using MoMoney.Models;
using MoMoney.Exceptions;

namespace MoMoney.Services;

public static class CategoryService
{
    public static Dictionary<int, Category> Categories { get; set; } = new();

    /// <summary>
    /// Calls db Init.
    /// </summary>
    static async Task Init()
    {
        await MoMoneydb.Init();

        if (!Categories.Any())
            Categories = await GetCategoriesAsDict();
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
            throw new DuplicateCategoryException("Category '" + categoryName + "' already exists");

        var category = new Category
        {
            CategoryName = categoryName,
            ParentName = string.IsNullOrEmpty(parentName) ? "" : parentName
        };

        // adds Category to db and dictionary
        await MoMoneydb.db.InsertAsync(category);
        Categories.Add(category.CategoryID, category);
    }

    /// <summary>
    /// Inserts multiple Category objects into Categories table.
    /// </summary>
    /// <param name="categories"></param>
    /// <exception cref="DuplicateCategoryException"></exception>
    public static async Task AddCategories(List<Category> categories)
    {
        await Init();

        // gets accounts from db
        var dbCategories = await MoMoneydb.db.Table<Category>().ToListAsync();

        // gets names of all categories where name matches any names of categories in parameter categories
        bool containsDuplicates = categories.Any(a =>
             dbCategories.Any(dba => dba.CategoryName == a.CategoryName && dba.ParentName == a.ParentName));
        if (containsDuplicates)
            throw new DuplicateCategoryException("Imported categories contained duplicates. Please try again");

        // adds Categories to db and dictionary
        await MoMoneydb.db.InsertAllAsync(categories);
        foreach (var cat in categories)
            Categories.Add(cat.CategoryID, cat);
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
        Categories[updatedCategory.CategoryID] = updatedCategory;
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
        await MoMoneydb.db.CreateTableAsync<Category>();
        Categories.Clear();
    }

    /// <summary>
    /// Gets an category from the Categories table using an ID.
    /// </summary>
    /// <param name="ID"></param>
    /// <returns>Category object</returns>
    /// <exception cref="CategoryNotFoundException"></exception>
    public static async Task<Category>GetCategory(int ID)
    {
        await Init();

        if (Categories.TryGetValue(ID, out var category))
            return category;
        else
        {
            var cat = await MoMoneydb.db.Table<Category>().FirstOrDefaultAsync(c => c.CategoryID == ID);
            if (cat is null)
                throw new CategoryNotFoundException($"Could not find Category with ID '{ID}'.");
            else
                return cat;
        }
    }

    /// <summary>
    /// Gets an category from the Categories table using a name and parent name.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="parent"></param>
    /// <returns>Category object</returns>
    /// <exception cref="CategoryNotFoundException"></exception>
    public static async Task<Category> GetCategory(string name, string parent)
    {
        await Init();

        var cat = await MoMoneydb.db.Table<Category>().FirstOrDefaultAsync(c => c.CategoryName == name && c.ParentName == parent);
        if (cat is null)
            throw new CategoryNotFoundException($"Could not find Category with name '{name}'.");
        else
            return cat;
    }

    /// <summary>
    /// Gets Category from the Categories table using name. Only works for parent categories.
    /// </summary>
    /// <param name="name"></param>
    /// <returns>Category object</returns>
    /// <exception cref="CategoryNotFoundException"></exception>
    public static async Task<Category> GetParentCategory(string name)
    {
        await Init();

        var cat = await MoMoneydb.db.Table<Category>().FirstOrDefaultAsync(c => c.CategoryName == name && c.ParentName == "");
        if (cat is null)
            throw new CategoryNotFoundException($"Could not find Category with name '{name}'.");
        else
            return cat;
    }

    /// <summary>
    /// Gets all Categories from Categories table as a dictionary with Category ID as Key.
    /// </summary>
    /// <returns>Dictionary of Category objects</returns>
    static async Task<Dictionary<int, Category>> GetCategoriesAsDict()
    {
        var categories = await MoMoneydb.db.Table<Category>().ToListAsync();
        return categories.ToDictionary(c => c.CategoryID, c => c);
    }

    /// <summary>
    /// Gets all Categories from Categories table as a list.
    /// </summary>
    /// <returns>List of Category objects</returns>
    public static async Task<IEnumerable<Category>> GetCategories()
    {
        await Init();

        var cats = Categories.Where(c => c.Value.CategoryID >= Constants.EXPENSE_ID).Select(pair => pair.Value);
        if (cats.Any())
            return cats;
        else
            return await MoMoneydb.db.Table<Category>()
                                     .Where(c => c.CategoryID >= Constants.EXPENSE_ID)
                                     .ToListAsync();
    }

    /// <summary>
    /// Gets all parent Categories from Categories table as a list.
    /// </summary>
    /// <returns>List of parent Category objects</returns>
    public static async Task<IEnumerable<Category>> GetAllParentCategories()
    {
        await Init();

        var cats = Categories.Where(c => c.Value.ParentName == "").Select(pair => pair.Value);
        if (cats.Any())
            return cats;
        else
            return await MoMoneydb.db.Table<Category>().Where(c => c.ParentName == "").ToListAsync();
    }

    /// <summary>
    /// Gets all parent Categories, minus transfers, from Categories table as a list.
    /// </summary>
    /// <returns>List of parent Category objects</returns>
    public static async Task<IEnumerable<Category>> GetParentCategories()
    {
        await Init();

        var cats = Categories.Where(c => c.Value.ParentName == "" && c.Value.CategoryID != Constants.TRANSFER_ID)
                             .Select(pair => pair.Value);
        if (cats.Any())
            return cats;
        else
            return await MoMoneydb.db.Table<Category>()
                                     .Where(c => c.ParentName == "" && c.CategoryID != Constants.TRANSFER_ID)
                                     .ToListAsync();
    }

    /// <summary>
    /// Gets all expense Categories from Categories table as a list.
    /// </summary>
    /// <returns>List of parent Category objects</returns>
    public static async Task<IEnumerable<Category>> GetExpenseCategories()
    {
        await Init();

        var cats = Categories.Where(c => c.Value.ParentName == "" && c.Value.CategoryID >= Constants.EXPENSE_ID)
                             .Select(pair => pair.Value);
        if (cats.Any())
            return cats;
        else
            return await MoMoneydb.db.Table<Category>()
                                     .Where(c => c.ParentName == "" && c.CategoryID >= Constants.EXPENSE_ID)
                                     .ToListAsync();
    }

    /// <summary>
    /// Gets all Subcategories of a Category from Categories table as a list.
    /// </summary>
    /// <returns>List of Category objects</returns>
    public static async Task<IEnumerable<Category>> GetSubcategories(Category parentCategory)
    {
        await Init();

        var cats = Categories.Where(c => c.Value.ParentName == parentCategory.CategoryName)
                             .Select(pair => pair.Value);
        if (cats.Any())
            return cats;
        else
            return await MoMoneydb.db.Table<Category>()
                                     .Where(c => c.ParentName == parentCategory.CategoryName)
                                     .ToListAsync();
    }
}
