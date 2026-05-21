using System.Globalization;
using CsvHelper.Configuration;

namespace MoMoney.Core.Models.Statements;

public class Tangerine : IStatement
{
    public string Date { get; set; } = string.Empty;
    public string Transaction { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Memo { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string FullText => $"""
    Date: {Date}
    Transaction: {Transaction}
    Name: {Name}
    Memo: {Memo}
    Amount: {Amount}
    """;

    DateTime IStatement.Date => DateTime.Parse(Date, CultureInfo.InvariantCulture);
    string IStatement.SearchText => $"{Name} {Memo}".ToLower();
}

public class TangerineClassMap : ClassMap<Tangerine>
{
    public TangerineClassMap()
    {
        Map(m => m.Date).Index(0).Name("Date", "Transaction date");
        Map(m => m.Transaction).Index(1);
        Map(m => m.Name).Index(2);
        Map(m => m.Memo).Index(3);
        Map(m => m.Amount).Index(4);
    }
}