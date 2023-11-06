
namespace MoMoney.Core.Models;

public class DetailedStock : Stock
{
    public decimal MarketValue => Symbol.EndsWith(":TSE") ? MarketPrice * Quantity : MarketPrice * Quantity * 1.35m;
    public decimal Change => MarketValue - BookValue;
    public decimal ChangePercent => (MarketValue / BookValue) - 1;

    public DetailedStock(Stock stock)
    {
        Symbol = stock.Symbol;
        Quantity = stock.Quantity;
        Cost = stock.Cost;
        MarketPrice = stock.MarketPrice;
        BookValue = stock.BookValue;
    }
}
