using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;
using WeShare.UI;

namespace WeShare.Android
{
    [Activity(Label = "We Share", 
              Theme = "@style/MainTheme", 
              Icon = "@drawable/icon", 
              MainLauncher = true, 
              ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
    public class MainActivity : AvaloniaMainActivity<App>
    {
        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            UI.App.PlatformService = new Services.AndroidPlatformService();
            return base.CustomizeAppBuilder(builder);
        }
    }
}
