
namespace MoMoney.Core.Helpers;

public class FirebaseParameters
{
    public static string ClassName => "Class_Name";
    public static string FunctionName => "Function_Name";
    public static string Exception => "Exception";
    public static string AppVersion => "App_Version";

    public static string EVENT_OPEN_APP => "Open_App";
    public static string EVENT_BULK_FIND => "Bulk_Find";
    public static string EVENT_BULK_REPLACE => "Bulk_Replace";
    public static string EVENT_OPENAI_CALL => "OpenAI_Call";

    public static string EVENT_WARNING_LOG => "Warning_Log";
    public static string EVENT_ERROR_LOG => "Error_Log";
    public static string EVENT_CRITICAL_LOG => "Critical_Log";

    public static string EVENT_VIEW_ACCOUNTS => "View_Accounts";
    public static string EVENT_VIEW_STOCKS => "View_Stocks";
    public static string EVENT_VIEW_BREAKDOWN => "View_Breakdown";
    public static string EVENT_VIEW_INSIGHTS => "View_Insights";

    public static string EVENT_ADD_TRANSACTION => "Add_Transaction";
    public static string EVENT_EDIT_TRANSACTION => "Edit_Transaction";
    public static string EVENT_DELETE_TRANSACTION => "Delete_Transaction";
    public static string EVENT_IMPORT_TRANSACTIONS => "Import_Transactions";
    public static string EVENT_EXPORT_TRANSACTIONS => "Export_Transactions";
    public static string EVENT_REMOVE_ALL_TRANSACTIONS => "Remove_All_Transactions";

    public static string EVENT_ADD_ACCOUNT => "Add_Account";
    public static string EVENT_EDIT_ACCOUNT => "Edit_Account";
    public static string EVENT_DELETE_ACCOUNT => "Delete_Account";
    public static string EVENT_IMPORT_ACCOUNTS => "Import_Accounts";
    public static string EVENT_EXPORT_ACCOUNTS => "Export_Accounts";
    public static string EVENT_REMOVE_ALL_ACCOUNTS => "Remove_All_Accounts";

    public static string EVENT_ADD_CATEGORY => "Add_Category";
    public static string EVENT_EDIT_CATEGORY => "Edit_Category";
    public static string EVENT_DELETE_CATEGORY => "Delete_Category";
    public static string EVENT_IMPORT_CATEGORIES => "Import_Categories";
    public static string EVENT_EXPORT_CATEGORIES => "Export_Categories";
    public static string EVENT_REMOVE_ALL_CATEGORIES => "Remove_All_Categories";

    public static string EVENT_ADD_STOCK => "Add_Stock";
    public static string EVENT_EDIT_STOCK => "Edit_Stock";
    public static string EVENT_DELETE_STOCK => "Delete_Stock";
    public static string EVENT_IMPORT_STOCKS => "Import_Stocks";
    public static string EVENT_EXPORT_STOCKS => "Export_Stocks";
    public static string EVENT_REMOVE_ALL_STOCKS => "Remove_All_Stocks";

    public static string EVENT_IMPORT_LOGS => "Import_Logs";
    public static string EVENT_EXPORT_LOGS => "Export_Logs";
    public static string EVENT_REMOVE_ALL_LOGS => "Remove_All_Logs";

    public static string EVENT_REMOVE_ALL_DATA => "Remove_All_Data";

    public static Dictionary<string, string> GetFirebaseParameters(Exception ex = null, string functionName = null, string className = null)
    {
        var parameters = new Dictionary<string, string>
        {
            { AppVersion, AppInfo.Current.VersionString }
        };

        if (ex != null && !string.IsNullOrEmpty(functionName) && !string.IsNullOrEmpty(className))
        {
            parameters.Add(ClassName, className);
            parameters.Add(FunctionName, functionName);
            parameters.Add(Exception, ex.GetType().Name);
        }

        return parameters;
    }
}