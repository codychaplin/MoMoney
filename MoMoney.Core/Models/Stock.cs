using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace MoMoney.Core.Models;

public partial class Stock : ObservableObject
{
    [PrimaryKey]
    public string Symbol { get; set; }
    public decimal Quantity { get; set; }
    public decimal Cost { get; set; }

    [ObservableProperty]
    public decimal marketPrice;

    public Stock() { }

    public Stock(string symbol, decimal quantity, decimal cost, decimal marketPrice)
    {
        Symbol = symbol;
        Quantity = quantity;
        Cost = cost;
        MarketPrice = marketPrice;
    }

    public Stock (Stock stock)
    {
        Symbol = stock.Symbol;
        Quantity = stock.Quantity;
        Cost = stock.Cost;
        MarketPrice = stock.MarketPrice;
    }
}