
namespace MoMoney.Core.Models;

public partial class DetailedStock : Stock
{
    public string FullName => $"{Symbol}:{Market}";
    public decimal MarketValue => MarketPrice * Quantity;
    public decimal BookValue => Quantity * Cost;
    public decimal Change => MarketValue - BookValue;
    public decimal ChangePercent => (MarketValue / BookValue) - 1;

    public DetailedStock(Stock stock)
    {
        StockID = stock.StockID;
        Symbol = stock.Symbol;
        Market = stock.Market;
        Quantity = stock.Quantity;
        Cost = stock.Cost;
        MarketPrice = stock.MarketPrice;
    }
}
