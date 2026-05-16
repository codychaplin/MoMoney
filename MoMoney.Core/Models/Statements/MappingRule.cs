using SQLite;

namespace MoMoney.Core.Models.Statements;

public enum BankType
{
    Tangerine,
    Wealthsimple
}

public class MappingRule
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public BankType BankType { get; set; }
    public string Pattern { get; set; } = string.Empty;
    public string MappingJson { get; set; } = string.Empty;
}