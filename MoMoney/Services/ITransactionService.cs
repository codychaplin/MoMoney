using MoMoney.Models;
using MoMoney.Exceptions;

namespace MoMoney.Services;

public interface ITransactionService
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
    Task<Transaction> AddTransaction(DateTime date, int accountID, decimal amount, int categoryID,
        int subcategoryID, string payee, int? transferID);

    /// <summary>
    /// Inserts multiple Transaction objects into Transactions table.
    /// </summary>
    /// <param name="transactions"></param>
    Task AddTransactions(List<Transaction> transactions);

    /// <summary>
    /// Given an Transaction object, updates the corresponding transaction in the Transactions table.
    /// </summary>
    /// <param name="updatedTransaction"></param>
    Task UpdateTransaction(Transaction updatedTransaction);

    /// <summary>
    /// Removes Transaction from Transactions table.
    /// </summary>
    /// <param name="ID"></param>
    Task RemoveTransaction(int ID);

    /// <summary>
    /// Drops Transactions table and re-initializes it.
    /// </summary>
    Task RemoveAllTransactions();

    /// <summary>
    /// Gets an transaction from the transactions table using an ID.
    /// </summary>
    /// <param name="ID"></param>
    /// <returns>Transaction object</returns>
    /// <exception cref="TransactionNotFoundException"></exception>
    Task<Transaction> GetTransaction(int ID);

    /// <summary>
    /// Gets all Transactions from Transaction table as a list.
    /// </summary>
    /// <returns>List of Transaction objects</returns>
    Task<IEnumerable<Transaction>> GetTransactions();

    /// <summary>
    /// Gets Transactions from Transaction table that match parameters as a list.
    /// </summary>
    /// <returns>List of Transaction objects</returns>
    Task<List<Transaction>> GetFilteredTransactions(Account account, Category category, Category subcategory, string payee);

    /// <summary>
    /// Gets all distinct payees from Transactions table.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<string>> GetPayeesFromTransactions();

    /// <summary>
    /// Gets all Transactions between specified dates from Transaction table as a list.
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="reverse"></param>
    /// <returns>List of Transaction objects between the specified dates</returns>
    Task<IEnumerable<Transaction>> GetTransactionsFromTo(DateTime from, DateTime to, bool reverse);

    /// <summary>
    /// Gets first Transaction from Transaction table.
    /// </summary>
    /// <returns>Transaction object</returns>
    Task<Transaction> GetFirstTransaction();

    /// <summary>
    /// Gets total number of Transactions in db.
    /// </summary>
    /// <returns>Integer representing number of Transactions</returns>
    Task<int> GetTransactionCount();
}
