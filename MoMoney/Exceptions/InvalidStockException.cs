
namespace MoMoney.Exceptions
{
    public class InvalidStockException : Exception
    {
        public InvalidStockException() { }

        public InvalidStockException(string message) : base(message) { }

        public InvalidStockException(string message, Exception inner) : base(message, inner) { }
    }
}
