using MoMoney.Data;
using MoMoney.Models;
using MoMoney.Exceptions;

namespace MoMoney.Services;

/// <inheritdoc />
public class TransactionService : ITransactionService
{
    readonly MoMoneydb momoney;
    readonly ILoggerService<TransactionService> logger;

    public TransactionService(MoMoneydb _momoney, ILoggerService<TransactionService> _logger)
    {
        momoney = _momoney;
        logger = _logger;
    }

    public async Task<Transaction> AddTransaction(DateTime date, int accountID, decimal amount, int categoryID,
        int subcategoryID, string payee, int? transferID)
    {
        await momoney.Init();
        
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

        await momoney.db.InsertAsync(transaction);
        await logger.LogInfo($"Added Transaction#{transaction.TransactionID} to db.");
        return transaction;
    }

    public async Task AddTransactions(List<Transaction> transactions)
    {
        await momoney.Init();
        await momoney.db.InsertAllAsync(transactions);
        await logger.LogInfo($"Added {transactions.Count} Transactions to db.");
    }

    public async Task UpdateTransaction(Transaction updatedTransaction)
    {
        await momoney.Init();
        ValidateTransaction(updatedTransaction.Date, updatedTransaction.AccountID, updatedTransaction.Amount,
                            updatedTransaction.CategoryID, updatedTransaction.SubcategoryID,
                            updatedTransaction.Payee.Trim(), updatedTransaction.TransferID);
        await momoney.db.UpdateAsync(updatedTransaction);
        await logger.LogInfo($"Updated Transaction#{updatedTransaction.TransactionID} in db.");
    }

    public async Task RemoveTransaction(int ID)
    {
        await momoney.Init();
        await momoney.db.DeleteAsync<Transaction>(ID);
        await logger.LogInfo($"Removed Transaction#{ID} from db.");
    }

    public async Task RemoveAllTransactions()
    {
        await momoney.Init();
        await momoney.db.DeleteAllAsync<Transaction>();
        await momoney.db.DropTableAsync<Transaction>();
        await momoney.db.CreateTableAsync<Transaction>();
        await logger.LogInfo($"Removed all Transactions from db.");
    }

    public async Task<Transaction> GetTransaction(int ID)
    {
        await momoney.Init();
        var transaction = await momoney.db.Table<Transaction>().FirstOrDefaultAsync(t => t.TransactionID == ID);
        return transaction is null
            ? throw new TransactionNotFoundException($"Could not find Transaction with ID '{ID}'.")
            : new Transaction(transaction);
    }

    public async Task<IEnumerable<Transaction>> GetTransactions()
    {
        await momoney.Init();
        return await momoney.db.Table<Transaction>().OrderBy(t => t.Date).ToListAsync();
    }

    public async Task<IEnumerable<string>> GetPayeesFromTransactions()
    {
        await momoney.Init();
        return await momoney.db.QueryScalarsAsync<string>("SELECT DISTINCT Payee FROM \"Transaction\"");
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsFromTo(DateTime from, DateTime to, bool reverse)
    {
        await momoney.Init();
        if (reverse)
        {
            
            return await momoney.db.Table<Transaction>().Where(t => t.Date >= from && t.Date <= to)
                                                        .OrderByDescending(t => t.Date)
                                                        .ToListAsync();
        }

        return await momoney.db.Table<Transaction>().Where(t => t.Date >= from && t.Date <= to)
                                                    .OrderBy(t => t.Date)
                                                    .ToListAsync();
    }

    public async Task<Transaction> GetFirstTransaction()
    {
        await momoney.Init();
        return await momoney.db.Table<Transaction>().FirstOrDefaultAsync();
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
    void ValidateTransaction(DateTime date, int accountID, decimal amount, int categoryID,
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