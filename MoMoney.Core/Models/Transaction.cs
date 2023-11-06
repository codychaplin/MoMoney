using SQLite;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Converters;
using CsvHelper.Configuration;
using MoMoney.Core.Services;

namespace MoMoney.Core.Models;

#pragma warning disable CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()

public partial class Transaction : ObservableObject
{
    [PrimaryKey, AutoIncrement]
    public int TransactionID { get; set; }

    [ObservableProperty]
    public DateTime date;

    [ObservableProperty]
    public int accountID;

    [ObservableProperty]
    public decimal amount;

    [ObservableProperty]
    public int categoryID;

    [ObservableProperty]
    public int subcategoryID;

    [ObservableProperty]
    public string payee;

    [ObservableProperty]
    public int? transferID;

    public Transaction() { }

    public Transaction(int transactionID, DateTime date, int accountID, decimal amount, int categoryID, int subcategoryID, string payee, int? transferID)
    {
        TransactionID = transactionID;
        Date = date;
        AccountID = accountID;
        Amount = amount;
        CategoryID = categoryID;
        SubcategoryID = subcategoryID;
        Payee = payee;
        TransferID = transferID;
    }

    public Transaction(Transaction transaction)
    {
        TransactionID = transaction.TransactionID;
        Date = transaction.Date;
        AccountID = transaction.AccountID;
        Amount = transaction.Amount;
        CategoryID = transaction.CategoryID;
        SubcategoryID = transaction.SubcategoryID;
        Payee = transaction.Payee;
        TransferID = transaction.TransferID;
    }

    public static bool operator ==(Transaction trans1, Transaction trans2)
    {
        return Compare(ref trans1, ref trans2);

    }

    public static bool operator !=(Transaction trans1, Transaction trans2)
    {
        return !Compare(ref trans1, ref trans2);
    }

    static bool Compare(ref Transaction trans1, ref Transaction trans2)
    {
        return trans1.Date == trans2.Date &&
               trans1.AccountID == trans2.AccountID &&
               trans1.Amount == trans2.Amount &&
               trans1.CategoryID == trans2.CategoryID &&
               trans1.SubcategoryID == trans2.SubcategoryID &&
               trans1.Payee == trans2.Payee &&
               trans1.TransferID == trans2.TransferID;
    }
}

public class TransactionImportMap : ClassMap<Transaction>
{
    public TransactionImportMap()
    {
        Map(t => t.TransactionID).Ignore();
        Map(t => t.Date).Index(0);
        Map(t => t.AccountID).Index(1).TypeConverter<TransactionImportConverter>();
        Map(t => t.Amount).Index(2);
        Map(t => t.CategoryID).Index(3).TypeConverter<TransactionImportConverter>();
        Map(t => t.SubcategoryID).Index(4).TypeConverter<TransactionImportConverter>();
        Map(t => t.Payee).Index(5).TypeConverter<TransactionImportConverter>();
        Map(t => t.TransferID).Ignore();
    }
}

public class TransactionExportMap : ClassMap<Transaction>
{
    public TransactionExportMap()
    {
        Map(t => t.TransactionID).Ignore();
        Map(t => t.Date).Index(0).TypeConverterOption.Format("yyyy-MM-dd");
        Map(t => t.AccountID).Index(1).TypeConverter<TransactionExportConverter>();
        Map(t => t.Amount).Index(2);
        Map(t => t.CategoryID).Index(3).TypeConverter<TransactionExportConverter>();
        Map(t => t.SubcategoryID).Index(4).TypeConverter<TransactionExportConverter>();
        Map(t => t.Payee).Index(5);
        Map(t => t.TransferID).Ignore();
    }
}