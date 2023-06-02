using SQLite;

namespace MoMoney.Models;
public class Account
{
    [PrimaryKey, AutoIncrement]
    public int AccountID { get; set; }
    public string AccountName { get; set; }
    public string AccountType { get; set; }
    public decimal StartingBalance { get; set; }
    public decimal CurrentBalance { get; set; }
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

// account type 
public enum AccountType
{
    Checkings,
    Savings,
    Credit,
    Investments
}