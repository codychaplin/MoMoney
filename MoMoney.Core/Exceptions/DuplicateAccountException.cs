
namespace MoMoney.Core.Exceptions;

public class DuplicateAccountException : Exception
{
    public DuplicateAccountException() { }

    public DuplicateAccountException(string message) : base(message) { }
    
    public DuplicateAccountException(string message, Exception inner) : base(message, inner) { }
}
