using System.Diagnostics;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using MoMoney.Core.Data;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.Services;

public class BaseService<TLogger, TMessenger, TType> where TMessenger : ValueChangedMessage<TType>, new()
{
    protected readonly MoMoneydb momoney;
    readonly ILoggerService<TLogger> logger;

    protected BaseService(MoMoneydb _momoney, ILoggerService<TLogger> _logger)
    {
        momoney = _momoney;
        logger = _logger;
    }

    protected virtual async Task Init()
    {
        await momoney.Init();
    }

    /// <summary>
    /// This function is a wrapper for all database operations. It lazy loads the database, logs the elapsed time, and sends a weak reference message so the UI can update if required.
    /// </summary>
    /// <param name="operation"></param>
    /// <param name="sendMessage"></param>
    /// <returns></returns>
    protected async Task DbOperation(Func<Task<string>> operation, bool sendMessage = true)
    {
        Stopwatch sw = Stopwatch.StartNew();
        await Init();

        string logMessage = await operation();

        sw.Stop();
        await logger.LogInfo($"[{sw.ElapsedMilliseconds}ms] {logMessage}");

        if (sendMessage)
            WeakReferenceMessenger.Default.Send<TMessenger>();
    }
}