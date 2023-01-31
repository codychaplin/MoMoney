namespace MoMoney
{
    public static class Constants
    {
        public const string dbName = "MoMoney.db";
        
        public const SQLite.SQLiteOpenFlags Flags =
        // open the database in read/write mode
        SQLite.SQLiteOpenFlags.ReadWrite |
        // create the database if it doesn't exist
        SQLite.SQLiteOpenFlags.Create |
        // enable multi-threaded database access
        SQLite.SQLiteOpenFlags.SharedCache;

        public static string DatabasePath => Path.Combine(FileSystem.AppDataDirectory, dbName);

        // category IDs
        public const int INCOME_ID = 1;
        public const int TRANSFER_ID = 2;
        public const int DEBIT_ID = 3;
        public const int CREDIT_ID = 4;
        public const int EXPENSE_ID = 5;

        public enum AccountTypes
        {
            Checkings,
            Savings,
            Credit,
            Investments
        }

    }
}