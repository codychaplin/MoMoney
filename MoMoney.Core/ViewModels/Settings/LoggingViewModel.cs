using System.Text;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Syncfusion.Maui.ListView;
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

    [ObservableProperty] ObservableCollection<LogLevel> levels;
    [ObservableProperty] ObservableCollection<string> classes;
    [ObservableProperty] ObservableCollection<string> exceptions;
    LogLevel Level = LogLevel.None;
    string ClassName;
    string ExceptionType;

    List<Log> Logs = [];

    public SfListView listview;

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

        listview.LoadMoreOption = LoadMoreOption.Auto;
        var logs = await logger.GetLogs();
        Logs.Clear();
        Logs = new(logs);
        totalItems = Logs.Count;

        // populate filters
        var levels = logs.Select(l => l.Level).Distinct();
        var classes = logs.Select(l => l.ClassName).Distinct();
        var exceptions = logs.Select(l => l.ExceptionType == "" ? "None" : l.ExceptionType).Distinct();
        Levels = new(levels);
        Classes = new(classes);
        Exceptions = new(exceptions);

        await Task.Delay(200);
        Shell.Current.IsBusy = false;
    }

    [RelayCommand]
    public void ToggleExpand()
    {
        IsExpanded = !IsExpanded;
    }

    /// <summary>
    /// Loads items from Transactions.
    /// </summary>
    [RelayCommand]
    async Task LoadMoreItems()
    {
        listview.IsLazyLoading = true;
        await Task.Delay(250);
        int index = LoadedLogs.Count;
        int count = index + Constants.LOAD_COUNT >= totalItems ? totalItems - index : Constants.LOAD_COUNT;
        AddLogs(index, count);
        listview.IsLazyLoading = false;

        // disables loading indicator
        if (totalItems > 0 && count == 0)
            listview.LoadMoreOption = LoadMoreOption.None;
    }

    /// <summary>
    /// Copies logs from Logs to LoadedLogs.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="count"></param>
    void AddLogs(int index, int count)
    {
        for (int i = index; i < index + count && i < totalItems; i++)
            LoadedLogs.Add(Logs[i]);
    }

    /// <summary>
    /// Updates the DataTemplateSelector
    /// </summary>
    [RelayCommand]
    void CheckChanged()
    {
        listview?.RefreshView();
    }

    /// <summary>
    /// Updates Logs Filter.
    /// </summary>
    void UpdateFilter()
    {
        if (listview.DataSource != null)
        {
            listview.LoadMoreOption = LoadMoreOption.Auto;
            listview.DataSource.Filter = FilterLogs;
            listview.DataSource.RefreshFilter();
        }
    }

    /// <summary>
    /// Checks if log matches filters.
    /// </summary>
    /// <param name="obj"></param>
    bool FilterLogs(object obj)
    {
        // if all are blank, show Log
        if (Level == LogLevel.None && ClassName == null && ExceptionType == null)
            return true;

        var log = obj as Log;
        string ex = ExceptionType == "None" ? "" : ExceptionType;

        // if fields aren't blank and match values, show log
        if (Level != LogLevel.None && log.Level != Level)
            return false;
        if (ClassName != null && log.ClassName != ClassName)
            return false;
        if (ex != null && log.ExceptionType != ex)
            return false;

        return true;
    }

    /// <summary>
    /// Updates selected Level and filter.
    /// </summary>
    [RelayCommand]
    void UpdateLevel(LogLevel level)
    {
        Level = level;
        UpdateFilter();
    }

    /// <summary>
    /// Updates selected Class and filter.
    /// </summary>
    [RelayCommand]
    void UpdateClass(string className)
    {
        ClassName = className;
        UpdateFilter();
    }

    /// <summary>
    /// Updates selected Exception and filter.
    /// </summary>
    [RelayCommand]
    void UpdateException(string exceptionType)
    {
        ExceptionType = exceptionType;
        UpdateFilter();
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