namespace MoMoney.Core.Exceptions;

// --------------------------------------
// -------- NOT FOUND EXCEPTIONS --------
// --------------------------------------

public class AccountNotFoundException : Exception
{
    public AccountNotFoundException() { }

    public AccountNotFoundException(string message) : base(message) { }

    public AccountNotFoundException(string message, Exception inner) : base(message, inner) { }
}

public class CategoryNotFoundException : Exception
{
    public CategoryNotFoundException() { }

    public CategoryNotFoundException(string message) : base(message) { }

    public CategoryNotFoundException(string message, Exception inner) : base(message, inner) { }
}

public class StockNotFoundException : Exception
{
    public StockNotFoundException() { }

    public StockNotFoundException(string message) : base(message) { }

    public StockNotFoundException(string message, Exception inner) : base(message, inner) { }
}

public class TransactionNotFoundException : Exception
{
    public TransactionNotFoundException() { }

    public TransactionNotFoundException(string message) : base(message) { }

    public TransactionNotFoundException(string message, Exception inner) : base(message, inner) { }
}


public class LogNotFoundException : Exception
{
    public LogNotFoundException() { }

    public LogNotFoundException(string message) : base(message) { }

    public LogNotFoundException(string message, Exception inner) : base(message, inner) { }
}

// --------------------------------------
// -------- DUPLICATE EXCEPTIONS --------
// --------------------------------------

public class DuplicateAccountException : Exception
{
    public DuplicateAccountException() { }

    public DuplicateAccountException(string message) : base(message) { }

    public DuplicateAccountException(string message, Exception inner) : base(message, inner) { }
}

public class DuplicateCategoryException : Exception
{
    public DuplicateCategoryException() { }

    public DuplicateCategoryException(string message) : base(message) { }

    public DuplicateCategoryException(string message, Exception inner) : base(message, inner) { }
}

public class DuplicateStockException : Exception
{
    public DuplicateStockException() { }

    public DuplicateStockException(string message) : base(message) { }

    public DuplicateStockException(string message, Exception inner) : base(message, inner) { }
}

// ------------------------------------
// -------- INVALID EXCEPTIONS --------
// ------------------------------------

public class InvalidAccountException : Exception
{
    public InvalidAccountException() { }

    public InvalidAccountException(string message) : base(message) { }

    public InvalidAccountException(string message, Exception inner) : base(message, inner) { }
}

public class InvalidCategoryException : Exception
{
    public InvalidCategoryException() { }

    public InvalidCategoryException(string message) : base(message) { }

    public InvalidCategoryException(string message, Exception inner) : base(message, inner) { }
}

public class InvalidStockException : Exception
{
    public InvalidStockException() { }

    public InvalidStockException(string message) : base(message) { }

    public InvalidStockException(string message, Exception inner) : base(message, inner) { }
}

public class InvalidTransactionException : Exception
{
    public InvalidTransactionException() { }

    public InvalidTransactionException(string message) : base(message) { }

    public InvalidTransactionException(string message, Exception inner) : base(message, inner) { }
}