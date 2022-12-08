
namespace MoMoney.Exceptions
{
    public class DuplicateCategoryException : Exception
    {
        public DuplicateCategoryException() { }

        public DuplicateCategoryException(string message) : base(message) { }
        
        public DuplicateCategoryException(string message, Exception inner) : base(message, inner) { }
    }
}
