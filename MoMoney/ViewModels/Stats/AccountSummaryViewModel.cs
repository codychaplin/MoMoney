using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Helpers;
using MoMoney.Models;
using MoMoney.Services;

namespace MoMoney.ViewModels.Stats;

public partial class AccountSummaryViewModel : ObservableObject
{
    readonly IAccountService accountService;

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

    public AccountSummaryViewModel(IAccountService _accountService)
    {
        accountService = _accountService;
    }

    /// <summary>
    /// Gets active accounts from database, groups them by account type,
    /// adds them to the corresponding collection, then adds balance to corresponding sum
    /// </summary>
    public async void Init(object sender, EventArgs e)
    {
        var accounts = await accountService.GetActiveAccounts();

        // checks if there are any accounts in db, then if the all the current balances are the same
        if (accounts.Any())
        {
            // update account type values
            foreach (var acc in accounts)
            {
                var type = Enum.Parse(typeof(AccountType), acc.AccountType);
                switch (type)
                {
                    case AccountType.Checkings:
                        CheckingsAccounts.Add(acc);
                        if (Utilities.ShowValue == false) break; // if ShowValue is false, skip calculations
                        CheckingsSum += acc.CurrentBalance;
                        Networth += acc.CurrentBalance;
                        break;
                    case AccountType.Savings:
                        SavingsAccounts.Add(acc);
                        if (Utilities.ShowValue == false) break;
                        SavingsSum += acc.CurrentBalance;
                        Networth += acc.CurrentBalance;
                        break;
                    case AccountType.Credit:
                        CreditAccounts.Add(acc);
                        if (Utilities.ShowValue == false) break;
                        CreditSum += acc.CurrentBalance;
                        Networth += acc.CurrentBalance;
                        break;
                    case AccountType.Investments:
                        InvestmentAccounts.Add(acc);
                        if (Utilities.ShowValue == false) break;
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
