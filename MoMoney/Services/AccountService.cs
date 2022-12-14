using MoMoney.Exceptions;
using MoMoney.Models;

namespace MoMoney.Services
{
    public static class AccountService
    {

        /// <summary>
        /// Calls db Init.
        /// </summary>
        public static async Task Init()
        {
            await MoMoneydb.Init();
        }

        /// <summary>
        /// Creates new Account object and inserts into Accounts table.
        /// </summary>
        /// <param name="accountName"></param>
        /// <param name="accountType"></param>
        /// <param name="startingBalance"></param>
        /// <param name="enabled"></param>
        /// <exception cref="DuplicateAccountException"></exception>
        public static async Task AddAccount(string accountName, string accountType,
            decimal startingBalance, bool enabled)
        {
            await Init();

            var res = await MoMoneydb.db.Table<Account>().CountAsync(a => a.AccountName == accountName);
            if (res > 0)
                throw new DuplicateAccountException("Account named '" + accountName + "' already exists");

            var Account = new Account
            {
                AccountName = accountName,
                AccountType = accountType,
                StartingBalance = startingBalance,
                CurrentBalance = startingBalance,
                Enabled = enabled
            };

            await MoMoneydb.db.InsertAsync(Account);
        }

        /// <summary>
        /// Inserts multiple Account objects into Accounts table.
        /// </summary>
        /// <param name="accounts"></param>
        public static async Task AddAccounts(List<Account> accounts)
        {
            await Init();

            var dbAccounts = await MoMoneydb.db.Table<Account>().ToListAsync(); // gets accounts from db
            // gets names of all accounts where name matches any names of accounts in parameter accounts
            var accs = dbAccounts.Select(a => a.AccountName)
                                 .Where(a1 => accounts.Any(a2 => a1.Contains(a2.AccountName))).ToList();
            
            // displays duplicate accounts on screen, if any
            string names = "";
            if (accs.Count() > 0)
            {
                names = accs[0];
                if (accs.Count() > 1)
                {
                    for (int i = 1; i < accs.Count(); i++)
                        names += ", " + accs[i];
                    names += " are duplicates, ";
                }
                else
                    names += " is a duplicate, ";

                await Shell.Current.DisplayAlert("Attention", names + "all other accounts were added", "OK");
            }

            accounts.RemoveAll(a => accs.Contains(names));

            await MoMoneydb.db.InsertAllAsync(accounts);
        }

        /// <summary>
        /// Given an Account object, updates the corresponding account in the Accounts table.
        /// </summary>
        /// <param name="updatedAccount"></param>
        public static async Task UpdateAccount(Account updatedAccount)
        {
            await Init();

            await MoMoneydb.db.UpdateAsync(updatedAccount);
        }

        /// <summary>
        /// Removes Account from Accounts table.
        /// </summary>
        /// <param name="ID"></param>
        public static async Task RemoveAccount(int ID)
        {
            await Init();

            await MoMoneydb.db.DeleteAsync<Account>(ID);
        }

        /// <summary>
        /// Removes ALL Accounts from Accounts table.
        /// </summary>
        public static async Task RemoveAllAccounts()
        {
            await Init();

            await MoMoneydb.db.DeleteAllAsync<Account>();
        }

        /// <summary>
        /// Gets an account from the Accounts table using an ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Account object</returns>
        public static async Task<Account> GetAccount(int id)
        {
            var account = await MoMoneydb.db.Table<Account>().FirstOrDefaultAsync(a => a.AccountID == id);

            return account;
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
        /// Gets all Accounts from Accounts table as a dictionary with Account Name as Key.
        /// </summary>
        /// <returns>Dictionary of Account objects</returns>
        public static async Task<Dictionary<string, int>> GetAccountsAsDictWithName()
        {
            await Init();

            var accounts = await MoMoneydb.db.Table<Account>().ToListAsync();
            return accounts.ToDictionary(a => a.AccountName, a => a.AccountID);
        }

        /// <summary>
        /// Gets all Accounts from Accounts table as a dictionary with Account ID as Key.
        /// </summary>
        /// <returns>Dictionary of Account objects</returns>
        public static async Task<Dictionary<int, string>> GetAccountsAsDictWithID()
        {
            await Init();

            var accounts = await MoMoneydb.db.Table<Account>().ToListAsync();
            var accountsDict = accounts.ToDictionary(a => a.AccountID, a => a.AccountName);
            return accountsDict;
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
        }
    }
}
