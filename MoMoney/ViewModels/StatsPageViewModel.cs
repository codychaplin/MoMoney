using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Models;
using MoMoney.Services;
using Syncfusion.Maui.Charts;

namespace MoMoney.ViewModels
{
    public partial class StatsPageViewModel : ObservableObject
    {
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

        [ObservableProperty]
        public ObservableCollection<SfCartesianChart> charts = new() { new SfCartesianChart() };

        /// <summary>
        /// Gets active accounts from database, groups them by account type,
        /// adds them to the corresponding collection, then adds balance to corresponding sum
        /// </summary>
        public async Task getAccounts()
        {
            var accounts = await AccountService.GetActiveAccounts();
            if (accounts.Any())
            {
                CheckingsAccounts.Clear();
                SavingsAccounts.Clear();
                CreditAccounts.Clear();
                InvestmentAccounts.Clear();
                CheckingsSum = 0;
                SavingsSum = 0;
                CreditSum = 0;
                InvestmentSum = 0;
                Networth = 0;
            }
            else
                return;

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
