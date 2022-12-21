using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Models;
using MoMoney.Services;

namespace MoMoney.ViewModels
{
    public partial class StatsPageViewModel : ObservableObject
    {
        IEnumerable<Account> PreviousAccounts { get; set; }

        [ObservableProperty]
        public ObservableCollection<Account> checkingsAccounts = new();

        [ObservableProperty]
        public ObservableCollection<Account> savingsAccounts = new();

        [ObservableProperty]
        public ObservableCollection<Account> creditAccounts = new();

        [ObservableProperty]
        public ObservableCollection<Account> investmentAccounts = new();

        [ObservableProperty]
        public decimal checkingsSum = 0;

        [ObservableProperty]
        public decimal savingsSum = 0;

        [ObservableProperty]
        public decimal creditSum = 0;

        [ObservableProperty]
        public decimal investmentSum = 0;

        [ObservableProperty]
        public decimal networth = 0;

        /// <summary>
        /// Gets active accounts from database, groups them by account type,
        /// adds them to the corresponding collection, then adds balance to corresponding sum
        /// </summary>
        public async Task getAccounts()
        {
            var accounts = await AccountService.GetActiveAccounts();

            // checks if there are any accounts in db, then if the all the current balances are the same
            if (accounts.Any())
            {
                if (PreviousAccounts != null &&
                    PreviousAccounts.Select(a1 => a1.CurrentBalance)
                                    .All(a => accounts.Select(a2 => a2.CurrentBalance)
                                    .Contains(a)))
                    return;

                // cache accounts and clear fields
                PreviousAccounts = accounts;
                CheckingsAccounts.Clear();
                SavingsAccounts.Clear();
                CreditAccounts.Clear();
                InvestmentAccounts.Clear();
                CheckingsSum = 0;
                SavingsSum = 0;
                CreditSum = 0;
                InvestmentSum = 0;
                Networth = 0;

                // update account type values
                foreach (var acc in accounts)
                {
                    var type = Enum.Parse(typeof(Constants.AccountTypes), acc.AccountType);
                    switch (type)
                    {
                        case Constants.AccountTypes.Checkings:
                            CheckingsAccounts.Add(acc);
                            CheckingsSum += acc.CurrentBalance;
                            Networth += acc.CurrentBalance;
                            break;
                        case Constants.AccountTypes.Savings:
                            SavingsAccounts.Add(acc);
                            SavingsSum += acc.CurrentBalance;
                            Networth += acc.CurrentBalance;
                            break;
                        case Constants.AccountTypes.Credit:
                            CreditAccounts.Add(acc);
                            CreditSum += acc.CurrentBalance;
                            Networth += acc.CurrentBalance;
                            break;
                        case Constants.AccountTypes.Investments:
                            InvestmentAccounts.Add(acc);
                            InvestmentSum += acc.CurrentBalance;
                            Networth += acc.CurrentBalance;
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }
}
