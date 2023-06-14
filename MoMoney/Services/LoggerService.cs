﻿using Microsoft.Extensions.Logging;
using MoMoney.Data;
using MoMoney.Models;

namespace MoMoney.Services;

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

    public async Task<IEnumerable<Log>> GetLogs()
    {
        await momoney.Init();
        return await momoney.db.Table<Log>().ToListAsync();
    }
}