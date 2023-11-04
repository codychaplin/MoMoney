using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Syncfusion.Maui.ListView;
using MoMoney.Models;
using MoMoney.Helpers;
using MoMoney.Services;
using System.Text;
using MoMoney.Exceptions;

namespace MoMoney.ViewModels.Settings;

public partial class LoggingViewModel : ObservableObject
{
    readonly ILoggerService<LoggingViewModel> logger;

    [ObservableProperty]
    public bool isExpanded = false;

    [ObservableProperty]
    public ObservableCollection<Log> loadedLogs = new();

    [ObservableProperty]
    public ObservableCollection<LogLevel> levels;

    [ObservableProperty]
    public ObservableCollection<string> classes;

    [ObservableProperty]
    public ObservableCollection<string> exceptions;

    [ObservableProperty]
    public LogLevel level = LogLevel.None;

#nullable enable
    [ObservableProperty]
    public string? className;

    [ObservableProperty]
    public string? exceptionType;
#nullable disable

    List<Log> Logs = new();

    public SfListView listview;

    int totalItems = 0;

    public LoggingViewModel(ILoggerService<LoggingViewModel> _logger)
    {
        logger = _logger;
    }

    /// <summary>
    /// Gets logs from db.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public async void Init(object sender, EventArgs e)
    {
        listview.LoadMoreOption = LoadMoreOption.Auto;
        var logs = await logger.GetLogs();
        Logs.Clear();
        Logs = new(logs);
        totalItems = Logs.Count;

        // populate filters
        var levels = logs.Select(l => l.Level).Distinct().ToList();
        var classes = logs.Select(l => l.ClassName).Distinct().ToList();
        var exceptions = logs.Select(l => l.ExceptionType == "" ? "None" : l.ExceptionType).Distinct().ToList();
        Levels = new(levels);
        Classes = new(classes);
        Exceptions = new(exceptions);
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
    /// Updates Logs Filter.
    /// </summary>
    public void UpdateFilter(object sender, EventArgs e)
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
#nullable enable
        string? ex = ExceptionType == "None" ? "" : ExceptionType;
#nullable disable

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
    /// Clears selected Level.
    /// </summary>
    [RelayCommand]
    void ClearLevel()
    {
        Level = LogLevel.None;
        UpdateFilter(this, default);
    }

    /// <summary>
    /// Clears selected Class.
    /// </summary>
    [RelayCommand]
    void ClearClass()
    {
        ClassName = null;
        UpdateFilter(this, default);
    }

    /// <summary>
    /// Clears selected Exception.
    /// </summary>
    [RelayCommand]
    void ClearException()
    {
        ExceptionType = null;
        UpdateFilter(this, default);
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
            string exception = log.ExceptionType == "" ? "N/A" : log.ExceptionType;
            sb.Append($"Exception: {exception}\n\n");
            sb.Append($"Message: {log.Message}");
            await Shell.Current.DisplayAlert("Details", sb.ToString(), "OK");
        }
        catch (LogNotFoundException ex)
        {
            await logger.LogError(ex.Message, ex.GetType().Name);
            await Shell.Current.DisplayAlert("Log Error", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await logger.LogError(ex.Message, ex.GetType().Name);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }
}