using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace MoMoney.Core.Models;

public partial class Stock : ObservableObject
{
    [PrimaryKey, AutoIncrement]
    public int StockID { get; set; }
    public string Symbol { get; set; }
    public string Market { get; set; }
    public decimal Quantity { get; set; }
    public decimal Cost { get; set; }

    [ObservableProperty]
    public decimal marketPrice;

    public Stock() { }

    public Stock(string symbol, string market, decimal quantity, decimal cost, decimal marketPrice)
    {
        Symbol = symbol;
        Market = market;
        Quantity = quantity;
        Cost = cost;
        MarketPrice = marketPrice;
    }

    public Stock (Stock stock)
    {
        Symbol = stock.Symbol;
        Market = stock.Market;
        Quantity = stock.Quantity;
        Cost = stock.Cost;
        MarketPrice = stock.MarketPrice;
    }
}