using SQLite;
using MoMoney.Models;

namespace MoMoney;

public static class MoMoneydb
{
    public static SQLiteAsyncConnection db { get; private set; }

    /// <summary>
    /// Creates new database connection, creates tables if not exists and adds default data to tables.
    /// </summary>
    public static async Task Init()
    {
        try
        {
            if (db is not null)
                return;

            db = new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);

            await db.CreateTableAsync<Transaction>();
            await db.CreateTableAsync<Account>();
            await db.CreateTableAsync<Stock>();

            var category = await db.CreateTableAsync<Category>();
            if (category == CreateTableResult.Created)
            {
                await db.InsertAllAsync(GetDefaultCategories());
            }
            else
            {
                int count = await db.Table<Category>().CountAsync();
                if (count < 1)
                {
                    await db.DropTableAsync<Category>();
                    await db.CreateTableAsync<Category>();
                    await db.InsertAllAsync(GetDefaultCategories());
                }
            }
        }
        catch(Exception ex)
        {
            await Shell.Current.DisplayAlert("Database Error", ex.Message, "OK");
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
