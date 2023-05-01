using MoMoney.Models;
using MoMoney.Exceptions;

namespace MoMoney.Services;

public static class TransactionService
{
    /// <summary>
    /// Creates new Transaction object and inserts into Transaction table.
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
        ValidateTransaction(date, accountID, amount, categoryID, subcategoryID, payee, transferID);

        var transaction = new Transaction
        {
            Date = date,
            AccountID = accountID,
            Amount = amount,
            CategoryID = categoryID,
            SubcategoryID = subcategoryID,
            Payee = payee.Trim(),
            TransferID = transferID
        };

        await MoMoneydb.db.InsertAsync(transaction);
        return transaction;
    }

    /// <summary>
    /// Inserts multiple Transaction objects into Transactions table.
    /// </summary>
    /// <param name="transactions"></param>
    public static async Task AddTransactions(List<Transaction> transactions)
    {
        await MoMoneydb.db.InsertAllAsync(transactions);
    }

    /// <summary>
    /// Given an Transaction object, updates the corresponding transaction in the Transactions table.
    /// </summary>
    /// <param name="updatedTransaction"></param>
    public static async Task UpdateTransaction(Transaction updatedTransaction)
    {
        ValidateTransaction(updatedTransaction.Date, updatedTransaction.AccountID, updatedTransaction.Amount,
                            updatedTransaction.CategoryID, updatedTransaction.SubcategoryID, updatedTransaction.Payee.Trim(),
                            updatedTransaction.TransferID);

        await MoMoneydb.db.UpdateAsync(updatedTransaction);
    }

    /// <summary>
    /// Removes Transaction from Transactions table.
    /// </summary>
    /// <param name="ID"></param>
    public static async Task RemoveTransaction(int ID)
    {
        await MoMoneydb.db.DeleteAsync<Transaction>(ID);
    }

    /// <summary>
    /// Drops Transactions table and re-initializes it.
    /// </summary>
    public static async Task ResetTransactions()
    {
        await MoMoneydb.db.DeleteAllAsync<Transaction>();
        await MoMoneydb.db.DropTableAsync<Transaction>();
        await MoMoneydb.db.CreateTableAsync<Transaction>();
    }

    /// <summary>
    /// Gets an transaction from the transactions table using an ID.
    /// </summary>
    /// <param name="ID"></param>
    /// <returns>Transaction object</returns>
    /// <exception cref="TransactionNotFoundException"></exception>
    public static async Task<Transaction> GetTransaction(int ID)
    {
        var transaction = await MoMoneydb.db.Table<Transaction>().FirstOrDefaultAsync(t => t.TransactionID == ID);
        if (transaction is null)
            throw new TransactionNotFoundException($"Could not find Transaction with ID '{ID}'.");
        else
            return new Transaction(transaction);
    }

    /// <summary>
    /// Gets all Transactions from Transaction table as a list.
    /// </summary>
    /// <returns>List of Transaction objects</returns>
    public static async Task<IEnumerable<Transaction>> GetTransactions()
    {
        return await MoMoneydb.db.Table<Transaction>().OrderBy(t => t.Date).ToListAsync();
    }

    public static async Task<IEnumerable<string>> GetPayeesFromTransactions()
    {
        return await MoMoneydb.db.QueryScalarsAsync<string>("SELECT DISTINCT Payee FROM \"Transaction\"");
    }

    /// <summary>
    /// Gets last 5 Transactions from Transaction table as a list.
    /// </summary>
    /// <returns>List of Transaction objects</returns>
    public static async Task<IEnumerable<Transaction>> GetRecentTransactions(DateTime to)
    {
        return await MoMoneydb.db.Table<Transaction>()
                                 .Where(t => t.Date <= to)
                                 .OrderByDescending(t => t.Date)
                                 .Take(5)
                                 .ToListAsync();
    }

    /// <summary>
    /// Gets all Transactions between specified dates from Transaction table as a list.
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="reverse"></param>
    /// <returns>List of Transaction objects between the specified dates</returns>
    public static async Task<IEnumerable<Transaction>> GetTransactionsFromTo(DateTime from, DateTime to, bool reverse)
    {
        if (reverse)
        {
            return await MoMoneydb.db.Table<Transaction>().Where(t => t.Date >= from && t.Date <= to)
                                                          .OrderByDescending(t => t.Date)
                                                          .ToListAsync();
        }
        else
        {
            return await MoMoneydb.db.Table<Transaction>().Where(t => t.Date >= from && t.Date <= to)
                                                          .OrderBy(t => t.Date)
                                                          .ToListAsync();
        }
    }

    /// <summary>
    /// Gets first Transaction from Transaction table.
    /// </summary>
    /// <returns>Transaction object</returns>
    public static async Task<Transaction> GetFirstTransaction()
    {
        return await MoMoneydb.db.Table<Transaction>().FirstOrDefaultAsync();
    }

    /// <summary>
    /// Validates input fields for Transactions.
    /// </summary>
    /// <param name="date"></param>
    /// <param name="accountID"></param>
    /// <param name="amount"></param>
    /// <param name="categoryID"></param>
    /// <param name="subcategoryID"></param>
    /// <param name="payee"></param>
    /// <param name="transferID"></param>
    /// <exception cref="InvalidTransactionException"></exception>
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
        if (categoryID != Constants.TRANSFER_ID && string.IsNullOrEmpty(payee))
            throw new InvalidTransactionException("Payee cannot be blank");
        if (transferID != null && transferID < 1)
            throw new InvalidTransactionException("Invalid Transfer Account");
    }
}
