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

            await db.CreateTableAsync<Log>();
            await db.CreateTableAsync<Stock>();
            await db.CreateTableAsync<Account>();
            await db.CreateTableAsync<Transaction>();
            await db.CreateTableAsync<MappingRule>();
            await db.CreateTableAsync<ChatResponse>();
            await db.CreateTableAsync<WhisperResponse>();
            await CreateCategories();
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

    public async Task CreateCategories()
    {
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

    /// <summary>
    /// Gets default categories.
    /// </summary>
    /// <returns>List of default categories</returns>
    static List<Category> GetDefaultCategories()
    {
        return
        [
            new Category(Constants.TRANSFER_ID, "Transfer", null), // 1
            new Category(Constants.DEBIT_ID, "Debit", Constants.TRANSFER_ID), // 2
            new Category(Constants.CREDIT_ID, "Credit", Constants.TRANSFER_ID), // 3
            new Category(Constants.INCOME_ID, "Income", null) // 4
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
