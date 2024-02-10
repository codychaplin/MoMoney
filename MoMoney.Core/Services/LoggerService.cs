﻿using Microsoft.Extensions.Logging;
using MoMoney.Core.Data;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Exceptions;
using MoMoney.Core.Services.Interfaces;
#if ANDROID
using Android.OS;
using Firebase.Analytics;
#endif

namespace MoMoney.Core.Services;

/// <inheritdoc />
public class LoggerService<T> : ILoggerService<T>, IFirebaseService
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
        try
        {
            await momoney.Init();
            Log log = new(level, className, message, exceptionType);
            await momoney.db.InsertAsync(log);
        }
        catch (Exception) { }
    }

    public async Task LogInfo(string message, string exceptionType = "")
    {
        await Log(LogLevel.Information, message, exceptionType);
    }

    public async Task LogWarning(string functionName, Exception ex)
    {
        await Log(LogLevel.Warning, ex.Message, ex.GetType().Name);
        LogFirebaseEvent(FirebaseParameters.EVENT_WARNING_LOG, FirebaseParameters.GetFirebaseParameters(ex, functionName, className));
    }

    public async Task LogError(string functionName, Exception ex)
    {
        // if SQLite exception, log as critical, otherwise log as error
        if (ex is SQLite.SQLiteException)
        {
            await Log(LogLevel.Critical, ex.Message, ex.GetType().Name);
            LogFirebaseEvent(FirebaseParameters.EVENT_CRITICAL_LOG, FirebaseParameters.GetFirebaseParameters(ex, functionName, className));
        }
        else
        {
            await Log(LogLevel.Error, ex.Message, ex.GetType().Name);
            LogFirebaseEvent(FirebaseParameters.EVENT_ERROR_LOG, FirebaseParameters.GetFirebaseParameters(ex, functionName, className));
        }
    }

    public async Task LogCritical(string functionName, Exception ex)
    {
        await Log(LogLevel.Critical, ex.Message, ex.GetType().Name);
        LogFirebaseEvent(FirebaseParameters.EVENT_CRITICAL_LOG, FirebaseParameters.GetFirebaseParameters(ex, functionName, className));
    }

    public void LogFirebaseEvent(string eventName, IDictionary<string, string> parameters)
    {
#if ANDROID
        var firebaseAnalytics = FirebaseAnalytics.GetInstance(Platform.CurrentActivity);

        if (parameters == null)
        {
            firebaseAnalytics.LogEvent(eventName, null);
            return;
        }

        var bundle = new Bundle();
        foreach (var param in parameters)
        {
            bundle.PutString(param.Key, param.Value);
        }

        firebaseAnalytics.LogEvent(eventName, bundle);
#endif
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