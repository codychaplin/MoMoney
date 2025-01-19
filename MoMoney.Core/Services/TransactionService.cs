using CommunityToolkit.Mvvm.Messaging;
using MoMoney.Core.Data;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Exceptions;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.Services;

/// <inheritdoc />
public class TransactionService : BaseService<TransactionService, UpdateTransactionsMessage, TransactionEventArgs?>, ITransactionService
{
    readonly IAccountService accountService;

    public TransactionService(IMoMoneydb _momoney, ILoggerService<TransactionService> _logger, IAccountService _accountService) : base(_momoney, _logger)
    {
        accountService = _accountService;
    }

    public async Task<int> AddTransaction(DateTime date, int accountID, decimal amount, int categoryID,
        int subcategoryID, string payee, int? transferID)
    {
        Transaction transaction = new();

        // add transaction
        await DbOperation(async () =>
        {
            ValidateTransaction(date, accountID, amount, categoryID, subcategoryID, payee, transferID);
            transaction = new Transaction(date, accountID, amount, categoryID, subcategoryID, payee.Trim(), transferID);
            await momoney.db.InsertAsync(transaction);

            return $"Added Transaction #{transaction.TransactionID} to db.";
        }, false);

        // update account balance
        await accountService.UpdateBalance(accountID, amount);

        // send message to update UI
        var args = new TransactionEventArgs(transaction, TransactionEventArgs.CRUD.Create);
        WeakReferenceMessenger.Default.Send(new UpdateTransactionsMessage(args));
        WeakReferenceMessenger.Default.Send(new UpdateHomePageMessage());

        return transaction.TransactionID;
    }

    public async Task AddTransactions(List<Transaction> transactions)
    {
        await DbOperation(async () =>
        {
            await momoney.db.InsertAllAsync(transactions);

            return $"Added {transactions.Count} Transactions to db.";
        }, false);

        WeakReferenceMessenger.Default.Send(new UpdateHomePageMessage());
    }

    public async Task UpdateTransaction(Transaction updatedTransaction)
    {
        // update transaction
        await DbOperation(async () =>
        {
            ValidateTransaction(updatedTransaction.Date, updatedTransaction.AccountID, updatedTransaction.Amount,
                updatedTransaction.CategoryID, updatedTransaction.SubcategoryID,
                updatedTransaction.Payee?.Trim() ?? string.Empty, updatedTransaction.TransferID);
            await momoney.db.UpdateAsync(updatedTransaction);

            return $"Updated Transaction #{updatedTransaction.TransactionID} in db.";
        }, false);

        // send message to update UI
        var args = new TransactionEventArgs(updatedTransaction, TransactionEventArgs.CRUD.Update);
        WeakReferenceMessenger.Default.Send(new UpdateTransactionsMessage(args));
        WeakReferenceMessenger.Default.Send(new UpdateHomePageMessage());
    }

    public async Task RemoveTransaction(Transaction transaction)
    {
        // remove transaction
        await DbOperation(async () =>
        {
            await momoney.db.DeleteAsync<Transaction>(transaction.TransactionID);

            return $"Removed Transaction #{transaction.TransactionID} from db.";
        }, false);

        // update account balance
        bool isIncomeOrTrasfer = transaction.CategoryID == Constants.INCOME_ID || transaction.CategoryID == Constants.TRANSFER_ID;
        decimal amount = isIncomeOrTrasfer ? -transaction.Amount : transaction.Amount;
        await accountService.UpdateBalance(transaction.AccountID, amount);

        // send message to update UI
        var args = new TransactionEventArgs(transaction, TransactionEventArgs.CRUD.Delete);
        WeakReferenceMessenger.Default.Send(new UpdateTransactionsMessage(args));
        WeakReferenceMessenger.Default.Send(new UpdateHomePageMessage());
    }

    public async Task RemoveAllTransactions()
    {
        await DbOperation(async () =>
        {
            await momoney.db.DeleteAllAsync<Transaction>();
            await momoney.db.DropTableAsync<Transaction>();
            await momoney.db.CreateTableAsync<Transaction>();

            return $"Removed all Transactions from db.";
        }, false);

        var args = new TransactionEventArgs(null, TransactionEventArgs.CRUD.Read);
        WeakReferenceMessenger.Default.Send(new UpdateTransactionsMessage(args));
        WeakReferenceMessenger.Default.Send(new UpdateHomePageMessage());
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
        return await momoney.db.Table<Transaction>().OrderBy(t => t.Date).ThenBy(t => t.TransactionID).ToListAsync();
    }
    
    public async Task<List<Transaction>> GetFilteredTransactions(DateTime? from = null, DateTime? to = null, int? accountID = null,
        decimal? minAmount = null, decimal? maxAmount = null, int? categoryID = null, int? subcategoryID = null, string? payee = null)
    {
        await momoney.Init();

        var transactionsQuery = momoney.db.Table<Transaction>();
        if (from != null)
            transactionsQuery = transactionsQuery.Where(t => t.Date >= from);
        if (to != null)
            transactionsQuery = transactionsQuery.Where(t => t.Date <= to);
        if (accountID != null)
            transactionsQuery = transactionsQuery.Where(t => t.AccountID == accountID);
        if (minAmount != null && minAmount != 0)
            transactionsQuery = transactionsQuery.Where(t => Math.Abs(t.Amount) >= minAmount);
        if (maxAmount != null && maxAmount != 500) // 500 is the max on the scale, so only filter when it's changed
            transactionsQuery = transactionsQuery.Where(t => Math.Abs(t.Amount) <= maxAmount);
        if (categoryID != null)
            transactionsQuery = transactionsQuery.Where(t => t.CategoryID == categoryID);
        if (subcategoryID != null)
            transactionsQuery = transactionsQuery.Where(t => t.SubcategoryID == subcategoryID);
        if (!string.IsNullOrEmpty(payee))
            transactionsQuery = transactionsQuery.Where(t => t.Payee == payee);

        return await transactionsQuery.OrderByDescending(t => t.Date).ThenBy(t => t.TransactionID).ToListAsync();
    }

    public async Task<IEnumerable<string>> GetPayeesFromTransactions()
    {
        await momoney.Init();
        return await momoney.db.QueryScalarsAsync<string>("SELECT DISTINCT Payee FROM \"Transaction\"");
    }

    public async Task<List<Transaction>> GetTransactionsFromTo(DateTime from, DateTime to, bool reverse)
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

    public async Task<int> GetTransactionCount()
    {
        await momoney.Init();
        return await momoney.db.Table<Transaction>().CountAsync();
    }

    public async Task CalculateAccountBalances()
    {
        var transactions = await GetTransactions();
        var accounts = await accountService.GetAccounts();
        if (!transactions.Any() || !accounts.Any())
            return;

        // group transactions by account, sum amounts, and convert to dictionary
        var currentBalances = transactions.GroupBy(t => t.AccountID)
                                          .Select(g => new
                                          {
                                              g.First().AccountID,
                                              Balance = g.Sum(t => t.Amount)
                                          })
                                          .ToDictionary(a => a.AccountID, a => a.Balance);

        // get accounts that only exist in currentBalances
        var matchingAccounts = accounts.Where(a => currentBalances.ContainsKey(a.AccountID));

        // update current balance in db
        foreach (var account in matchingAccounts)
        {
            account.CurrentBalance = account.StartingBalance + currentBalances[account.AccountID];
            await accountService.UpdateAccount(account);
        }
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
        if (categoryID == Constants.TRANSFER_ID)
        {
            if (transferID < 1)
                throw new InvalidTransactionException("Invalid Transfer Account");
            if (accountID == transferID)
                throw new InvalidTransactionException("Cannot transfer to the same account.");
        }
    }
}