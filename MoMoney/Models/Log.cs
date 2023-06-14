using Microsoft.Extensions.Logging;
using SQLite;

namespace MoMoney.Models;

public class Log
{
    [PrimaryKey, AutoIncrement]
    public int LogId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = LogLevel.Information.ToString();
    public string Message { get; set; } = "";
    public string ClassName { get; set; } = "";
    public string ExceptionType { get; set; } = "";

    public Log() { }

    public Log(LogLevel level, string className, string message, string exceptionType)
    {
        Timestamp = DateTime.Now;
        Level = level.ToString();
        ClassName = className;
        Message = message;
        ExceptionType = exceptionType;
    }
}