using Microsoft.Extensions.Logging;
using MoMoney.Core.Data;
using MoMoney.Core.Models;
using MoMoney.Core.Exceptions;

namespace MoMoney.Core.Services;

/// <inheritdoc />
public class LoggerService<T> : ILoggerService<T>
{
    readonly MoMoneydb momoney;
    readonly string className;

    public LoggerService(MoMoneydb _momoney)
    {
        momoney = _momoney;
        className = typeof(T).Name;
    }

    public async Task Log(LogLevel level, string message, string exceptionType)
    {
        await momoney.Init();
        Log log = new(level, className, message, exceptionType);
        await momoney.db.InsertAsync(log);
    }

    public async Task LogInfo(string message, string exceptionType = "")
    {
        await Log(LogLevel.Information, message, exceptionType);
    }

    public async Task LogWarning(string message, string exceptionType = "")
    {
        await Log(LogLevel.Warning, message, exceptionType);
    }

    public async Task LogError(string message, string exceptionType = "")
    {
        await Log(LogLevel.Error, message, exceptionType);
    }

    public async Task LogCritical(string message, string exceptionType = "")
    {
        await Log(LogLevel.Critical, message, exceptionType);
    }

    public async Task<Log> GetLog(int ID)
    {
        await momoney.Init();

        var log = await momoney.db.Table<Log>().FirstOrDefaultAsync(l => l.LogId == ID);
        if (log is null)
            throw new LogNotFoundException($"Could not find Log with ID '{ID}'.");
        else
            return log;
    }

    public async Task<IEnumerable<Log>> GetLogs()
    {
        await momoney.Init();
        return await momoney.db.Table<Log>().OrderByDescending(l => l.Timestamp).ToListAsync();
    }

    public async Task RemoveLogs()
    {
        await momoney.Init();
        await momoney.db.DeleteAllAsync<Log>();
        await momoney.db.DropTableAsync<Log>();
        await momoney.db.CreateTableAsync<Log>();
    }

    public async Task<int> GetLogCount()
    {
        await momoney.Init();
        return await momoney.db.Table<Log>().CountAsync();
    }
}