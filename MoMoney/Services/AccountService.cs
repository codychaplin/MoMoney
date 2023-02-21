using MoMoney.Models;
using MoMoney.Exceptions;

namespace MoMoney.Services;

public static class AccountService
{
    public static Dictionary<int, Account> Accounts { get; private set; } = new();

    /// <summary>
    /// Calls db Init.
    /// </summary>
    static async Task Init()
    {
        await MoMoneydb.Init();

        if (!Accounts.Any())
            Accounts = await GetAccountsAsDict();
    }

    /// <summary>
    /// Creates new Account object and inserts into Accounts table.
    /// </summary>
    /// <param name="accountName"></param>
    /// <param name="accountType"></param>
    /// <param name="startingBalance"></param>
    /// <exception cref="DuplicateAccountException"></exception>
    public static async Task AddAccount(string accountName, string accountType, decimal startingBalance)
    {
        await Init();

        var res = await MoMoneydb.db.Table<Account>().CountAsync(a => a.AccountName == accountName);
        if (res > 0)
            throw new DuplicateAccountException("Account named '" + accountName + "' already exists");

        // startingBalance used for CurrentBalance because it's calculated later
        var account = new Account
        {
            AccountName = accountName,
            AccountType = accountType,
            StartingBalance = startingBalance,
            CurrentBalance = startingBalance,
            Enabled = true
        };

        // adds Account to db and dictionary
        await MoMoneydb.db.InsertAsync(account);
        Accounts.Add(account.AccountID, account);
    }

    /// <summary>
    /// Inserts multiple Account objects into Accounts table.
    /// </summary>
    /// <param name="accounts"></param>
    /// <exception cref="DuplicateAccountException"></exception>
    public static async Task AddAccounts(List<Account> accounts)
    {
        await Init();

        // gets accounts from db
        var dbAccounts = await MoMoneydb.db.Table<Account>().ToListAsync();

        // checks if names of any new accounts matches any names from dbAccounts and throw exception if true
        bool containsDuplicates = accounts.Any(a => dbAccounts.Select(dba => dba.AccountName).Contains(a.AccountName));
        if (containsDuplicates)
            throw new DuplicateAccountException("Imported accounts contained duplicates. Please try again");

        // adds accounts to db and dictionary
        await MoMoneydb.db.InsertAllAsync(accounts);
        foreach (var acc in accounts)
            Accounts.Add(acc.AccountID, acc);
    }

    /// <summary>
    /// Given an Account object, updates the corresponding account in the Accounts table.
    /// </summary>
    /// <param name="updatedAccount"></param>
    public static async Task UpdateAccount(Account updatedAccount)
    {
        await Init();

        // update Account in db and dictionary
        await MoMoneydb.db.UpdateAsync(updatedAccount);
        Accounts[updatedAccount.AccountID] = updatedAccount;
    }

    /// <summary>
    /// Removes Account from Accounts table.
    /// </summary>
    /// <param name="ID"></param>
    public static async Task RemoveAccount(int ID)
    {
        await Init();

        // removes Account from db and dictionary
        await MoMoneydb.db.DeleteAsync<Account>(ID);
        Accounts.Remove(ID);
    }

    /// <summary>
    /// Removes ALL Accounts from Accounts table.
    /// </summary>
    public static async Task RemoveAllAccounts()
    {
        await Init();

        await MoMoneydb.db.DeleteAllAsync<Account>();
        await MoMoneydb.db.DropTableAsync<Account>();
        await MoMoneydb.db.CreateTableAsync<Account>();
        Accounts.Clear();
    }

    /// <summary>
    /// Gets an account from the Accounts table using an ID.
    /// </summary>
    /// <param name="ID"></param>
    /// <returns>Account object</returns>
    /// <exception cref="AccountNotFoundException"></exception>
    public static async Task<Account> GetAccount(int ID)
    {
        await Init();

        if (Accounts.TryGetValue(ID, out var account))
            return account;
        else
        {
            var acc = await MoMoneydb.db.Table<Account>().FirstOrDefaultAsync(a => a.AccountID == ID);
            if (acc is null)
                throw new AccountNotFoundException($"Could not find Account with ID '{ID}'.");
            else
                return acc;
        }
    }

    /// <summary>
    /// Gets an account from the Accounts table using a name.
    /// </summary>
    /// <param name="name"></param>
    /// <returns>Account object</returns>
    /// <exception cref="AccountNotFoundException"></exception>
    public static async Task<Account> GetAccount(string name)
    {
        await Init();

        var acc = await MoMoneydb.db.Table<Account>().FirstOrDefaultAsync(a => a.AccountName == name);
        if (acc is null)
            throw new AccountNotFoundException($"Could not find Account with name '{name}'.");
        else
            return acc;
    }

    /// <summary>
    /// Gets all Accounts from Accounts table as a list.
    /// </summary>
    /// <returns>List of Account objects</returns>
    public static async Task<IEnumerable<Account>> GetAccounts()
    {
        await Init();

        return await MoMoneydb.db.Table<Account>().ToListAsync();
    }

    /// <summary>
    /// Gets all Accounts from Accounts table as a dictionary with Account ID as Key.
    /// </summary>
    /// <returns>Dictionary of Account objects</returns>
    static async Task<Dictionary<int, Account>> GetAccountsAsDict()
    {
        var accounts = await MoMoneydb.db.Table<Account>().ToListAsync();
        return accounts.ToDictionary(a => a.AccountID, a => a);
    }

    /// <summary>
    /// Gets enabled accounts from Accounts table as a list.
    /// </summary>
    /// <returns>List of active Account objects</returns>
    public static async Task<IEnumerable<Account>> GetActiveAccounts()
    {
        await Init();

        return await MoMoneydb.db.Table<Account>().Where(a => a.Enabled).ToListAsync();
    }

    /// <summary>
    /// Updates CurrentBalance of specified Account.
    /// </summary>
    /// <param name="ID"></param>
    /// <param name="amount"></param>
    public static async Task UpdateBalance(int ID, decimal amount)
    {
        await MoMoneydb.db.QueryAsync<Account>($"UPDATE Account SET CurrentBalance=CurrentBalance + {amount} WHERE AccountID={ID}");
        Accounts[ID].CurrentBalance += amount; 
    }
}