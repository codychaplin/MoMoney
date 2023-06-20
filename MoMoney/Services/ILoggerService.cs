using Microsoft.Extensions.Logging;
using MoMoney.Models;
using MoMoney.Exceptions;

namespace MoMoney.Services;
public interface ILoggerService<T>
{
    /// <summary>
    /// Sends log to db.
    /// </summary>
    /// <param name="level"></param>
    /// <param name="message"></param>
    /// <param name="exceptionType"></param>
    Task Log(LogLevel level, string message, string exceptionType);

    /// <summary>
    /// Sends info level log to db.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="exceptionType"></param>
    Task LogInfo(string message, string exceptionType = "");

    /// <summary>
    /// Sends warning level log to db.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="exceptionType"></param>
    Task LogWarning(string message, string exceptionType = "");

    /// <summary>
    /// Sends error level log to db.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="exceptionType"></param>
    Task LogError(string message, string exceptionType = "");

    /// <summary>
    /// Sends critical level log to db.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="exceptionType"></param>
    Task LogCritical(string message, string exceptionType = "");

    /// <summary>
    /// Gets an log from the Logs table using an ID.
    /// </summary>
    /// <param name="ID"></param>
    /// <returns>Log object</returns>
    /// <exception cref="LogNotFoundException"></exception>
    Task<Log> GetLog(int ID);

    /// <summary>
    /// Gets all Logs from Logs table as a list.
    /// </summary>
    /// <returns>List of Log objects</returns>
    Task<IEnumerable<Log>> GetLogs();

    /// <summary>
    /// Removes ALL Logs from Logs table.
    /// </summary>
    Task RemoveLogs();
}