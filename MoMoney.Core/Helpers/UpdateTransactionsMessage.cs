using CommunityToolkit.Mvvm.Messaging.Messages;

namespace MoMoney.Core.Helpers;

public class UpdateTransactionsMessage : ValueChangedMessage<TransactionEventArgs>
{
    public UpdateTransactionsMessage() : base(default) { }
    public UpdateTransactionsMessage(TransactionEventArgs args) : base(args) { }
}