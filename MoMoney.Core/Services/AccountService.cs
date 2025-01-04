using CommunityToolkit.Mvvm.Messaging;
using SQLite;
using MoMoney.Core.Data;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Exceptions;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.Services;

/// <inheritdoc />
public class AccountService : BaseService<AccountService, UpdateAccountsMessage, string>, IAccountService
{
    public Dictionary<int, Account> Accounts { get; private set; } = new();

    public AccountService(IMoMoneydb _momoney, ILoggerService<AccountService> _logger) : base(_momoney, _logger) { }
    
    protected override async Task Init()
    {
        await base.Init();
        if (Accounts.Count == 0)
            Accounts = await GetAccountsAsDict();
    }

    public async Task<int> AddAccount(string accountName, string accountType, decimal startingBalance)
    {
        int numRows = 0;
        await DbOperation(async () =>
        {
            var count = await momoney.AccountsCountAsync(accountName);
            if (count > 0)
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
            numRows = await momoney.db.InsertAsync(account);
            Accounts.Add(account.AccountID, account);

            return $"Added Account #{account.AccountID} to db.";
        });

        WeakReferenceMessenger.Default.Send(new UpdateHomePageMessage());
        return numRows;
    }

    public async Task<int> AddAccounts(List<Account> accounts)
    {
        int numRows = 0;
        await DbOperation(async () =>
        {
            var dbAccounts = await momoney.AccountsToList();

            // checks if names of any new accounts matches any names from dbAccounts and throw exception if true
            bool containsDuplicates = accounts.Any(a => dbAccounts.Select(dba => dba.AccountName).Contains(a.AccountName));
            if (containsDuplicates)
                throw new DuplicateAccountException("Imported accounts contained duplicates. Please try again");

            // adds accounts to db and dictionary
            numRows = await momoney.db.InsertAllAsync(accounts);
            foreach (var acc in accounts)
                Accounts.Add(acc.AccountID, acc);

            return $"Added {accounts.Count} Accounts to db.";
        });

        WeakReferenceMessenger.Default.Send(new UpdateHomePageMessage());
        return numRows;
    }

    public async Task<int> UpdateAccount(Account updatedAccount)
    {
        int numRows = 0;
        await DbOperation(async () =>
        {
            numRows = await momoney.db.UpdateAsync(updatedAccount);
            Accounts[updatedAccount.AccountID] = updatedAccount;

            return $"Updated Account #{updatedAccount.AccountID} in db.";
        });

        WeakReferenceMessenger.Default.Send(new UpdateHomePageMessage());
        return numRows;
    }

    public async Task<int> UpdateBalance(int ID, decimal amount)
    {
        int numRows = 0;
        await DbOperation(async () =>
        {
            numRows = await momoney.db.ExecuteAsync($"UPDATE Account SET CurrentBalance=CurrentBalance + {amount} WHERE AccountID={ID}");
            if (numRows == 0)
                throw new AccountNotFoundException($"Could not find Account with ID '{ID}'.");

            decimal balanceBefore = Math.Round(Accounts[ID].CurrentBalance, 2);
            Accounts[ID].CurrentBalance += amount;
            decimal balanceAfter = Math.Round(Accounts[ID].CurrentBalance, 2);

            return $"Updated Account #{ID} balance from {balanceBefore} to {balanceAfter}.";
        }, false);

        return numRows;
    }

    public async Task<int> RemoveAccount(int ID)
    {
        int numRows = 0;
        await DbOperation(async () =>
        {
            numRows = await momoney.db.DeleteAsync<Account>(ID);
            Accounts.Remove(ID);

            return $"Removed Account #{ID} from db.";
        });

        WeakReferenceMessenger.Default.Send(new UpdateHomePageMessage());
        return numRows;
    }

    public async Task<bool> RemoveAllAccounts()
    {
        bool success = false;
        await DbOperation(async () =>
        {
            int deleteAll = await momoney.db.DeleteAllAsync<Account>();
            int dropTable = await momoney.db.DropTableAsync<Account>();
            CreateTableResult createTable = await momoney.db.CreateTableAsync<Account>();
            Accounts.Clear();
            success = deleteAll > 0 && dropTable == 1 && createTable == CreateTableResult.Created;

            return $"Removed all Accounts from db.";
        });

        WeakReferenceMessenger.Default.Send(new UpdateHomePageMessage());
        return success;
    }

    public async Task<Account?> GetAccount(int ID, bool tryGet = false)
    {
        await Init();
        if (Accounts.TryGetValue(ID, out var value))
            return new Account(value);

        var account = await momoney.FirstOrDefaultAccountAsync(ID);
        return account is null && !tryGet
            ? throw new AccountNotFoundException($"Could not find Account with ID '{ID}'.")
            : account;
    }

    public async Task<IEnumerable<Account>> GetAccounts()
    {
        await Init();
        var accounts = Accounts.Values;
        if (accounts.Count != 0)
            return accounts;
        return await momoney.AccountsToList();
    }

    public async Task<Dictionary<string, int>> GetAccountsAsNameDict()
    {
        await Init();
        var accounts = await momoney.AccountsToList();
        return accounts.ToDictionary(a => a.AccountName, a => a.AccountID);
    }

    public async Task<IEnumerable<Account>> GetActiveAccounts()
    {
        await Init();
        var accounts = Accounts.Values.Where(a => a.Enabled);
        if (accounts.Any())
            return accounts;
        return await momoney.db.Table<Account>().Where(a => a.Enabled).ToListAsync();
    }

    public async Task<IEnumerable<Account>> GetOrderedAccounts()
    {
        await Init();
        var accounts = Accounts.Values.OrderByDescending(a => a.Enabled)
                                      .ThenBy(a => a.AccountName);
        if (accounts.Any())
            return accounts;
        return await momoney.db.Table<Account>().Where(a => a.Enabled).ThenBy(a => a.AccountName).ToListAsync();
    }

    public async Task<int> GetAccountCount()
    {
        await Init();
        if (Accounts.Count != 0)
            return Accounts.Count;
        return await momoney.AccountsCountAsync();
    }

    /// <summary>
    /// Gets all Accounts from Accounts table as a dictionary with Account ID as key.
    /// </summary>
    /// <returns>Dictionary of Account objects</returns>
    async Task<Dictionary<int, Account>> GetAccountsAsDict()
    {
        await momoney.Init();
        var accounts = await momoney.AccountsToList();
        return accounts.ToDictionary(a => a.AccountID, a => a);
    }
}