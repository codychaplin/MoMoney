using MoMoney.Models;

namespace MoMoney.Services
{
    public static class CategoryService
    {

        /// <summary>
        /// Creates Category table, if none exist
        /// </summary>
        public static async Task Init()
        {
            if (MoMoneydb.Init())
                await MoMoneydb.db.CreateTableAsync<Category>();
            else
                return;
        }

        /// <summary>
        /// Creates new Category object and inserts into Category table
        /// </summary>
        /// <param name="categoryName"></param>
        /// <param name="parentName"></param>
        /// <param name="enabled"></param>
        public static async Task AddCategory(string categoryName, string parentName, bool enabled)
        {
            await Init();

            var category = new Category
            {
                CategoryName = categoryName,
                ParentName = string.IsNullOrEmpty(parentName) ? "" : parentName,
                Enabled = enabled
            };

            await MoMoneydb.db.InsertAsync(category);
        }

        /// <summary>
        /// Given an Category object, updates the corresponding category in the Categories table
        /// </summary>
        /// <param name="category"></param>
        public static async Task UpdateCategory(Category category)
        {
            await Init();

            var updatedCategory = new Category
            {
                CategoryID = category.CategoryID,
                CategoryName = category.CategoryName,
                ParentName = category.ParentName,
                Enabled = category.Enabled
            };

            await MoMoneydb.db.UpdateAsync(updatedCategory);
        }

        /// <summary>
        /// Removes Category from Categories table
        /// </summary>
        /// <param name="ID"></param>
        public static async Task RemoveCategory(int ID)
        {
            await Init();

            await MoMoneydb.db.DeleteAsync<Category>(ID);
        }

        /// <summary>
        /// Gets an category from the Categories table using an ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Category object</returns>
        public static async Task<Category>GetCategory(int id)
        {
            var category = await MoMoneydb.db.Table<Category>().FirstOrDefaultAsync(c => c.CategoryID == id);

            return category;
        }

        /// <summary>
        /// Gets all Categories from Categories table as a list
        /// </summary>
        /// <returns>List of Category objects</returns>
        public static async Task<IEnumerable<Category>> GetCategories()
        {
            await Init();

            return await MoMoneydb.db.Table<Category>().ToListAsync();
        }

        /// <summary>
        /// Gets enabled categories from Categories table as a list
        /// </summary>
        /// <returns>List of active Category objects</returns>
        public static async Task<IEnumerable<Category>> GetActiveCategories()
        {
            await Init();

            return await MoMoneydb.db.Table<Category>().Where(c => c.Enabled).ToListAsync();
        }

        /// <summary>
        /// Gets parent Category from Categories table
        /// </summary>
        /// <returns>Parent of specified category</returns>
        public static async Task<Category> GetParentCategory(string parentName)
        {
            await Init();

            return await MoMoneydb.db.Table<Category>().FirstOrDefaultAsync(c => c.CategoryName == parentName);
        }

        /// <summary>
        /// Gets all parent Categories from Categories table as a list
        /// </summary>
        /// <returns>List of parent Category objects</returns>
        public static async Task<IEnumerable<Category>> GetParentCategories()
        {
            await Init();

            return await MoMoneydb.db.Table<Category>().Where(c => c.ParentName == "").ToListAsync();
        }

        /// <summary>
        /// Gets all enabled parent Categories from Categories table as a list
        /// </summary>
        /// <returns>List of active parent Category objects</returns>
        public static async Task<IEnumerable<Category>> GetActiveParentCategories()
        {
            await Init();

            return await MoMoneydb.db.Table<Category>().Where(c => c.ParentName == "" && c.Enabled).ToListAsync();
        }

        /// <summary>
        /// Gets all parent Categories from Categories table as a list
        /// </summary>
        /// <returns>List of parent Category objects</returns>
        public static async Task<IEnumerable<Category>> GetSubcategories(Category parentCategory)
        {
            await Init();

            return await MoMoneydb.db.Table<Category>().Where(c => c.ParentName == parentCategory.CategoryName).ToListAsync();
        }
    }
}
