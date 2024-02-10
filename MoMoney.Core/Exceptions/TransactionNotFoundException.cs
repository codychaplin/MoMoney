
namespace MoMoney.Core.Exceptions;

public class TransactionNotFoundException : Exception
{
    public TransactionNotFoundException() { }

    public TransactionNotFoundException(string message) : base(message) { }

    public TransactionNotFoundException(string message, Exception inner) : base(message, inner) { }
}
