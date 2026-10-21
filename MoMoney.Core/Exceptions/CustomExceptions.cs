namespace MoMoney.Core.Exceptions;

public class NotFoundException(string message) : Exception(message) {}

public class DuplicateException(string message) : Exception(message) {}

public class InvalidException(string message) : Exception(message) {}