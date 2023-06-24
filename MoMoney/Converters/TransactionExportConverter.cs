using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MoMoney.Models;

namespace MoMoney.Converters;

public class TransactionExportConverter : DefaultTypeConverter
{
    public static Dictionary<int, Account> accounts;
    public static Dictionary<int, Category> categories;

    public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
    {
        string name = memberMapData.Member.Name;
        switch (name)
        {
            case "AccountID":
                return accounts[(int)value].AccountName;
            case "CategoryID":
                return categories[(int)value].CategoryName;
            case "SubcategoryID":
                return categories[(int)value].CategoryName;
            default:
                break;
        }

        return value.ToString();
    }
}