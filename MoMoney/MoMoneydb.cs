using MoMoney.Models;
using SQLite;

namespace MoMoney
{
    public static class MoMoneydb
    {
        public static SQLiteAsyncConnection db { get; private set; }

        /// <summary>
        /// Creates new database connection, creates tables if not exists and adds default data to tables
        /// </summary>
        public static async Task Init()
        {
            if (db is not null)
                return;
            
            db = new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);

            var account = await db.CreateTableAsync<Account>();
            if (account == CreateTableResult.Created)
            {
                await db.InsertAsync(new Account { AccountName = "Checkings", AccountType = "Checkings", StartingBalance = 0 });
            }

            var category = await db.CreateTableAsync<Category>();
            if (category == CreateTableResult.Created)
            {
                var defaultList = new List<Category>();
                defaultList.Add(new Category { CategoryName = "Income", ParentName = "" }); // 1
                defaultList.Add(new Category { CategoryName = "Transfer", ParentName = "" }); // 2
                defaultList.Add(new Category { CategoryName = "Debit", ParentName = "Transfer" }); // 3
                defaultList.Add(new Category { CategoryName = "Credit", ParentName = "Transfer" }); // 4
                await db.InsertAllAsync(defaultList);
            }

            await db.CreateTableAsync<Transaction>();
        }
    }
}
