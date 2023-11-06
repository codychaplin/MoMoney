using CommunityToolkit.Mvvm.Messaging.Messages;

namespace MoMoney.Core.Helpers;

public class UpdateAccountsMessage : ValueChangedMessage<string>
{
    public UpdateAccountsMessage() : base(string.Empty) { }
}