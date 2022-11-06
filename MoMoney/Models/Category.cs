using SQLite;

namespace MoMoney.Models
{
    public class Category
    {
        [PrimaryKey, AutoIncrement]
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public string ParentName { get; set; }
        public bool Enabled { get; set; }
    }
}
