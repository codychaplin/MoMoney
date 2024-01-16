using System.Globalization;
using MoMoney.Core.Services;
using MoMoney.Core.Exceptions;

namespace MoMoney.Core.Converters;

/// <summary>
/// Gets Account name from Account ID
/// </summary>
public class IdToAccountConverter : IValueConverter
{
#if ANDROID
    readonly IAccountService accountService;
    readonly ILoggerService<IdToAccountConverter> logger;
#endif

    public IdToAccountConverter()
    {
#if ANDROID
        accountService = IPlatformApplication.Current.Services.GetService<IAccountService>();
        logger = IPlatformApplication.Current.Services.GetService<ILoggerService<IdToAccountConverter>>();
#endif
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
#if ANDROID
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
                catch (AccountNotFoundException ex)
                {
                    logger.LogError(ex.Message, ex.GetType().Name);
                    return "";
                }
            }
        }
#endif

        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}