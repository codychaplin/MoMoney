using System.Globalization;

namespace MoMoney.Core.Helpers;

public static class Constants
{
    // db constants
    public const string dbName = "MoMoney.db";
    public const SQLite.SQLiteOpenFlags Flags =
    SQLite.SQLiteOpenFlags.ReadWrite |
    SQLite.SQLiteOpenFlags.Create |
    SQLite.SQLiteOpenFlags.SharedCache;
    public static string DatabasePath => Path.Combine(FileSystem.AppDataDirectory, dbName);

    // category IDs
    public const int INCOME_ID = 1;
    public const int TRANSFER_ID = 2;
    public const int DEBIT_ID = 3;
    public const int CREDIT_ID = 4;
    public const int EXPENSE_ID = 5; // anything >= 5 will be an expense

    // month name array alias
    public static readonly string[] MONTHS = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthNames;

    // load count for infinite scroll
    public const int LOAD_COUNT = 50;

    // OpenAI
    public const string AUDIO_FILE_NAME = "recording.ogg";
    public const string AUDIO_MODEL = "whisper-1";
    public const decimal WHISPER_COST = 0.006m; // per minute
    public const string CHAT_MODEL = "gpt-3.5-turbo-0125";
    public const decimal CHAT_INPUT_COST = 0.0005m; // per 1000 tokens
    public const decimal CHAT_OUTPUT_COST = 0.0015m; // per 1000 tokens
    public const int MAX_TOKENS = 200;
}