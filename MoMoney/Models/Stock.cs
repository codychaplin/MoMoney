using SQLite;

namespace MoMoney.Models
{
    public class Stock
    {
        [PrimaryKey]
        public string Symbol { get; set; }
        public int Quantity { get; set; }
        public decimal Cost { get; set; }
        public decimal MarketPrice { get; set; }
        public decimal BookValue { get; set; }
    }
}
