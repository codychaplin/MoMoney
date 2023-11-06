using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace MoMoney.Core.Models;

public partial class Stock : ObservableObject
{
    [PrimaryKey]
    public string Symbol { get; set; }
    public int Quantity { get; set; }
    public decimal Cost { get; set; }

    [ObservableProperty]
    public decimal marketPrice;
    public decimal BookValue { get; set; }

    public Stock() { }

    public Stock(string symbol, int quantity, decimal cost, decimal marketPrice, decimal bookValue)
    {
        Symbol = symbol;
        Quantity = quantity;
        Cost = cost;
        MarketPrice = marketPrice;
        BookValue = bookValue;
    }

    public Stock (Stock stock)
    {
        Symbol = stock.Symbol;
        Quantity = stock.Quantity;
        Cost = stock.Cost;
        MarketPrice = stock.MarketPrice;
        BookValue = stock.BookValue;
    }
}
