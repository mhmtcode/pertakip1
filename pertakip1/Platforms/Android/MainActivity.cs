using Android.App;
using Android.Content.PM;

namespace pertakip1
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
      ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode
          | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density,
      ScreenOrientation = ScreenOrientation.FullUser)]
    public class MainActivity : MauiAppCompatActivity
    {
    }
}
