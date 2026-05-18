using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace MoMoney.Core.Models.Statements;

public enum BankType
{
    Tangerine,
    Wealthsimple
}

public partial class MappingRule : ObservableObject
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public BankType BankType { get; set; }
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string Pattern { get; set; } = string.Empty;
    public string MappingJson { get; set; } = string.Empty;

    public MappingRule() {}
    public MappingRule(string name, BankType bankType)
    {
        Name = name;
        BankType = bankType;
    }
}