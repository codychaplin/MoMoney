using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MoMoney.Core.Models;

namespace MoMoney.Core.Converters;

public class TransactionExportConverter : DefaultTypeConverter
{
    public static Dictionary<int, Account> accounts = [];
    public static Dictionary<int, Category> categories = [];

    public override string ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
    {
        if (!int.TryParse(value?.ToString(), out int result))
            return value?.ToString() ?? string.Empty;

        string name = memberMapData.Member?.Name ?? string.Empty;
        switch (name)
        {
            case "AccountID":
                return accounts[result].AccountName;
            case "CategoryID":
                return categories[result].CategoryName;
            case "SubcategoryID":
                return categories[result].CategoryName;
            default:
                break;
        }

        return value?.ToString() ?? string.Empty;
    }
}