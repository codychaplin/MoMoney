using MoMoney.Exceptions;
using MoMoney.Models;

namespace MoMoney.Services
{
    public static class CategoryService
    {

        /// <summary>
        /// Calls db Init.
        /// </summary>
        public static async Task Init()
        {
            await MoMoneydb.Init();
        }

        /// <summary>
        /// Creates new Category object and inserts into Category table.
        /// </summary>
        /// <param name="categoryName"></param>
        /// <param name="parentName"></param>
        public static async Task AddCategory(string categoryName, string parentName)
        {
            await Init();

            var category = new Category
            {
                CategoryName = categoryName,
                ParentName = string.IsNullOrEmpty(parentName) ? "" : parentName
            };

            await MoMoneydb.db.InsertAsync(category);
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

            await MoMoneydb.db.InsertAllAsync(categories);
        }

        /// <summary>
        /// Given an Category object, updates the corresponding category in the Categories table.
        /// </summary>
        /// <param name="updatedCategory"></param>
        public static async Task UpdateCategory(Category updatedCategory)
        {
            await Init();

            await MoMoneydb.db.UpdateAsync(updatedCategory);
        }

        /// <summary>
        /// Removes Category from Categories table.
        /// </summary>
        /// <param name="ID"></param>
        public static async Task RemoveCategory(int ID)
        {
            await Init();

            await MoMoneydb.db.DeleteAsync<Category>(ID);
        }

        /// <summary>
        /// Removes ALL Categories from Categories table.
        /// </summary>
        public static async Task RemoveAllCategories()
        {
            await Init();

            await MoMoneydb.db.DeleteAllAsync<Category>();
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
        /// Gets all Categories from Categories table as a list.
        /// </summary>
        /// <returns>List of Category objects</returns>
        public static async Task<IEnumerable<Category>> GetCategories()
        {
            await Init();

            // CategoryID > 4 so users can't select income/transfer categories
            return await MoMoneydb.db.Table<Category>().Where(c => c.CategoryID > 4).ToListAsync();
        }

        /// <summary>
        /// Gets parent Category from Categories table.
        /// </summary>
        /// <returns>Parent of specified category</returns>
        public static async Task<Category> GetParentCategory(string parentName)
        {
            await Init();

            return await MoMoneydb.db.Table<Category>().FirstOrDefaultAsync(c => c.CategoryName == parentName);
        }

        /// <summary>
        /// Gets all parent Categories from Categories table as a list.
        /// </summary>
        /// <returns>List of parent Category objects</returns>
        public static async Task<IEnumerable<Category>> GetParentCategories()
        {
            await Init();

            // CategoryID != 4 so users can't select transfer categories
            return await MoMoneydb.db.Table<Category>().Where(c => c.ParentName == "" && c.CategoryID != 4).ToListAsync();
        }

        /// <summary>
        /// Gets all expense Categories from Categories table as a list.
        /// </summary>
        /// <returns>List of parent Category objects</returns>
        public static async Task<IEnumerable<Category>> GetExpenseCategories()
        {
            await Init();

            // CategoryID > 4 so users can't select income/transfer categories
            return await MoMoneydb.db.Table<Category>().Where(c => c.ParentName == "" && c.CategoryID > 4).ToListAsync();
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
}
