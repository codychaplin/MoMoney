using Android.OS;
using Android.App;
using Android.Content.PM;

namespace MoMoney;

[Activity(Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SupportActionBar.Hide();
        Window.SetSoftInputMode(Android.Views.SoftInput.AdjustResize);
    }
}