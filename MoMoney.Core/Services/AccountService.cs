using MoMoney.Core.Data;
using MoMoney.Core.Models;
using MoMoney.Core.Exceptions;
using System.Diagnostics;

namespace MoMoney.Core.Services;

/// <inheritdoc />
public class AccountService : IAccountService
{
    readonly MoMoneydb momoney;
    readonly ILoggerService<AccountService> logger;

    public Dictionary<int, Account> Accounts { get; private set; } = new();

    public AccountService(MoMoneydb _momoney, ILoggerService<AccountService> _logger)
    {
        momoney = _momoney;
        logger = _logger;
    }

    /// <summary>
    /// Calls db Init.
    /// </summary>
    public async Task Init()
    {
        await momoney.Init();
        if (!Accounts.Any())
            Accounts = await GetAccountsAsDict();
    }

    public async Task AddAccount(string accountName, string accountType, decimal startingBalance)
    {
        Stopwatch sw = Stopwatch.StartNew();

        await Init();
        var res = await momoney.db.Table<Account>().CountAsync(a => a.AccountName == accountName);
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
        await momoney.db.InsertAsync(account);
        Accounts.Add(account.AccountID, account);

        sw.Stop();
        await logger.LogInfo($"[{sw.ElapsedMilliseconds}ms] Added Account #{account.AccountID} to db.");
    }

    public async Task AddAccounts(List<Account> accounts)
    {
        Stopwatch sw = Stopwatch.StartNew();

        await Init();
        var dbAccounts = await momoney.db.Table<Account>().ToListAsync();

        // checks if names of any new accounts matches any names from dbAccounts and throw exception if true
        bool containsDuplicates = accounts.Any(a => dbAccounts.Select(dba => dba.AccountName).Contains(a.AccountName));
        if (containsDuplicates)
            throw new DuplicateAccountException("Imported accounts contained duplicates. Please try again");

        // adds accounts to db and dictionary
        await momoney.db.InsertAllAsync(accounts);
        foreach (var acc in accounts)
            Accounts.Add(acc.AccountID, acc);

        sw.Stop();
        await logger.LogInfo($"[{sw.ElapsedMilliseconds}ms] Added {accounts.Count} Accounts to db.");
    }

    public async Task UpdateAccount(Account updatedAccount)
    {
        Stopwatch sw = Stopwatch.StartNew();

        await Init();
        await momoney.db.UpdateAsync(updatedAccount);
        Accounts[updatedAccount.AccountID] = updatedAccount;

        sw.Stop();
        await logger.LogInfo($"[{sw.ElapsedMilliseconds}ms] Updated Account #{updatedAccount.AccountID} in db.");
    }

    public async Task RemoveAccount(int ID)
    {
        Stopwatch sw = Stopwatch.StartNew();

        await Init();
        await momoney.db.DeleteAsync<Account>(ID);
        Accounts.Remove(ID);

        sw.Stop();
        await logger.LogInfo($"[{sw.ElapsedMilliseconds}ms] Removed Account #{ID} from db.");
    }

    public async Task RemoveAllAccounts()
    {
        Stopwatch sw = Stopwatch.StartNew();

        await Init();
        await momoney.db.DeleteAllAsync<Account>();
        await momoney.db.DropTableAsync<Account>();
        await momoney.db.CreateTableAsync<Account>();
        Accounts.Clear();

        sw.Stop();
        await logger.LogInfo($"[{sw.ElapsedMilliseconds}ms] Removed all Accounts from db.");
    }

    public async Task<Account> GetAccount(int ID)
    {
        await Init();
        if (Accounts.TryGetValue(ID, out var account))
            return new Account(account);

        var acc = await momoney.db.Table<Account>().FirstOrDefaultAsync(a => a.AccountID == ID);
        if (acc is null)
            throw new AccountNotFoundException($"Could not find Account with ID '{ID}'.");
        else
            return acc;
    }

    public async Task<Account> GetAccount(string name)
    {
        await Init();
        var accs = Accounts.Values.Where(a => a.AccountName.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (accs.Any())
            return accs.First();

        var acc = await momoney.db.Table<Account>().FirstOrDefaultAsync(a => a.AccountName == name);
        if (acc is null)
            throw new AccountNotFoundException($"Could not find Account with name '{name}'.");
        else
            return acc;
    }

    public async Task<IEnumerable<Account>> GetAccounts()
    {
        await Init();
        return await momoney.db.Table<Account>().ToListAsync();
    }

    public async Task<Dictionary<string, int>> GetAccountsAsNameDict()
    {
        await Init();
        var accounts = await momoney.db.Table<Account>().ToListAsync();
        return accounts.ToDictionary(a => a.AccountName, a => a.AccountID);
    }

    public async Task<IEnumerable<Account>> GetActiveAccounts()
    {
        await Init();
        return await momoney.db.Table<Account>().Where(a => a.Enabled).ToListAsync();
    }

    public async Task<int> GetAccountCount()
    {
        await momoney.Init();
        return await momoney.db.Table<Account>().CountAsync();
    }

    public async Task UpdateBalance(int ID, decimal amount)
    {
        Stopwatch sw = Stopwatch.StartNew();

        await Init();
        await momoney.db.QueryAsync<Account>($"UPDATE Account SET CurrentBalance=CurrentBalance + {amount} WHERE AccountID={ID}");
        decimal balanceBefore = Math.Round(Accounts[ID].CurrentBalance, 2);
        Accounts[ID].CurrentBalance += amount;
        decimal balanceAfter = Math.Round(Accounts[ID].CurrentBalance, 2);

        sw.Stop();
        await logger.LogInfo($"[{sw.ElapsedMilliseconds}ms] Updated Account #{ID} balance from {balanceBefore} to {balanceAfter}.");
    }

    /// <summary>
    /// Gets all Accounts from Accounts table as a dictionary with Account ID as key.
    /// </summary>
    /// <returns>Dictionary of Account objects</returns>
    async Task<Dictionary<int, Account>> GetAccountsAsDict()
    {
        await momoney.Init();
        var accounts = await momoney.db.Table<Account>().ToListAsync();
        return accounts.ToDictionary(a => a.AccountID, a => a);
    }
}