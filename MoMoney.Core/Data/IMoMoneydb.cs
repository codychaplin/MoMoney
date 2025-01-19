using MoMoney.Core.Models;
using SQLite;

namespace MoMoney.Core.Data;

public interface IMoMoneydb
{
    public ISQLiteAsyncConnection db { get; }

    /// <summary>
    /// Creates new database connection, creates tables if not exists and adds default data to tables.
    /// </summary>
    Task Init();

    /// <summary>
    /// Drops all tables, closes and nullifies database connection, and re-initializes the database.
    /// </summary>
    Task ResetDb();

    /// <summary>
    /// Creates default categories.
    /// </summary>
    Task CreateCategories();

    // ----------------------- CRUD wrappers (needed for unit testing) ----------------------- //

    /// <summary>
    /// Counts the number of accounts in the database.
    /// </summary>
    /// <returns>Number of accounts</returns>
    Task<int> AccountsCountAsync();

    /// <summary>
    /// Counts the number of accounts in the database that match the account name.
    /// </summary>
    /// <returns>Number of accounts that match</returns>
    Task<int> AccountsCountAsync(string accountName);

    /// <summary>
    /// Gets all accounts from the database as a dictionary.
    /// </summary>
    /// <returns>List of accounts</returns>
    Task<List<Account>> AccountsToList();

    /// <summary>
    /// Gets the first account that matches the accountID.
    /// </summary>
    /// <param name="accountID"></param>
    /// <returns>Account object</returns>
    Task<Account> FirstOrDefaultAccountAsync(int accountID);
}