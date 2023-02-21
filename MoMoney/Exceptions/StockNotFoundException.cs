
namespace MoMoney.Exceptions;

public class StockNotFoundException : Exception
{
    public StockNotFoundException() { }

    public StockNotFoundException(string message) : base(message) { }

    public StockNotFoundException(string message, Exception inner) : base(message, inner) { }
}