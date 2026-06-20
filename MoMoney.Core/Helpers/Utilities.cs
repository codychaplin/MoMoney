using System.Text.Json;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Messaging;

namespace MoMoney.Core.Helpers;

public static class Utilities
{
    // if false, sensitive values are hidden
    public static bool ShowValue { get; set; } = true;

    // Changes when developer mode is toggled
    public static bool IsAdmin 
    {
        get { return Preferences.Get("IsAdmin", false); }
        set { Preferences.Set("IsAdmin", value); }
    }

    // AI feature visibility toggle
    public static bool TransactionDictationEnabled
    {
        get { return Preferences.Get("TransactionDictationEnabled", false); }
        set { Preferences.Set("TransactionDictationEnabled", value); }
    }

    public static string CurrentTheme
    {
        get { return Preferences.Get("CurrentTheme", "green"); }
        set { Preferences.Set("CurrentTheme", value); }
    }

    public static bool MaterialYouEnabled
    {
        get { return Preferences.Get("MaterialYou", false); }
        set { Preferences.Set("MaterialYou", value); }
    }

    public static Color GetColour(string lightColour, string darkColour) => UraniumUI.Resources.ColorResource.GetColor(lightColour, darkColour);
    public static Color GetColour(string colour) => UraniumUI.Resources.ColorResource.GetColor(colour);

    /// <summary>
    /// Displays a toast message to the user.
    /// </summary>
    /// <param name="message"></param>
    public static async Task DisplayToast(string message, ToastDuration duration = ToastDuration.Long, double fontSize = 14)
    {
        var toast = Toast.Make(message, duration, fontSize);
        await toast.Show();
    }

    /// <summary>
    /// Applies the selected theme from the corresponding json file. Use material variant if enabled.
    /// </summary>
    /// <param name="themeName"></param>
    public static void ApplyTheme(string themeName)
    {
        using var stream = FileSystem.OpenAppPackageFileAsync($"theme_{themeName}.json").GetAwaiter().GetResult();
        using var reader = new StreamReader(stream);
        using var doc = JsonDocument.Parse(reader.ReadToEnd());
        var root = doc.RootElement;
        var res = Application.Current!.Resources;

        bool useMaterial = MaterialYouEnabled;
        string lightPrefix = useMaterial ? "lightMaterialYou" : "light";
        string darkPrefix = useMaterial ? "darkMaterialYou" : "dark";

        if (root.TryGetProperty(lightPrefix, out var light))
            foreach (var prop in light.EnumerateObject())
                if (Color.TryParse(prop.Value.GetString(), out var c))
                    res[prop.Name] = c;

        if (root.TryGetProperty(darkPrefix, out var dark))
            foreach (var prop in dark.EnumerateObject())
                if (Color.TryParse(prop.Value.GetString(), out var c))
                    res[prop.Name + "Dark"] = c;

        // workaround for updating components that don't update properly
        WeakReferenceMessenger.Default.Send(new UpdateThemeMessage(themeName));
    }
}

public class ThemeInfo(string name, Color colour)
{
    public string Name { get; } = name;
    public Color Colour { get; } = colour;
}

/// <summary>
/// Helps to map Transaction IDs to OpenAI logs.
/// </summary>
/// <param name="whisperResponseID"></param>
/// <param name="chatResponseID"></param>
public class ResponseIDs(int whisperResponseID, int chatResponseID)
{
    public int WhisperResponseID = whisperResponseID;
    public int ChatResponseID = chatResponseID;
}