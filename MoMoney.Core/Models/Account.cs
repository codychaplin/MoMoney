using SQLite;
using CsvHelper.Configuration.Attributes;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MoMoney.Core.Models;

#pragma warning disable CS0659
public partial class Account : ObservableObject
#pragma warning restore CS0659
{
    [PrimaryKey, AutoIncrement, CsvHelper.Configuration.Attributes.Ignore]
    public int AccountID { get; set; }
    [Index(0), ObservableProperty]
    public partial string AccountName { get; set; } = string.Empty;
    [Index(1), ObservableProperty]
    public partial string? AccountType { get; set; } = null;
    [Index(2), ObservableProperty]
    public partial decimal? StartingBalance { get; set; }
    [CsvHelper.Configuration.Attributes.Ignore]
    public decimal CurrentBalance { get; set; }
    [Index(3)]
    public bool Enabled { get; set; }

    public Account() { }

    public Account(int accountID, string accountName, string accountType, decimal? startingBalance, decimal currentBalance, bool enabled)
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

    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        Account account = (Account)obj;
        return AccountID == account.AccountID &&
               AccountName == account.AccountName &&
               AccountType == account.AccountType &&
               StartingBalance == account.StartingBalance &&
               CurrentBalance == account.CurrentBalance &&
               Enabled == account.Enabled;
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
    public string AccountType { get; set; } = string.Empty;
    public decimal Total { get; set; }
}