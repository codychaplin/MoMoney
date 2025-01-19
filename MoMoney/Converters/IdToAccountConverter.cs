using System.Globalization;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Converters;

/// <summary>
/// Gets Account name from Account ID
/// </summary>
public class IdToAccountConverter : IValueConverter
{
    readonly IAccountService? accountService;
    readonly ILoggerService<IdToAccountConverter>? logger;

    public IdToAccountConverter()
    {
        accountService = IPlatformApplication.Current?.Services.GetService<IAccountService>();
        logger = IPlatformApplication.Current?.Services.GetService<ILoggerService<IdToAccountConverter>>();
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (int.TryParse(value?.ToString(), out int ID))
        {
            // try to get account from dictionary
            if (accountService!.Accounts.TryGetValue(ID, out var account))
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
                catch (Exception ex)
                {
                    logger?.LogError(nameof(IdToAccountConverter), ex);
                    return "";
                }
            }
        }

        return "";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}