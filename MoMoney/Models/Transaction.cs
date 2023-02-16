using SQLite;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MoMoney.Models;

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
