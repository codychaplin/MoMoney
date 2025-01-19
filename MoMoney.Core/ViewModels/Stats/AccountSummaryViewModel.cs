using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels.Stats;

public partial class AccountSummaryViewModel : ObservableObject
{
    readonly IAccountService accountService;
    readonly ILoggerService<AccountSummaryViewModel> logger;

    [ObservableProperty] ObservableCollection<Account> checkingsAccounts = [];
    [ObservableProperty] ObservableCollection<Account> savingsAccounts = [];
    [ObservableProperty] ObservableCollection<Account> creditAccounts = [];
    [ObservableProperty] ObservableCollection<Account> investmentAccounts = [];

    [ObservableProperty] decimal checkingsSum;
    [ObservableProperty] decimal savingsSum;
    [ObservableProperty] decimal creditSum;
    [ObservableProperty] decimal investmentSum;
    [ObservableProperty] decimal networth;

    public AccountSummaryViewModel(IAccountService _accountService, ILoggerService<AccountSummaryViewModel> _loggerService)
    {
        accountService = _accountService;
        logger = _loggerService;
        logger.LogFirebaseEvent(FirebaseParameters.EVENT_VIEW_ACCOUNTS, FirebaseParameters.GetFirebaseParameters());
    }

    /// <summary>
    /// Gets active accounts from database, groups them by account type,
    /// adds them to the corresponding collection, then adds balance to corresponding sum
    /// </summary>
    public async Task LoadAccountsSummary()
    {
        try
        {
            var accounts = await accountService.GetActiveAccounts();

            // checks if there are any accounts in db, then if the all the current balances are the same
            if (!accounts.Any())
                return;

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
        catch (Exception ex)
        {
            await logger.LogError(nameof(LoadAccountsSummary), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }
}
