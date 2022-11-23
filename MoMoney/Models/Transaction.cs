using SQLite;

namespace MoMoney.Models
{
    #pragma warning disable CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
    #pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()

    public class Transaction
    {
        [PrimaryKey, AutoIncrement]
        public int TransactionID { get; set; }
        public DateTime Date { get; set; }
        public int AccountID { get; set; }
        public decimal Amount { get; set; }
        public int CategoryID { get; set; }
        public int SubcategoryID { get; set; }
        public string Payee { get; set; }
        public int? TransferID { get; set; }

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
}
