﻿using SQLite;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;

namespace MoMoney.Core.Data;

public class MoMoneydb : IMoMoneydb
{
    public ISQLiteAsyncConnection db { get; private set; } = new SQLiteAsyncConnection(Constants.DefaultDbPath);
    bool initialized = false;

    /// <summary>
    /// Creates new database connection, creates tables if not exists and adds default data to tables.
    /// </summary>
    public async Task Init()
    {
        if (db.DatabasePath != Constants.DefaultDbPath)
        {
            while (!initialized)
            {
                await Task.Delay(100);
            }
            return;
        }

        try
        {
            db = new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);

            await db.CreateTableAsync<Log>();
            await db.CreateTableAsync<Stock>();
            await db.CreateTableAsync<Account>();
            await db.CreateTableAsync<Transaction>();
            await db.CreateTableAsync<ChatResponse>();
            await db.CreateTableAsync<WhisperResponse>();
            await CreateCategories();

            initialized = true;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Database Error", ex.Message, "OK");
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
            await db.DropTableAsync<ChatResponse>();
            await db.DropTableAsync<WhisperResponse>();
            await db.CloseAsync();
            db = new SQLiteAsyncConnection(Constants.DefaultDbPath);
        }
        initialized = false;
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
