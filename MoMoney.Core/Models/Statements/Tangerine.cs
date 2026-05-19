using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MoMoney.Core.Models.Statements;

public class Tangerine
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
}

public enum ImportStatus
{
    Verified,
    [Description("Manually Approved")]
    ManuallyApproved,
    [Description("Needs Verification")]
    NeedsVerification
}

public partial class TangerineTransactionPair : ObservableObject
{
    public Tangerine RawData { get; set; }
    public Transaction Transaction { get; set; }
    [ObservableProperty]
    ImportStatus status = ImportStatus.NeedsVerification;
    [ObservableProperty]
    bool canEdit = false;
    [ObservableProperty]
    bool isTransfer = false;

    public TangerineTransactionPair(Tangerine rawData) : this(rawData, new Transaction()) {}

    public TangerineTransactionPair(Tangerine rawData, Transaction transaction)
    {
        RawData = rawData;
        Transaction = transaction;
        Status = ImportStatus.NeedsVerification;
    }
}