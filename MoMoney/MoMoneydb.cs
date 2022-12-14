using SQLite;
using MoMoney.Models;

namespace MoMoney
{
    public static class MoMoneydb
    {
        public static SQLiteAsyncConnection db { get; private set; }

        /// <summary>
        /// Creates new database connection, creates tables if not exists and adds default data to tables.
        /// </summary>
        public static async Task Init()
        {
            if (db is not null)
                return;
            
            db = new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);

            await db.CreateTableAsync<Transaction>();

            var account = await db.CreateTableAsync<Account>();
            if (account == CreateTableResult.Created)
            {
                await db.InsertAsync(new Account { AccountName = "Checkings", AccountType = "Checkings", StartingBalance = 0, Enabled = true });
            }
            else
            {
                int count = await db.Table<Account>().CountAsync();
                if (count <= 0)
                {
                    await db.InsertAsync(new Account { AccountName = "Checkings", AccountType = "Checkings", StartingBalance = 0, Enabled = true });
                }
            }

            var category = await db.CreateTableAsync<Category>();
            if (category == CreateTableResult.Created)
            {
                await db.InsertAllAsync(GetDefaultCategories());
            }
            else
            {
                int count = await db.Table<Category>().CountAsync();
                if (count <= 0)
                {
                    await db.DropTableAsync<Category>();
                    await db.CreateTableAsync<Category>();
                    await db.InsertAllAsync(GetDefaultCategories());
                }
            }
        }

        /// <summary>
        /// Gets default categories.
        /// </summary>
        /// <returns>List of default categories</returns>
        static List<Category> GetDefaultCategories()
        {
            return new List<Category>
                {
                    new Category { CategoryName = "Income", ParentName = "" }, // 1
                    new Category { CategoryName = "Transfer", ParentName = "" }, // 2
                    new Category { CategoryName = "Debit", ParentName = "Transfer" }, // 3
                    new Category { CategoryName = "Credit", ParentName = "Transfer" } // 4
                };
        }
    }
}
