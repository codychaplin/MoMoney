using System.Text;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Exceptions;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels.Settings;

public partial class LoggingViewModel : ObservableObject
{
    readonly ILoggerService<LoggingViewModel> logger;

    [ObservableProperty] bool isExpanded = false;
    [ObservableProperty] bool isChecked = true;
    [ObservableProperty] ObservableCollection<Log> loadedLogs = [];

    [ObservableProperty] ObservableCollection<LogLevel> levels = [];
    [ObservableProperty] ObservableCollection<string> classes = [];
    [ObservableProperty] ObservableCollection<string> exceptions = [];
    LogLevel Level = LogLevel.None;
    string ClassName = string.Empty;
    string ExceptionType = string.Empty;

    List<Log> Logs = [];

    int totalItems = 0;

    public LoggingViewModel(ILoggerService<LoggingViewModel> _logger)
    {
        logger = _logger;
    }

    /// <summary>
    /// Gets logs from db.
    /// </summary>
    public async Task Init()
    {
        Shell.Current.IsBusy = true;

        var logs = await logger.GetLogs();
        Logs.Clear();
        Logs = new(logs);
        totalItems = Logs.Count;
        await LoadMoreItems();

        // populate filters
        var levels = logs.Select(l => l.Level).Distinct();
        var classes = logs.Select(l => l.ClassName).Distinct();
        var exceptions = logs.Select(l => l.ExceptionType == "" ? "None" : l.ExceptionType).Distinct();
        Levels = new(levels);
        Classes = new(classes);
        Exceptions = new(exceptions);

        await Task.Delay(100);
        Shell.Current.IsBusy = false;
    }

    [RelayCommand]
    public void ToggleExpand()
    {
        IsExpanded = !IsExpanded;
    }

    /// <summary>
    /// Updates the DataTemplateSelector
    /// </summary>
    [RelayCommand]
    async Task CheckChanged()
    {
        await UpdateFilter();
    }

    /// <summary>
    /// Updates Logs Filters.
    /// </summary>
    async Task UpdateFilter()
    {
        Logs = await logger.GetFilteredLogs(Level, ClassName, ExceptionType);
        LoadedLogs.Clear();
        await LoadMoreItems();
    }


    /// <summary>
    /// Loads items from Transactions.
    /// </summary>
    [RelayCommand]
    async Task LoadMoreItems()
    {
        await Task.Delay(50);
        int index = LoadedLogs.Count;
        int count = index + Constants.LOAD_COUNT >= totalItems ? totalItems - index : Constants.LOAD_COUNT;
        var logs = Logs.Skip(index).Take(count);
        foreach (var log in logs)
            LoadedLogs.Add(log);
    }

    /// <summary>
    /// Updates selected Level and filter.
    /// </summary>
    [RelayCommand]
    async Task UpdateLevel(LogLevel level)
    {
        Level = level;
        await UpdateFilter();
    }

    /// <summary>
    /// Updates selected Class and filter.
    /// </summary>
    [RelayCommand]
    async Task UpdateClass(string className)
    {
        ClassName = className;
        await UpdateFilter();
    }

    /// <summary>
    /// Updates selected Exception and filter.
    /// </summary>
    [RelayCommand]
    async Task UpdateException(string exceptionType)
    {
        ExceptionType = exceptionType;
        await UpdateFilter();
    }

    [RelayCommand]
    async Task OpenPopup(int ID)
    {
        try
        {
            Log log = await logger.GetLog(ID);
            StringBuilder sb = new();
            sb.Append($"Date: {log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")}\n\n");
            sb.Append($"Level: {log.Level}\n\n");
            sb.Append($"Class: {log.ClassName}\n\n");
            if (log.ExceptionType != "")
                sb.Append($"Exception: {log.ExceptionType}\n\n");
            sb.Append($"Message: {log.Message}");
            await Shell.Current.DisplayAlert("Details", sb.ToString(), "OK");
        }
        catch (LogNotFoundException ex)
        {
            await logger.LogError(nameof(OpenPopup), ex);
            await Shell.Current.DisplayAlert("Log Error", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(OpenPopup), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }
}