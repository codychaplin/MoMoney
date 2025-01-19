using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace MoMoney.Core.Models;

public partial class Stock : ObservableObject
{
    [PrimaryKey, AutoIncrement, CsvHelper.Configuration.Attributes.Ignore]
    public int StockID { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Market { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Cost { get; set; }

    [ObservableProperty, CsvHelper.Configuration.Attributes.Ignore]
    public decimal marketPrice;

    [CsvHelper.Configuration.Attributes.Ignore]
    public string FullName => $"{Symbol}:{Market}";
    [CsvHelper.Configuration.Attributes.Ignore]
    public decimal MarketValue => MarketPrice * Quantity;
    [CsvHelper.Configuration.Attributes.Ignore]
    public decimal BookValue => Quantity * Cost;
    [CsvHelper.Configuration.Attributes.Ignore]
    public decimal Change => MarketValue - BookValue;
    [CsvHelper.Configuration.Attributes.Ignore]
    public decimal ChangePercent => (MarketValue / BookValue) - 1;

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
        StockID = stock.StockID;
        Symbol = stock.Symbol;
        Market = stock.Market;
        Quantity = stock.Quantity;
        Cost = stock.Cost;
        MarketPrice = stock.MarketPrice;
    }
}