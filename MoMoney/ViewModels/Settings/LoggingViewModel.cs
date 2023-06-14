using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Models;
using MoMoney.Services;

namespace MoMoney.ViewModels.Settings;

public partial class LoggingViewModel : ObservableObject
{
    readonly ILoggerService<LoggingViewModel> logger;

    [ObservableProperty]
    public ObservableCollection<Log> logs = new();

    public LoggingViewModel(ILoggerService<LoggingViewModel> _logger)
    {
        logger = _logger;
    }

    public async void Init(object sender, EventArgs e)
    {
        var logs = await logger.GetLogs();
        Logs.Clear();
        Logs = new(logs);
    }
}