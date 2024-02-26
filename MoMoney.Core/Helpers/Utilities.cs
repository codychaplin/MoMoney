using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Alerts;

namespace MoMoney.Core.Helpers;

public static class Utilities
{
    // if false, sensitive values are hidden
    public static bool ShowValue { get; set; } = true;

    public static ResourceDictionary Colours => Application.Current.Resources.MergedDictionaries.FirstOrDefault();

    /// <summary>
    /// Displays a toast message to the user.
    /// </summary>
    /// <param name="message"></param>
    public static async Task DisplayToast(string message, ToastDuration duration = ToastDuration.Long, double fontSize = 14)
    {
        var toast = Toast.Make(message, duration, fontSize);
        await toast.Show();
    }
}