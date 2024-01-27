using CommunityToolkit.Mvvm.Messaging.Messages;

namespace MoMoney.Core.Helpers;

public class UpdateStocksMessage : ValueChangedMessage<string>
{
    public UpdateStocksMessage() : base(string.Empty) { }
}