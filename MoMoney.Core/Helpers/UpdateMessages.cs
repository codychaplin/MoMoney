﻿using CommunityToolkit.Mvvm.Messaging.Messages;
using MoMoney.Core.ViewModels.Stats;

namespace MoMoney.Core.Helpers;

public class UpdateAccountsMessage : ValueChangedMessage<string>
{
    public UpdateAccountsMessage() : base(string.Empty) { }
}

public class UpdateCategoriesMessage : ValueChangedMessage<string>
{
    public UpdateCategoriesMessage() : base(string.Empty) { }
}

public class UpdateStocksMessage : ValueChangedMessage<string>
{
    public UpdateStocksMessage() : base(string.Empty) { }
}

public class UpdateTransactionsMessage : ValueChangedMessage<TransactionEventArgs>
{
    public UpdateTransactionsMessage() : base(default) { }
    public UpdateTransactionsMessage(TransactionEventArgs args) : base(args) { }
}

public class ChangeTabMessage : ValueChangedMessage<int>
{
    public ChangeTabMessage(int index) : base(index) { }
}

public class UpdateHomePageMessage : ValueChangedMessage<string>
{
    public UpdateHomePageMessage() : base(string.Empty) { }
}