using SQLite;

namespace MoMoney
{
    public static class MoMoneydb
    {
        public static SQLiteAsyncConnection db { get; private set; }

        /// <summary>
        /// Creates new database connection, if none exist
        /// </summary>
        public static void Init()
        {
            if (db is not null)
                return;

            db = new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);
        }
    }
}
