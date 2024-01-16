using Android.OS;
using Android.App;
using Android.Content.PM;
using AndroidX.AppCompat.App;

namespace MoMoney;

[Activity(Theme = "@style/Maui.LightTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SupportActionBar.Hide();
        Window.SetSoftInputMode(Android.Views.SoftInput.AdjustResize);
        AppCompatDelegate.DefaultNightMode = AppCompatDelegate.ModeNightNo;
    }
    public override void OnConfigurationChanged(Android.Content.Res.Configuration newConfig)
    {
        base.OnConfigurationChanged(newConfig);

        if (AppInfo.RequestedTheme == AppTheme.Light)
        {
            SetTheme(Resource.Style.Maui_LightTheme);
            AppCompatDelegate.DefaultNightMode = AppCompatDelegate.ModeNightNo;
        }
        else
        {
            SetTheme(Resource.Style.Maui_DarkTheme);
            AppCompatDelegate.DefaultNightMode = AppCompatDelegate.ModeNightYes;
        }
    }
}