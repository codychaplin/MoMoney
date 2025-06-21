using System.Security.Cryptography;
using SQLite;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;

namespace MoMoney.Core.Data;

public class MoMoneydb : IMoMoneydb
{
    public ISQLiteAsyncConnection db { get; private set; } = new SQLiteAsyncConnection(Constants.DefaultDbPath);
    private TaskCompletionSource<bool> _initializationTcs = new();

    public async Task Init(bool wait = true)
    {
        if (wait)
        {
            await _initializationTcs.Task;
            return;
        }

        try
        {
            // Get or create encryption key
            string? dbEncryptionKey = await SecureStorage.Default.GetAsync(Constants.dbEncryptionKey);
            if (string.IsNullOrEmpty(dbEncryptionKey))
            {
                dbEncryptionKey = GenerateRandomKey(32); // 32 bytes = 256 bits
                await SecureStorage.Default.SetAsync(Constants.dbEncryptionKey, dbEncryptionKey);
            } 
            var options = new SQLiteConnectionString(Constants.DatabasePath, true, dbEncryptionKey);
            db = new SQLiteAsyncConnection(options);

            await db.CreateTableAsync<Log>();
            await db.CreateTableAsync<Stock>();
            await db.CreateTableAsync<Account>();
            await db.CreateTableAsync<Transaction>();
            await db.CreateTableAsync<ChatResponse>();
            await db.CreateTableAsync<WhisperResponse>();
            await CreateCategories();

            _initializationTcs.SetResult(true);
        }
        catch (Exception ex)
        {
            _initializationTcs.SetException(ex);
            await Shell.Current.DisplayAlert("Database Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Generates a secure random key and encodes it as Base64.
    /// </summary>
    /// <param name="length">Number of bytes for the key.</param>
    /// <returns>Base64 string of the random key.</returns>
    static string GenerateRandomKey(int length)
    {
        byte[] keyBytes = new byte[length];
        RandomNumberGenerator.Fill(keyBytes);
        return Convert.ToBase64String(keyBytes);
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
            await db.DropTableAsync<ChatResponse>();
            await db.DropTableAsync<WhisperResponse>();
            await db.CloseAsync();
            db = new SQLiteAsyncConnection(Constants.DefaultDbPath);
        }
        _initializationTcs = new TaskCompletionSource<bool>();
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
    List<Category> GetDefaultCategories()
    {
        return
        [
            new Category(Constants.INCOME_ID, "Income", string.Empty), // 1
            new Category(Constants.TRANSFER_ID, "Transfer", string.Empty), // 2
            new Category(Constants.DEBIT_ID, "Debit", "Transfer"), // 3
            new Category(Constants.CREDIT_ID, "Credit", "Transfer") // 4
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
