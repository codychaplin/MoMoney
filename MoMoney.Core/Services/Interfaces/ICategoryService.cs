using MoMoney.Core.Models;
using MoMoney.Core.Exceptions;

namespace MoMoney.Core.Services.Interfaces;

public interface ICategoryService
{
    /// <summary>
    /// Cached dictionary of Categories
    /// </summary>
    Dictionary<int, Category> Categories { get; }

    /// <summary>
    /// Creates new Category object and inserts into Category table.
    /// </summary>
    /// <param name="categoryName"></param>
    /// <param name="parentCategoryID"></param>
    /// <exception cref="DuplicateException"></exception>
    Task AddCategory(string categoryName, int? parentCategoryID);

    /// <summary>
    /// Inserts multiple Category objects into Categories table.
    /// </summary>
    /// <param name="categories"></param>
    /// <exception cref="DuplicateException"></exception>
    Task AddCategories(List<Category> categories);

    /// <summary>
    /// Given an Category object, updates the corresponding category in the Categories table.
    /// </summary>
    /// <param name="updatedCategory"></param>
    Task UpdateCategory(Category updatedCategory);

    /// <summary>
    /// Removes Category from Categories table.
    /// </summary>
    /// <param name="ID"></param>
    Task RemoveCategory(int ID);

    /// <summary>
    /// Removes ALL Categories from Categories table.
    /// </summary>
    Task RemoveAllCategories();

    /// <summary>
    /// Gets an category from the Categories table using an ID.
    /// </summary>
    /// <param name="ID"></param>
    /// <param name="tryGet"></param>
    /// <returns>Category object</returns>
    /// <exception cref="NotFoundException"></exception>
    Task<Category?> GetCategory(int ID, bool tryGet = false);

    /// <summary>
    /// Gets Category from the Categories table using name. Only works for parent categories.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="tryGet"></param>
    /// <returns>Category object</returns>
    /// <exception cref="NotFoundException"></exception>
    Task<Category?> GetParentCategoryByName(string name, bool tryGet = false);

    /// <summary>
    /// Gets Category from the Categories table using an ID. Only works for parent categories.
    /// </summary>
    /// <param name="parentCategoryID"></param>
    /// <param name="tryGet"></param>
    /// <returns>Category object</returns>
    /// <exception cref="NotFoundException"></exception>
    Task<Category?> GetParentCategoryByID(int parentCategoryID, bool tryGet = false);

    /// <summary>
    /// Gets all Categories from Categories table as a dictionary with Category ID as Key.
    /// </summary>
    /// <returns>Dictionary of Category objects</returns>
    Task<Dictionary<string, int>> GetCategoriesAsNameDict();

    /// <summary>
    /// Gets all mutable Categories from Categories table as a list.
    /// </summary>
    /// <param name="includeParentName"></param>
    /// <returns>List of Category objects</returns>
    Task<IEnumerable<Category>> GetCategories(bool includeParentName = false);

    /// <summary>
    /// Gets all Categories from Categories table as a list.
    /// </summary>
    /// <returns>List of Category objects</returns>
    Task<IEnumerable<Category>> GetAllCategories();

    /// <summary>
    /// Gets all parent Categories from Categories table as a list.
    /// </summary>
    /// <returns>List of parent Category objects</returns>
    Task<IEnumerable<Category>> GetAllParentCategories();

    /// <summary>
    /// Gets all parent Categories, minus transfers, from Categories table as a list.
    /// </summary>
    /// <returns>List of parent Category objects</returns>
    Task<IEnumerable<Category>> GetParentCategories();

    /// <summary>
    /// Gets all expense Categories from Categories table as a list.
    /// </summary>
    /// <returns>List of parent Category objects</returns>
    Task<IEnumerable<Category>> GetExpenseCategories();

    /// <summary>
    /// Gets all Subcategories of a Category from Categories table as a list.
    /// </summary>
    /// <returns>List of Category objects</returns>
    Task<IEnumerable<Category>> GetSubcategories(Category parentCategory);

    /// <summary>
    /// Gets total number of Categories in db.
    /// </summary>
    /// <returns>Integer representing number of Categories</returns>
    Task<int> GetCategoryCount();
}