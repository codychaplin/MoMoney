﻿using System.Globalization;

namespace MoMoney
{
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

        // if false, sensitive values are hidden
        public static bool ShowValue = true;

        // account type 
        public enum AccountTypes
        {
            Checkings,
            Savings,
            Credit,
            Investments
        }
    }
}