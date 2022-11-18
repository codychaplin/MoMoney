using SQLite;

namespace MoMoney.Models
{
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
    }
}
