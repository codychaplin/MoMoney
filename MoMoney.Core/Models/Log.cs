using Microsoft.Extensions.Logging;
using SQLite;
using CsvHelper.Configuration.Attributes;

namespace MoMoney.Core.Models;

public class Log
{
    [PrimaryKey, AutoIncrement, CsvHelper.Configuration.Attributes.Ignore]
    public int LogId { get; set; }
    [Index(0)]
    public DateTime Timestamp { get; set; }
    [Index(1)]
    public LogLevel Level { get; set; } = LogLevel.Information;
    [Index(4)]
    public string Message { get; set; } = string.Empty;
    [Index(2)]
    public string ClassName { get; set; } = string.Empty;
    [Index(3)]
    public string ExceptionType { get; set; } = string.Empty;

    public Log() { }

    public Log(LogLevel level, string className, string message, string exceptionType)
    {
        Timestamp = DateTime.Now;
        Level = level;
        ClassName = className;
        Message = message;
        ExceptionType = exceptionType;
    }
}