using MoMoney.Models;
using MoMoney.Exceptions;

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
        /// <returns>Newly created Transaction</returns>
        public static async Task<Transaction> AddTransaction(DateTime date, int accountID, decimal amount, int categoryID,
            int subcategoryID, string payee, int? transferID)
        {
            await Init();

            ValidateTransaction(date, accountID, amount, categoryID, subcategoryID, payee, transferID);

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
            return transaction;
        }

        /// <summary>
        /// Given an Transaction object, updates the corresponding transaction in the Transactions table
        /// </summary>
        /// <param name="updatedTransaction"></param>
        public static async Task UpdateTransaction(Transaction updatedTransaction)
        {
            await Init();

            ValidateTransaction(updatedTransaction.Date, updatedTransaction.AccountID, updatedTransaction.Amount,
                                updatedTransaction.CategoryID, updatedTransaction.SubcategoryID, updatedTransaction.Payee,
                                updatedTransaction.TransferID);

            await MoMoneydb.db.UpdateAsync(updatedTransaction);
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

        /// <summary>
        /// Gets an transaction from the transactions table using an ID
        /// </summary>
        /// <param name="ID"></param>
        /// <returns>Transaction object</returns>
        public static async Task<Transaction> GetTransaction(int ID)
        {
            await Init();

            var transaction = await MoMoneydb.db.Table<Transaction>().FirstOrDefaultAsync(t => t.TransactionID == ID);

            if (transaction is null)
                throw new TransactionNotFoundException();
            else
                return transaction;
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

        static void ValidateTransaction(DateTime date, int accountID, decimal amount, int categoryID,
            int subcategoryID, string payee, int? transferID)
        {
            if (date.Year < 2000)
                throw new InvalidTransactionException("Invalid Date");
            if (accountID < 1)
                throw new InvalidTransactionException("Invalid Account");
            if (amount == 0)
                throw new InvalidTransactionException("Amount cannot be 0");
            if (categoryID < 1)
                throw new InvalidTransactionException("Invalid Category");
            if (subcategoryID < 1)
                throw new InvalidTransactionException("Invalid Subcategory");
            if (string.IsNullOrEmpty(payee))
                throw new InvalidTransactionException("Payee cannot be blank");
            if (transferID != null || transferID < 1)
                throw new InvalidTransactionException("Invalid Transfer Account");
            if (accountID == transferID)
                throw new InvalidTransactionException("Cannot transfer to and from the same Account");
        }
    }
}
