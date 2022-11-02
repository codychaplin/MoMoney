using SQLite;

namespace MoMoney.Models
{
    public class Account
    {
        [PrimaryKey, AutoIncrement]
        int AccountID { get; set; }
        string AccountName { get; set; }
        int AccountTypeID { get; set; }
        public decimal StartingBalance { get; set; }
        public decimal CurrentBalance { get; set; }
        public bool Enabled { get; set; }

        /// <summary>
        /// Initializes a new instance of the Account class.
        /// </summary>
        public Account() { }

        /// <summary>
        /// Initializes a new instance of the Account class only setting the ID.
        /// </summary>
        /// <param name="ID">Account ID</param>
        public Account(int ID)
        {
            AccountID = ID;
        }

        /// <summary>
        /// Initializes a new instance of the Account class setting all parameters
        /// </summary>
        /// <param name="ID">Account ID</param>
        /// <param name="accountName">Account name</param>
        /// <param name="accountTypeID">Account type ID</param>
        /// <param name="startingBalance">Starting balance</param>
        /// <param name="currentBalance">Current balance</param>
        /// <param name="enabled">Enabled</param>
        public Account(int ID, string accountName, int accountTypeID,
            decimal startingBalance, decimal currentBalance, bool enabled) : this(ID)
        {
            AccountName = accountName;
            AccountTypeID = accountTypeID;
            StartingBalance = startingBalance;
            CurrentBalance = currentBalance;
            Enabled = enabled;
        }

        /// <summary>
        /// Initializes a new instance of the Account class using a copy constructor
        /// </summary>
        /// <param name="acc">Account object</param>
        public Account(Account acc) : this(acc.AccountID, acc.AccountName, acc.AccountTypeID,
            acc.StartingBalance, acc.CurrentBalance, acc.Enabled)
        {

        }

        /// <summary>
        /// Initializes a new instance of the Account class using a copy constructor and a new account ID
        /// </summary>
        /// <param name="ID">Account ID</param>
        /// <param name="acc">Account object</param>
        public Account(int ID, Account acc) : this(ID, acc.AccountName, acc.AccountTypeID,
            acc.StartingBalance, acc.CurrentBalance, acc.Enabled)
        {

        }

        /// <summary>
        /// Account .ToString() override used for debug purposes
        /// </summary>
        public override string ToString()
        {
            return $"{AccountID},{AccountName},{AccountTypeID},{StartingBalance},{CurrentBalance}";
        }
    }
}