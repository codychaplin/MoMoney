using MoMoney.Core.Models;

namespace MoMoney.Core.Helpers;

public class TransactionEventArgs : EventArgs
{
    public Transaction? Transaction { get; set; }

    public CRUD Type { get; }

    public enum CRUD
    {
        Create,
        Read,
        Update,
        Delete
    }

    public TransactionEventArgs(Transaction? transaction, CRUD type)
    {
        Transaction = transaction;
        Type = type;
    }
}