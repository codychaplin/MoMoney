using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MoMoney.Core.Helpers;
using MoMoney.Core.Exceptions;

namespace MoMoney.Core.Converters;

public class TransactionImportConverter : DefaultTypeConverter
{
    public static Dictionary<string, int> accounts;
    public static Dictionary<string, int> categories; // string is in 'subcategory,parentCategory' format

    public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
    {
        string name = memberMapData.Member.Name;
        switch (name)
        {
            case "AccountID":
                if (accounts.TryGetValue(text, out int accountId))
                    return accountId;
                break;
            case "CategoryID":
                if (categories.TryGetValue(text + ",", out int categoryId))
                    return categoryId;
                break;
            case "SubcategoryID":
                var parentName = row.GetField(3);
                if (categories.TryGetValue($"{text},{parentName}", out int subcategoryId))
                    return subcategoryId;
                break;
            case "Payee":
                var parentId = categories[row.GetField(3) + ","];
                if (parentId != Constants.TRANSFER_ID && string.IsNullOrEmpty(text))
                    throw new InvalidTransactionException($"Transaction {row.Parser.Row}: Payee cannot be blank");
                return text;
            default:
                break;
        }

        throw new InvalidTransactionException($"Transaction {row.Parser.Row}: '{text} is not a valid value for '{name}'");
    }
}