using CommunityToolkit.Mvvm.Messaging.Messages;

namespace MoMoney.Core.Helpers;

public class RefreshTransactionsMessage : ValueChangedMessage<TransactionEventArgs>
{
    public RefreshTransactionsMessage(TransactionEventArgs args) : base(args) { }
}