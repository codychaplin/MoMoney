using Microsoft.Extensions.Logging;
using SQLite;
using CsvHelper.Configuration.Attributes;

namespace MoMoney.Models;

public class Log
{
    [PrimaryKey, AutoIncrement]
    public int LogId { get; set; }
    [Index(0)]
    public DateTime Timestamp { get; set; }
    [Index(1)]
    public LogLevel Level { get; set; } = LogLevel.Information;
    [Index(4)]
    public string Message { get; set; } = "";
    [Index(2)]
    public string ClassName { get; set; } = "";
    [Index(3)]
    public string ExceptionType { get; set; } = "";

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