using System.Security.Cryptography;
using SQLite;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Models.Statements;

namespace MoMoney.Core.Data;

public class MoMoneydb : IMoMoneydb
{
    public ISQLiteAsyncConnection db { get; private set; } = new SQLiteAsyncConnection(Constants.DefaultDbPath);

    public async Task Init(bool wait = true)
    {
        try
        {
            // Ensure db is encrypted with a key stored in secure storage.
            string? dbEncryptionKey = await SecureStorage.Default.GetAsync(Constants.dbEncryptionKey);
            if (string.IsNullOrEmpty(dbEncryptionKey))
            {
                byte[] newEncrpytionKey = RandomNumberGenerator.GetBytes(32);
                dbEncryptionKey = Convert.ToBase64String(newEncrpytionKey);
                await SecureStorage.Default.SetAsync(Constants.dbEncryptionKey, dbEncryptionKey);
            }

            // Create encrypted db connection
            var options = new SQLiteConnectionString(Constants.DatabasePath, true, dbEncryptionKey);
            db = new SQLiteAsyncConnection(options);

            await db.CreateTableAsync<DbVersion>();
            await db.CreateTableAsync<Log>();
            await db.CreateTableAsync<Stock>();
            await db.CreateTableAsync<Account>();
            await db.CreateTableAsync<Transaction>();
            await db.CreateTableAsync<MappingRule>();
            await db.CreateTableAsync<ChatResponse>();
            await db.CreateTableAsync<WhisperResponse>();
            await CreateCategories();

            var currentVersion = await db.Table<DbVersion>().FirstOrDefaultAsync();
            if (currentVersion is null || currentVersion.Version != Constants.dbVersion)
            {
                var accountInfo = await db.GetTableInfoAsync(nameof(Account));
                if (accountInfo != null && !accountInfo.Select(x => x.Name).Contains("Colour"))
                    await db.ExecuteAsync("ALTER TABLE Account ADD COLUMN Colour TEXT");

                var categoryInfo = await db.GetTableInfoAsync(nameof(Category));
                if (categoryInfo != null && !categoryInfo.Select(x => x.Name).Contains("Colour"))
                    await db.ExecuteAsync("ALTER TABLE Category ADD COLUMN Colour TEXT");

                // Update db version
                await db.InsertOrReplaceAsync(new DbVersion(Constants.dbVersion));
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Database Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Drops all tables, closes and nullifies database connection, and re-initializes the database.
    /// </summary>
    /// <returns></returns>
    public async Task ResetDb()
    {
        if (db is not null)
        {
            await db.DropTableAsync<Log>();
            await db.DropTableAsync<Stock>();
            await db.DropTableAsync<Account>();
            await db.DropTableAsync<Category>();
            await db.DropTableAsync<Transaction>();
            await db.DropTableAsync<MappingRule>();
            await db.DropTableAsync<ChatResponse>();
            await db.DropTableAsync<WhisperResponse>();
            await db.CloseAsync();
            db = new SQLiteAsyncConnection(Constants.DefaultDbPath);
        }
        await Init();
    }

    public async Task CreateCategories(bool getAll = true)
    {
        var category = await db.CreateTableAsync<Category>();

        if (category == CreateTableResult.Created)
        {
            await db.InsertAllAsync(GetDefaultCategories(getAll));
        }
        else
        {
            int count = await db.Table<Category>().CountAsync();
            if (count < 1)
            {
                await db.DropTableAsync<Category>();
                await db.CreateTableAsync<Category>();
                await db.InsertAllAsync(GetDefaultCategories(getAll));
            }
        }
    }

    /// <summary>
    /// Gets default categories.
    /// </summary>
    /// <returns>List of default categories</returns>
    static List<Category> GetDefaultCategories(bool getAll = true)
    {
        List<Category> baseCategories = [
            new Category(Constants.TRANSFER_ID, "Transfer", null, "#808080"), // 1
            new Category(Constants.DEBIT_ID, "Debit", Constants.TRANSFER_ID, "#555555"), // 2
            new Category(Constants.CREDIT_ID, "Credit", Constants.TRANSFER_ID, "#A9A9A9"), // 3
            new Category(Constants.INCOME_ID, "Income", null, "#28AA74"), // 4
            new Category(5, "Work", Constants.INCOME_ID, "#1E7F57"),
        ];
        if (!getAll)
            return baseCategories;

        return
        [
            ..baseCategories,

            // income
            new Category(6, "Interest", Constants.INCOME_ID, "#79AA2C"),
            new Category(7, "Other", Constants.INCOME_ID, "#B6FF42"),

            // home
            new Category(8, "Home", null, "#AA6C24"),
            new Category(9, "Rent", 8, "#FFA237"),
            new Category(10, "Mortgage", 8, "#906546"),
            new Category(11, "Utilities", 8, "#FFC079"),

            // food
            new Category(12, "Food", null, "#FF3535"),
            new Category(13, "Groceries", 12, "#FF7777"),
            new Category(14, "Restaurant", 12, "#AA2323"),

            // shopping
            new Category(15, "Shopping", null, "#8938A6"),
            new Category(16, "Apparel", 15, "#CD55F9"),
            new Category(17, "Household", 15, "#A179F8"),

            // entertainment
            new Category(18, "Entertainment", null, "#3CF2FF"),
            new Category(19, "Movies", 18, "#2C63A0"),
            new Category(20, "Hobbies", 18, "#7CF6FF"),
            new Category(21, "Outings", 18, "#9DF8FF"),

            // subscriptions
            new Category(22, "Subscriptions", null, "#214A78"),
            new Category(23, "Phone", 22, "#163251"),
            new Category(24, "Spotify", 22, "#BFDAF9"),

            // car
            new Category(25, "Car", null, "#555555"),
            new Category(26, "Gas", 25, "#7F511B"),
            new Category(27, "Insurance", 25, "#404040"),
            new Category(28, "Maintenance", 25, "#808080"),

            // travel
            new Category(29, "Travel", null, "#28AA74"),
            new Category(30, "Flights", 29, "#B6FF42"),
            new Category(31, "Accomodation", 29, "#3CFFAE"),

            // miscellaneous
            new Category(32, "Miscellaneous", null, "#A9A9A9"),
            new Category(33, "Other", 32, "#BFBFBF"),
        ];
    }

    // ----------------------- CRUD wrappers (needed for unit testing) ----------------------- //

    public Task<int> AccountsCountAsync()
    {
        return db.Table<Account>().CountAsync();
    }

    public Task<int> AccountsCountAsync(string accountName)
    {
        return db.Table<Account>().CountAsync(a => a.AccountName == accountName);
    }

    public Task<List<Account>> AccountsToList()
    {
        return db.Table<Account>().ToListAsync();
    }

    public Task<Account> FirstOrDefaultAccountAsync(int accountID)
    {
        return db.Table<Account>().FirstOrDefaultAsync(a => a.AccountID == accountID);
    }
}
