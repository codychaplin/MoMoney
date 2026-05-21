using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MoMoney.Core.Models.Statements;

public enum ImportStatus
{
    Verified,
    [Description("Manually Approved")]
    ManuallyApproved,
    [Description("Needs Verification")]
    NeedsVerification,
    [Description("Auto Skipped")]
    AutoSkipped,
    [Description("Manually Skipped")]
    ManuallySkipped
}

public partial class RecordTransactionPair : ObservableObject
{
    public IStatement RawData { get; set; }
    public Transaction Transaction { get; set; }
    [ObservableProperty]
    ImportStatus status = ImportStatus.NeedsVerification;
    [ObservableProperty]
    bool canEdit = false;
    [ObservableProperty]
    bool isTransfer = false;

    public RecordTransactionPair(IStatement rawData) : this(rawData, new Transaction()) {}

    public RecordTransactionPair(IStatement rawData, Transaction transaction)
    {
        RawData = rawData;
        Transaction = transaction;
        Status = ImportStatus.NeedsVerification;
    }
}
