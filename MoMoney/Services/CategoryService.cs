using MoMoney.Data;
using MoMoney.Models;
using MoMoney.Exceptions;
using Android.Accounts;
using MoMoney.Helpers;
using System.Diagnostics;

namespace MoMoney.Services;

/// <inheritdoc />
public class CategoryService : ICategoryService
{
    readonly MoMoneydb momoney;
    readonly ILoggerService<CategoryService> logger;

    public Dictionary<int, Category> Categories { get; set; } = new();

    public CategoryService(MoMoneydb _momoney, ILoggerService<CategoryService> _logger)
    {
        momoney = _momoney;
        logger = _logger;
    }

    public async Task Init()
    {
        await momoney.Init();
        if (!Categories.Any())
            Categories = await GetCategoriesAsDict();
    }

    public async Task AddCategory(string categoryName, string parentName)
    {
        Stopwatch sw = Stopwatch.StartNew();

        await Init();
        var res = await momoney.db.Table<Category>().CountAsync(c => c.CategoryName == categoryName && c.ParentName == parentName);
        if (res > 0)
            throw new DuplicateCategoryException("Category '" + categoryName + "' already exists");

        var category = new Category
        {
            CategoryName = categoryName,
            ParentName = string.IsNullOrEmpty(parentName) ? "" : parentName
        };

        // adds Category to db and dictionary
        await momoney.db.InsertAsync(category);
        Categories.Add(category.CategoryID, category);

        sw.Stop();
        await logger.LogInfo($"[{sw.ElapsedMilliseconds}ms] Added Category #{category.CategoryID} to db.");
    }

    public async Task AddCategories(List<Category> categories)
    {
        Stopwatch sw = Stopwatch.StartNew();

        await Init();
        var dbCategories = await momoney.db.Table<Category>().ToListAsync();

        // gets names of all categories where name matches any names of categories in parameter categories
        bool containsDuplicates = categories.Any(a =>
             dbCategories.Any(dba => dba.CategoryName == a.CategoryName && dba.ParentName == a.ParentName));
        if (containsDuplicates)
            throw new DuplicateCategoryException("Imported categories contained duplicates. Please try again");

        // adds Categories to db and dictionary
        await momoney.db.InsertAllAsync(categories);
        foreach (var cat in categories)
            Categories.Add(cat.CategoryID, cat);

        sw.Stop();
        await logger.LogInfo($"[{sw.ElapsedMilliseconds}ms] Added {categories.Count} Categories to db.");
    }

    public async Task UpdateCategory(Category updatedCategory)
    {
        Stopwatch sw = Stopwatch.StartNew();

        await Init();
        await momoney.db.UpdateAsync(updatedCategory);
        Categories[updatedCategory.CategoryID] = updatedCategory;

        sw.Stop();
        await logger.LogInfo($"[{sw.ElapsedMilliseconds}ms] Updated Category #{updatedCategory.CategoryID} in db.");
    }

    public async Task RemoveCategory(int ID)
    {
        Stopwatch sw = Stopwatch.StartNew();

        await Init();
        await momoney.db.DeleteAsync<Category>(ID);
        Categories.Remove(ID);

        sw.Stop();
        await logger.LogInfo($"[{sw.ElapsedMilliseconds}ms] Removed Category #{ID} from db.");
    }

    public async Task RemoveAllCategories()
    {
        Stopwatch sw = Stopwatch.StartNew();

        await Init();
        await momoney.db.DeleteAllAsync<Category>();
        await momoney.db.DropTableAsync<Category>();
        await momoney.db.CreateTableAsync<Category>();
        Categories.Clear();

        sw.Stop();
        await logger.LogInfo($"[{sw.ElapsedMilliseconds}ms] Removed all Categories from db.");
    }

    public async Task<Category> GetCategory(int ID)
    {
        await Init();
        if (Categories.TryGetValue(ID, out var category))
            return new Category(category);

        var cat = await momoney.db.Table<Category>().FirstOrDefaultAsync(c => c.CategoryID == ID);
        if (cat is null)
            throw new CategoryNotFoundException($"Could not find Category with ID '{ID}'.");
        else
            return cat;

    }

    public async Task<Category> GetCategory(string name, string parent)
    {
        await Init();
        var cats = Categories.Values.Where(a => a.CategoryName.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                                                a.ParentName.Equals(parent, StringComparison.OrdinalIgnoreCase));
        if (cats.Any())
            return cats.First();

        var cat = await momoney.db.Table<Category>().FirstOrDefaultAsync(c => c.CategoryName == name && c.ParentName == parent);
        if (cat is null)
            throw new CategoryNotFoundException($"Could not find Category with name '{name}'.");
        else
            return cat;
    }

    public async Task<Category> GetParentCategory(string name)
    {
        await Init();
        var cats = Categories.Values.Where(a => a.CategoryName.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (cats.Any())
            return cats.First();

        var cat = await momoney.db.Table<Category>().FirstOrDefaultAsync(c => c.CategoryName == name && c.ParentName == "");
        if (cat is null)
            throw new CategoryNotFoundException($"Could not find Category with name '{name}'.");
        else
            return cat;
    }

    public async Task<Dictionary<string, int>> GetCategoriesAsNameDict()
    {
        await Init();
        var categories = await momoney.db.Table<Category>().ToListAsync();
        return categories.ToDictionary(c => c.CategoryName + "," + c.ParentName, c => c.CategoryID);
    }

    public async Task<IEnumerable<Category>> GetCategories()
    {
        await Init();
        var cats = Categories.Where(c => c.Value.CategoryID >= Constants.EXPENSE_ID).Select(pair => pair.Value);
        if (cats.Any())
            return cats;

        return await momoney.db.Table<Category>()
                                 .Where(c => c.CategoryID >= Constants.EXPENSE_ID)
                                 .ToListAsync();
    }

    public async Task<IEnumerable<Category>> GetAllParentCategories()
    {
        await Init();
        var cats = Categories.Where(c => c.Value.ParentName == "").Select(pair => pair.Value);
        if (cats.Any())
            return cats;

        return await momoney.db.Table<Category>().Where(c => c.ParentName == "").ToListAsync();
    }

    public async Task<IEnumerable<Category>> GetParentCategories()
    {
        await Init();
        var cats = Categories.Where(c => c.Value.ParentName == "" && c.Value.CategoryID != Constants.TRANSFER_ID)
                             .Select(pair => pair.Value);
        if (cats.Any())
            return cats;

        return await momoney.db.Table<Category>()
                               .Where(c => c.ParentName == "" && c.CategoryID != Constants.TRANSFER_ID)
                               .ToListAsync();
    }

    public async Task<IEnumerable<Category>> GetExpenseCategories()
    {
        await Init();
        var cats = Categories.Where(c => c.Value.ParentName == "" && c.Value.CategoryID >= Constants.EXPENSE_ID)
                             .Select(pair => pair.Value);
        if (cats.Any())
            return cats;

        return await momoney.db.Table<Category>()
                               .Where(c => c.ParentName == "" && c.CategoryID >= Constants.EXPENSE_ID)
                               .ToListAsync();
    }

    public async Task<IEnumerable<Category>> GetSubcategories(Category parentCategory)
    {
        await Init();
        var cats = Categories.Where(c => c.Value.ParentName == parentCategory.CategoryName)
                             .Select(pair => pair.Value);
        if (cats.Any())
            return cats;

        return await momoney.db.Table<Category>()
                               .Where(c => c.ParentName == parentCategory.CategoryName)
                               .ToListAsync();
    }

    /// <summary>
    /// Gets all Categories from Categories table as a dictionary with Category ID as key.
    /// </summary>
    /// <returns>Dictionary of Category objects</returns>
    async Task<Dictionary<int, Category>> GetCategoriesAsDict()
    {
        await momoney.Init();
        var categories = await momoney.db.Table<Category>().ToListAsync();
        return categories.ToDictionary(c => c.CategoryID, c => c);
    }
}