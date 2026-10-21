using System.Globalization;
using CsvHelper.Configuration;

namespace MoMoney.Core.Models.Statements;

public class Wealthsimple : IStatement
{
    public string Transaction_date { get; set; } = string.Empty;
    public string Settlement_date { get; set; } = string.Empty;
    public string Account_id { get; set; } = string.Empty;
    public string Account_type { get; set; } = string.Empty;
    public string Activity_type { get; set; } = string.Empty;
    public string Activity_sub_type { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal? Unit_price { get; set; }
    public decimal? Commission { get; set; }
    public decimal Net_cash_amount { get; set; }
    public string FullText => $"""
    transaction_date: {Transaction_date}
    settlement_date: {Settlement_date}
    account_id: {Account_id}
    account_type: {Account_type}
    activity_type: {Activity_type}
    activity_sub_type: {Activity_sub_type}
    direction: {Direction}
    symbol: {Symbol}
    name: {Name}
    currency: {Currency}
    quantity: {Quantity}
    unit_price: {Unit_price}
    commission: {Commission}
    net_cash_amount: {Net_cash_amount}
    """;

    DateTime IStatement.Date => DateTime.Parse(Transaction_date, CultureInfo.InvariantCulture);
    decimal IStatement.Amount => Net_cash_amount;
    string IStatement.SearchText => $"{Activity_type} {Activity_sub_type} {Symbol}".ToLower();
}

public class WealthsimpleClassMap : ClassMap<Wealthsimple>
{
    public WealthsimpleClassMap()
    {
        Map(m => m.Transaction_date).Name("transaction_date");
        Map(m => m.Settlement_date).Name("settlement_date");
        Map(m => m.Account_id).Name("account_id");
        Map(m => m.Account_type).Name("account_type");
        Map(m => m.Activity_type).Name("activity_type");
        Map(m => m.Activity_sub_type).Name("activity_sub_type");
        Map(m => m.Direction).Name("direction");
        Map(m => m.Symbol).Name("symbol");
        Map(m => m.Name).Name("name");
        Map(m => m.Currency).Name("currency");
        Map(m => m.Quantity).Name("quantity");
        Map(m => m.Unit_price).Name("unit_price");
        Map(m => m.Commission).Name("commission");
        Map(m => m.Net_cash_amount).Name("net_cash_amount");
    }
}