using MoMoney.Models;

namespace MoMoney.Services
{
    public static class AccountService
    {

        /// <summary>
        /// Calls db Init
        /// </summary>
        public static async Task Init()
        {
            await MoMoneydb.Init();
        }

        /// <summary>
        /// Creates new Account object and inserts into Accounts table
        /// </summary>
        /// <param name="accountName"></param>
        /// <param name="accountType"></param>
        /// <param name="startingBalance"></param>
        /// <param name="enabled"></param>
        public static async Task AddAccount(string accountName, string accountType,
            decimal startingBalance, bool enabled)
        {
            await Init();

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
        /// Given an Account object, updates the corresponding account in the Accounts table
        /// </summary>
        /// <param name="account"></param>
        public static async Task UpdateAccount(Account account)
        {
            await Init();

            var updatedAccount = new Account
            {
                AccountID = account.AccountID,
                AccountName = account.AccountName,
                AccountType = account.AccountType,
                StartingBalance = account.StartingBalance,
                CurrentBalance = account.StartingBalance,
                Enabled = account.Enabled
            };

            await MoMoneydb.db.UpdateAsync(updatedAccount);
        }

        /// <summary>
        /// Removes Account from Accounts table
        /// </summary>
        /// <param name="ID"></param>
        public static async Task RemoveAccount(int ID)
        {
            await Init();

            await MoMoneydb.db.DeleteAsync<Account>(ID);
        }

        /// <summary>
        /// Gets an account from the Accounts table using an ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Account object</returns>
        public static async Task<Account> GetAccount(int id)
        {
            var account = await MoMoneydb.db.Table<Account>().FirstOrDefaultAsync(a => a.AccountID == id);

            return account;
        }

        /// <summary>
        /// Gets all Accounts from Accounts table as a list
        /// </summary>
        /// <returns>List of Account objects</returns>
        public static async Task<IEnumerable<Account>> GetAccounts()
        {
            await Init();

            return await MoMoneydb.db.Table<Account>().ToListAsync();
        }

        /// <summary>
        /// Gets enabled accounts from Accounts table as a list
        /// </summary>
        /// <returns>List of active Account objects</returns>
        public static async Task<IEnumerable<Account>> GetActiveAccounts()
        {
            await Init();

            return await MoMoneydb.db.Table<Account>().Where(a => a.Enabled).ToListAsync();
        }
    }
}
