using System.Globalization;
using MoMoney.Core.Helpers;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Converters;

/// <summary>
/// Returns transfer destination account name for transfers, otherwise the payee string.
/// Bindings: [CategoryID, TransferID, Payee]
/// </summary>
public class PayeeOrTransferConverter : IMultiValueConverter
{
    readonly IAccountService? accountService;

    public PayeeOrTransferConverter()
    {
        accountService = IPlatformApplication.Current?.Services.GetService<IAccountService>();
    }

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        // CategoryID, TransferID, Payee
        if (values.Length < 3)
            return "";

        if (!int.TryParse(values[0]?.ToString(), out int categoryID))
            return "";

        if (categoryID == Constants.TRANSFER_ID && int.TryParse(values[1]?.ToString(), out int transferAccountID))
        {
            if (accountService!.Accounts.TryGetValue(transferAccountID, out var account))
                return account.AccountName;
        }

        return values[2]?.ToString() ?? "";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
