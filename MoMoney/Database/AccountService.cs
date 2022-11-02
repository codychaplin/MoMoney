using SQLite;
using MoMoney.Models;

namespace MoMoney.Database
{
    public static class AccountService
    {

        /// <summary>
        /// Creates Account table, if none exist
        /// </summary>
        /// <returns>Whether table was created</returns>
        public static async Task Init()
        {
            MoMoneydb.Init();
            await MoMoneydb.db.CreateTableAsync<Account>();
        }

        /// <summary>
        /// Creates new Account object and inserts into Accounts table
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="accountName"></param>
        /// <param name="accountTypeID"></param>
        /// <param name="startingBalance"></param>
        /// <param name="currentBalance"></param>
        /// <param name="enabled"></param>
        /// <returns>Number of rows added to Accounts table</returns>
        public static async Task AddAccount(int ID, string accountName, int accountTypeID,
            decimal startingBalance, decimal currentBalance, bool enabled)
        {
            await Init();

            var Account = new Account(ID, accountName, accountTypeID, startingBalance, currentBalance, enabled);
            var id = await MoMoneydb.db.InsertAsync(Account);
        }

        /// <summary>
        /// Removes Account from Accounts table
        /// </summary>
        /// <param name="ID"></param>
        /// <returns>The number of objects deleted</returns>
        public static async Task RemoveAccount(int ID)
        {
            await Init();

            await MoMoneydb.db.DeleteAsync<Account>(ID);
        }

        /// <summary>
        /// Gets all Accounts from Accounts table as a list
        /// </summary>
        /// <returns>List of Account objects</returns>
        public static async Task<IEnumerable<Account>> GetAccounts()
        {
            await Init();

            var accounts = await MoMoneydb.db.Table<Account>().ToListAsync();
            return accounts;
        }
    }
}
