using MoMoney.Core.Data;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Exceptions;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.Services;

/// <inheritdoc />
public class CategoryService : BaseService<CategoryService, UpdateCategoriesMessage, string>, ICategoryService
{
    public Dictionary<int, Category> Categories { get; set; } = new();

    public CategoryService(MoMoneydb _momoney, ILoggerService<CategoryService> _logger) : base(_momoney, _logger) { }

    protected override async Task Init()
    {
        await base.Init();
        if (Categories.Count == 0)
            Categories = await GetCategoriesAsDict();
    }

    public async Task AddCategory(string categoryName, string parentName)
    {
        await DbOperation(async () =>
        {
            var count = await momoney.db.Table<Category>().CountAsync(c => c.CategoryName == categoryName && c.ParentName == parentName);
            if (count > 0)
                throw new DuplicateCategoryException("Category '" + categoryName + "' already exists");

            var category = new Category
            {
                CategoryName = categoryName,
                ParentName = string.IsNullOrEmpty(parentName) ? "" : parentName
            };

            // adds Category to db and dictionary
            await momoney.db.InsertAsync(category);
            Categories.Add(category.CategoryID, category);

            return $"Added Category #{category.CategoryID} to db.";
        });
    }

    public async Task AddCategories(List<Category> categories)
    {
        await DbOperation(async () =>
        {
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

            return $"Added {categories.Count} Categories to db.";
        });
    }

    public async Task UpdateCategory(Category updatedCategory)
    {
        await DbOperation(async () =>
        {
            await momoney.db.UpdateAsync(updatedCategory);
            Categories[updatedCategory.CategoryID] = updatedCategory;

            return $"Updated Category #{updatedCategory.CategoryID} in db.";
        });
    }

    public async Task RemoveCategory(int ID)
    {
        await DbOperation(async () =>
        {
            await momoney.db.DeleteAsync<Category>(ID);
            Categories.Remove(ID);

            return $"Removed Category #{ID} from db.";
        });
    }

    public async Task RemoveAllCategories()
    {
        await DbOperation(async () =>
        {
            await momoney.db.DeleteAllAsync<Category>();
            await momoney.db.DropTableAsync<Category>();
            Categories.Clear();

            // re-add default categories
            await momoney.CreateCategories();

            return $"Removed all Categories from db.";
        });
    }

    public async Task<Category> GetCategory(int ID)
    {
        await Init();
        if (Categories.TryGetValue(ID, out var category))
            return new Category(category);

        var cat = await momoney.db.Table<Category>().FirstOrDefaultAsync(c => c.CategoryID == ID);
        return cat is null
            ? throw new CategoryNotFoundException($"Could not find Category with ID '{ID}'.")
            : cat;
    }

    public async Task<Category> GetCategory(string name, string parent)
    {
        await Init();
        var cats = Categories.Values.Where(a => a.CategoryName.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                                                a.ParentName.Equals(parent, StringComparison.OrdinalIgnoreCase));
        if (cats.Any())
            return cats.First();

        var cat = await momoney.db.Table<Category>().FirstOrDefaultAsync(c => c.CategoryName == name && c.ParentName == parent);
        return cat is null
            ? throw new CategoryNotFoundException($"Could not find Category with name '{name}'.")
            : cat;
    }

    public async Task<Category> GetParentCategory(string name, bool tryGet = false)
    {
        await Init();
        var cats = Categories.Values.Where(a => a.CategoryName.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (cats.Any())
            return cats.First();

        var cat = await momoney.db.Table<Category>().FirstOrDefaultAsync(c => c.CategoryName == name && c.ParentName == "");
        return cat is null && !tryGet
            ? throw new CategoryNotFoundException($"Could not find Category with name '{name}'.")
            : cat;
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

    public async Task<int> GetCategoryCount()
    {
        await momoney.Init();
        return await momoney.db.Table<Category>().CountAsync();
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