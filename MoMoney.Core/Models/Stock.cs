using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace MoMoney.Core.Models;

public partial class Stock : ObservableObject
{
    [PrimaryKey, AutoIncrement, CsvHelper.Configuration.Attributes.Ignore]
    public int StockID { get; set; }
    [ObservableProperty]
    public partial string Symbol { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string? Market { get; set; } = null;
    [ObservableProperty]
    public partial decimal? Quantity { get; set; }
    [ObservableProperty]
    public partial decimal? Cost { get; set; }

    [ObservableProperty, CsvHelper.Configuration.Attributes.Ignore]
    public decimal? marketPrice;

    [CsvHelper.Configuration.Attributes.Ignore]
    public string FullName => $"{Symbol}:{Market}";
    [CsvHelper.Configuration.Attributes.Ignore]
    public decimal MarketValue => MarketPrice ?? 0 * Quantity ?? 0;
    [CsvHelper.Configuration.Attributes.Ignore]
    public decimal BookValue => Quantity ?? 0 * Cost ?? 0;
    [CsvHelper.Configuration.Attributes.Ignore]
    public decimal Change => MarketValue - BookValue;
    [CsvHelper.Configuration.Attributes.Ignore]
    public decimal ChangePercent => (MarketValue / BookValue) - 1;

    public Stock() { }

    public Stock(string symbol, string? market, decimal? quantity, decimal? cost, decimal? marketPrice)
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