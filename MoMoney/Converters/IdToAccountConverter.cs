using System.Globalization;
using MoMoney.Services;
using MoMoney.Exceptions;

namespace MoMoney.Converters;

/// <summary>
/// Gets Account name from Account ID
/// </summary>
class IdToAccountConverter : IValueConverter
{
    readonly IAccountService accountService;

    public IdToAccountConverter()
    {
        accountService = MauiApplication.Current.Services.GetService<IAccountService>();
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (int.TryParse(value.ToString(), out int ID))
        {
            // try to get account from dictionary
            if (accountService.Accounts.TryGetValue(ID, out var account))
            {
                return account.AccountName;
            }
            else // get account from db
            {
                try
                {
                    var task = Task.Run(async () => await accountService.GetAccount(ID));
                    task.Wait();
                    var acc = task.Result;
                    return acc.AccountName;
                }
                catch (AccountNotFoundException) { return ""; }
            }
        }

        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}