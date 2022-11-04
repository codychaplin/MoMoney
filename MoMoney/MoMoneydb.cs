using SQLite;

namespace MoMoney
{
    public static class MoMoneydb
    {
        public static SQLiteAsyncConnection db { get; private set; }

        /// <summary>
        /// Creates new database connection, if none exist
        /// </summary>
        /// <returns>true if not initialized, false if already initialized</returns>
        public static bool Init()
        {
            if (db is not null)
                return false;
            
            db = new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);
            return true;
        }
    }
}
