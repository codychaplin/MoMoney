﻿using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels.Stats;

public partial class AccountSummaryViewModel : ObservableObject
{
    readonly IAccountService accountService;

    [ObservableProperty] ObservableCollection<Account> checkingsAccounts = [];
    [ObservableProperty] ObservableCollection<Account> savingsAccounts = [];
    [ObservableProperty] ObservableCollection<Account> creditAccounts = [];
    [ObservableProperty] ObservableCollection<Account> investmentAccounts = [];

    [ObservableProperty] decimal checkingsSum = 0;
    [ObservableProperty] decimal savingsSum = 0;
    [ObservableProperty] decimal creditSum = 0;
    [ObservableProperty] decimal investmentSum = 0;
    [ObservableProperty] decimal networth = 0;

    public AccountSummaryViewModel(IAccountService _accountService, ILoggerService<AccountSummaryViewModel> _loggerService)
    {
        accountService = _accountService;
        _loggerService.LogFirebaseEvent(FirebaseParameters.EVENT_VIEW_ACCOUNTS, FirebaseParameters.GetFirebaseParameters());
    }

    /// <summary>
    /// Gets active accounts from database, groups them by account type,
    /// adds them to the corresponding collection, then adds balance to corresponding sum
    /// </summary>
    public async void Init(object sender, EventArgs e)
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
}
