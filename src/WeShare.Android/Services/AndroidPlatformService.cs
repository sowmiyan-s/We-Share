using System;
using System.IO;
using System.Threading.Tasks;
using Android.Content;
using Android.Net.Wifi;
using Android.OS;
using WeShare.Core.Services;
using Avalonia;
using Avalonia.Android;

namespace WeShare.Android.Services
{
    public class AndroidPlatformService : IPlatformService
    {
        public bool IsHotspotRunning { get; private set; }
        public string HotspotIp { get; private set; } = "192.168.43.1"; // Default for Android
        private WifiManager.MulticastLock? _multicastLock;

        public string GetDeviceType() => "Phone";

        public string GetDefaultSavePath()
        {
            // Use Public Downloads folder
            string root = global::Android.OS.Environment.GetExternalStoragePublicDirectory(global::Android.OS.Environment.DirectoryDownloads)?.AbsolutePath 
                        ?? "/sdcard/Download";
            var path = Path.Combine(root, "WeShare");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            return path;
        }

        public async Task<(bool Success, string Message)> StartHotspotAsync(string ssid, string password)
        {
            // Note: On modern Android (Oreo+), you can't set SSID/PWD programmatically for LocalOnlyHotspot.
            // It generates a random SSID/PWD.
            try
            {
                var context = global::Android.App.Application.Context;
                var wifiManager = (WifiManager)context.GetSystemService(Context.WifiService)!;
                
                // This is a simplified placeholder. Actual implementation needs 
                // LocalOnlyHotspotCallback to get the credentials.
                return (false, "Please enable Hotspot manually from settings and share credentials.");
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public Task StopHotspotAsync()
        {
            IsHotspotRunning = false;
            return Task.CompletedTask;
        }

        public async Task<bool> RequestPermissionsAsync()
        {
            try
            {
                var context = global::Android.App.Application.Context;
                var wifi = (WifiManager)context.GetSystemService(Context.WifiService)!;
                if (_multicastLock == null)
                {
                    _multicastLock = wifi.CreateMulticastLock("WeShareLock");
                    _multicastLock.SetReferenceCounted(true);
                }
                _multicastLock.Acquire();
                return true;
            }
            catch { return false; }
        }

        public void OpenFile(string path)
        {
            try
            {
                var file = new Java.IO.File(path);
                var intent = new Intent(Intent.ActionView);
                var uri = global::AndroidX.Core.Content.FileProvider.GetUriForFile(global::Android.App.Application.Context, global::Android.App.Application.Context.PackageName + ".fileprovider", file);
                intent.SetDataAndType(uri, "*/*");
                intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.GrantReadUriPermission);
                global::Android.App.Application.Context.StartActivity(intent);
            }
            catch { }
        }

        public void OpenUrl(string url)
        {
            try
            {
                var intent = new Intent(Intent.ActionView, global::Android.Net.Uri.Parse(url));
                intent.SetFlags(ActivityFlags.NewTask);
                global::Android.App.Application.Context.StartActivity(intent);
            }
            catch { }
        }

        public void CopyToClipboard(string text)
        {
            var context = global::Android.App.Application.Context;
            var clipboard = (ClipboardManager)context.GetSystemService(Context.ClipboardService)!;
            var clip = ClipData.NewPlainText("WeShare", text);
            clipboard.PrimaryClip = clip;
        }

        public void ShareFile(string path)
        {
            try
            {
                var context = global::Android.App.Application.Context;
                var file = new Java.IO.File(path);
                var uri = global::AndroidX.Core.Content.FileProvider.GetUriForFile(context, context.PackageName + ".fileprovider", file);
                
                var intent = new Intent(Intent.ActionSend);
                intent.SetType("*/*");
                intent.PutExtra(Intent.ExtraStream, uri);
                intent.AddFlags(ActivityFlags.GrantReadUriPermission);
                
                var chooser = Intent.CreateChooser(intent, "Share File");
                chooser.AddFlags(ActivityFlags.NewTask);
                context.StartActivity(chooser);
            }
            catch { }
        }
    }
}
