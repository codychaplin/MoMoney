using MoMoney.Models;

namespace MoMoney.Services
{
    public static class TransactionService
    {
        /// <summary>
        /// Creates Category table, if doesn't exist, add default categories
        /// </summary>
        public static async Task Init()
        {
            await MoMoneydb.Init();
        }

        /// <summary>
        /// Creates new Transaction object and inserts into Transaction table
        /// </summary>
        /// <param name="date"></param>
        /// <param name="accountID"></param>
        /// <param name="amount"></param>
        /// <param name="categoryID"></param>
        /// <param name="subcategoryID"></param>
        /// <param name="payee"></param>
        /// <param name="transferID"></param>
        public static async Task AddTransaction(DateTime date, int accountID, decimal amount, int categoryID, int subcategoryID, string payee, int? transferID)
        {
            await Init();

            if (accountID > 0 || categoryID > 0 || subcategoryID > 0)
            {
                if (accountID != transferID)
                {
                    var transaction = new Transaction
                    {
                        Date = date,
                        AccountID = accountID,
                        Amount = amount,
                        CategoryID = categoryID,
                        SubcategoryID = subcategoryID,
                        Payee = payee,
                        TransferID = transferID
                    };

                    await MoMoneydb.db.InsertAsync(transaction);
                }
                else
                {
                    throw new Exception("Cannot transfer to and from the same account");
                }
            }
            else
            {
                throw new Exception("Transaction not valid");
            }
        }

        /// <summary>
        /// Given an Transaction object, updates the corresponding transaction in the Transactions table
        /// </summary>
        /// <param name="updatedTransaction"></param>
        public static async Task UpdateTransaction(Transaction updatedTransaction)
        {
            await Init();

            await MoMoneydb.db.UpdateAsync(updatedTransaction);
        }

        /// <summary>
        /// Gets an transaction from the transactions table using an ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Transaction object</returns>
        public static async Task<Transaction> GetTransaction(int id)
        {
            await Init();

            return await MoMoneydb.db.Table<Transaction>().FirstOrDefaultAsync(t => t.TransactionID == id);
        }

        /// <summary>
        /// Gets all Transactions from Transaction table as a list
        /// </summary>
        /// <returns>List of Transaction objects</returns>
        public static async Task<IEnumerable<Transaction>> GetTransactions()
        {
            await Init();

            return await MoMoneydb.db.Table<Transaction>().OrderBy(t => t.Date).ToListAsync();
        }

        /// <summary>
        /// Gets last 5 Transactions from Transaction table as a list
        /// </summary>
        /// <returns>List of Transaction objects</returns>
        public static async Task<IEnumerable<Transaction>> GetRecentTransactions()
        {
            await Init();

            return await MoMoneydb.db.Table<Transaction>().OrderByDescending(t => t.Date).Take(5).ToListAsync();
        }



        /// <summary>
        /// Removes Transaction from Transactions table
        /// </summary>
        /// <param name="ID"></param>
        public static async Task RemoveTransaction(int ID)
        {
            await Init();

            await MoMoneydb.db.DeleteAsync<Transaction>(ID);
        }
    }
}
