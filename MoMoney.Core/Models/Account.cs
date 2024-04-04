using SQLite;
using CsvHelper.Configuration.Attributes;

namespace MoMoney.Core.Models;

public class Account
{
    [PrimaryKey, AutoIncrement, CsvHelper.Configuration.Attributes.Ignore]
    public int AccountID { get; set; }
    [Index(0)]
    public string AccountName { get; set; }
    [Index(1)]
    public string AccountType { get; set; }
    [Index(2)]
    public decimal StartingBalance { get; set; }
    [CsvHelper.Configuration.Attributes.Ignore]
    public decimal CurrentBalance { get; set; }
    [Index(3)]
    public bool Enabled { get; set; }

    public Account() { }

    public Account(int accountID, string accountName, string accountType, decimal startingBalance, decimal currentBalance, bool enabled)
    {
        AccountID = accountID;
        AccountName = accountName;
        AccountType = accountType;
        StartingBalance = startingBalance;
        CurrentBalance = currentBalance;
        Enabled = enabled;
    }

    public Account(Account account)
    {
        AccountID = account.AccountID;
        AccountName = account.AccountName;
        AccountType = account.AccountType;
        StartingBalance= account.StartingBalance;
        CurrentBalance = account.CurrentBalance;
        Enabled = account.Enabled;
    }
}

public enum AccountType // account types
{
    Checkings,
    Savings,
    Credit,
    Investments
}

public class AccountTotalModel
{
    public string AccountType { get; set; }
    public decimal Total { get; set; }
}