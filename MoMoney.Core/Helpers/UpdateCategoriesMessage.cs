using CommunityToolkit.Mvvm.Messaging.Messages;

namespace MoMoney.Core.Helpers;

public class UpdateCategoriesMessage : ValueChangedMessage<string>
{
    public UpdateCategoriesMessage() : base(string.Empty) { }
}