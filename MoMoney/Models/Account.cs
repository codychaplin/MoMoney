using SQLite;

namespace MoMoney.Models
{
    public class Account
    {
        [PrimaryKey, AutoIncrement]
        public int AccountID { get; set; }
        public string AccountName { get; set; }
        public string AccountType { get; set; }
        public decimal StartingBalance { get; set; }
        public decimal CurrentBalance { get; set; }
        public bool Enabled { get; set; }
    }
}