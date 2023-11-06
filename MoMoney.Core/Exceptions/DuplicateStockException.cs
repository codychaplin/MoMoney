
namespace MoMoney.Core.Exceptions;

public class DuplicateStockException : Exception
{
    public DuplicateStockException() { }

    public DuplicateStockException(string message) : base(message) { }

    public DuplicateStockException(string message, Exception inner) : base(message, inner) { }
}
