﻿using MoMoney.Core.Models;
using MoMoney.Core.Exceptions;

namespace MoMoney.Core.Services.Interfaces;

public interface IAccountService
{
    /// <summary>
    /// Cached dictionary of Accounts
    /// </summary>
    Dictionary<int, Account> Accounts { get; }

    /// <summary>
    /// Creates new Account object and inserts into Accounts table.
    /// </summary>
    /// <param name="accountName"></param>
    /// <param name="accountType"></param>
    /// <param name="startingBalance"></param>
    /// <exception cref="DuplicateAccountException"></exception>
    Task<int> AddAccount(string accountName, string accountType, decimal startingBalance);

    /// <summary>
    /// Inserts multiple Account objects into Accounts table.
    /// </summary>
    /// <param name="accounts"></param>
    /// <exception cref="DuplicateAccountException"></exception>
    Task<int> AddAccounts(List<Account> accounts);

    /// <summary>
    /// Given an Account object, updates the corresponding account in the Accounts table.
    /// </summary>
    /// <param name="updatedAccount"></param>
    Task<int> UpdateAccount(Account updatedAccount);

    /// <summary>
    /// Updates CurrentBalance of specified Account.
    /// </summary>
    /// <param name="ID"></param>
    /// <param name="amount"></param>
    /// <exception cref="AccountNotFoundException"></exception>
    Task<int> UpdateBalance(int ID, decimal amount);

    /// <summary>
    /// Removes Account from Accounts table.
    /// </summary>
    /// <param name="ID"></param>
    Task<int> RemoveAccount(int ID);

    /// <summary>
    /// Removes ALL Accounts from Accounts table.
    /// </summary>
    Task<bool> RemoveAllAccounts();

    /// <summary>
    /// Gets an account from the Accounts table using an ID.
    /// </summary>
    /// <param name="ID"></param>
    /// <param name="tryGet"></param>
    /// <returns>Account object</returns>
    /// <exception cref="AccountNotFoundException"></exception>
    Task<Account?> GetAccount(int ID, bool tryGet = false);

    /// <summary>
    /// Gets all Accounts from Accounts table as a list.
    /// </summary>
    /// <returns>List of Account objects</returns>
    Task<IEnumerable<Account>> GetAccounts();

    /// <summary>
    /// Gets all Accounts from Accounts table as a dictionary with AccountName as Key.
    /// </summary>
    /// <returns>Dictionary of Account objects</returns>
    Task<Dictionary<string, int>> GetAccountsAsNameDict();

    /// <summary>
    /// Gets enabled accounts from Accounts table as a list.
    /// </summary>
    /// <returns>List of active Account objects</returns>
    Task<IEnumerable<Account>> GetActiveAccounts();

    /// <summary>
    /// Gets all Accounts from Accounts table ordered by Enabled and then AccountName.
    /// </summary>
    /// <returns>List of ordered Account objects</returns>
    Task<IEnumerable<Account>> GetOrderedAccounts();

    /// <summary>
    /// Gets total number of Accounts in db.
    /// </summary>
    /// <returns>Integer representing number of Accounts</returns>
    Task<int> GetAccountCount();
}