using Microsoft.Extensions.Logging;
using MoMoney.Models;

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
    /// Gets all Logs from Logs table as a list.
    /// </summary>
    /// <returns>List of Log objects</returns>
    Task<IEnumerable<Log>> GetLogs();
}