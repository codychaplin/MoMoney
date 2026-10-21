using System.Globalization;

namespace MoMoney.Core.Helpers;

public static class Constants
{
    // db constants
    public const int dbVersion = 1;
    public const string dbName = "momoney.db";
    public const string DefaultDbPath = ":memory:";
    public static string DatabasePath => Path.Combine(FileSystem.AppDataDirectory, dbName);

    // secure storage keys
    public const string dbEncryptionKey = "encryption_key";

    // category IDs
    public const int TRANSFER_ID = 1;
    public const int DEBIT_ID = 2;
    public const int CREDIT_ID = 3;
    public const int INCOME_ID = 4;
    public const int EXPENSE_THRESHOLD = 5; // anything >= 5 will be an expense

    // month name array alias
    public static readonly string[] MONTHS = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthNames;

    // load count for infinite scroll
    public const int LOAD_COUNT = 100;

    // OpenAI
    public const string AUDIO_FILE_NAME = "recording.ogg";
    public const string AUDIO_MODEL = "openai/whisper-1";
    public const decimal WHISPER_COST = 0.006m; // per minute
    public const string CHAT_MODEL = "google/gemma-4-31b-it";
    public const decimal CHAT_INPUT_COST = 0.12m;// per 1M tokens
    public const decimal CHAT_OUTPUT_COST = 0.37m;// per 1M tokens
    public const int MAX_TOKENS = 500;
}