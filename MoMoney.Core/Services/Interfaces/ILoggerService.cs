using Microsoft.Extensions.Logging;
using MoMoney.Core.Models;
using MoMoney.Core.Exceptions;

namespace MoMoney.Core.Services.Interfaces;
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
    /// <param name="functionName"></param>
    /// <param name="ex"></param>
    Task LogWarning(string functionName, Exception ex);

    /// <summary>
    /// Sends error level log to db.
    /// </summary>
    /// <param name="functionName"></param>
    /// <param name="ex"></param>
    Task LogError(string functionName, Exception ex);

    /// <summary>
    /// Sends critical level log to db.
    /// </summary>
    /// <param name="functionName"></param>
    /// <param name="ex"></param>
    Task LogCritical(string functionName, Exception ex);

    /// <summary>
    /// Sends log to Firebase.
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="parameters"></param>
    void LogFirebaseEvent(string eventName, IDictionary<string, string> parameters);

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

    /// <summary>
    /// Gets total number of Logs in db.
    /// </summary>
    /// <returns>Integer representing number of Logs</returns>
    Task<int> GetLogCount();
}