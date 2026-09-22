using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using WeShare.Core.Models;

namespace WeShare.UI.Views
{
    public partial class MainView : UserControl
    {
        // ── Transfer History & Library ────────────────────────────────────────

        private async void RefreshHistory()
        {
            var history = await _dbHelper.GetAllTransfersAsync();
            var query = FileSearchBox?.Text?.ToLower() ?? "";
            
            _isUpdatingLibrary = true;
            try
            {
                ReceivedFiles.Clear();
                var allDone = history.Where(t => t.Status == TransferStatus.Done).ToList();
                
                var now = DateTime.Now;
                var filteredByDate = allDone.Where(t =>
                {
                    if (_currentDateFilter == "Today")
                    {
                        return t.Timestamp.ToLocalTime().Date == now.Date;
                    }
                    if (_currentDateFilter == "Week")
                    {
                        return (now.Date - t.Timestamp.ToLocalTime().Date).TotalDays <= 7;
                    }
                    return true;
                }).ToList();
                
                foreach (var h in filteredByDate)
                {
                    if (string.IsNullOrEmpty(query) || h.FileName.ToLower().Contains(query))
                        ReceivedFiles.Add(h);
                }
                
                UpdateStats(allDone);
            }
            finally
            {
                _isUpdatingLibrary = false;
            }
            UpdateLibraryFilesList();
            
            if (HomeEmptyHistoryLabel != null)
                HomeEmptyHistoryLabel.IsVisible = ReceivedFiles.Count == 0;
        }

        private async void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            await _dbHelper.ClearHistoryAsync();
            RefreshHistory();
        }

        private async void DeleteHistoryItem_Click(object sender, RoutedEventArgs e)
        {
            var state = (sender as Button)?.DataContext as FileTransferState;
            if (state == null) return;
            try
            {
                await _dbHelper.DeleteTransferAsync(state.FileId);
                RefreshHistory();
            }
            catch (Exception ex) { ShowToast($"Error deleting: {ex.Message}"); }
        }

        private async void DeleteFileInList_Click(object sender, RoutedEventArgs e)
        {
            var file = (sender as Button)?.Tag as FileTransferState;
            if (file == null) return;

            try
            {
                if (File.Exists(file.FilePath))
                    File.Delete(file.FilePath);

                await _dbHelper.DeleteTransferAsync(file.FileId);
                ReceivedFiles.Remove(file);
                UpdateLibraryFilesList();
                ShowToast("File deleted");
            }
            catch (Exception ex) { ShowToast($"Error deleting: {ex.Message}"); }
        }

        private void FileSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshHistory();
        }

        private void FilterDate_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string filter)
            {
                _currentDateFilter = filter;

                var chips = new[] { FilterAllBtn, FilterTodayBtn, FilterWeekBtn };
                foreach (var chip in chips)
                {
                    if (chip != null)
                    {
                        chip.Classes.Set("ActiveChip", chip == btn);
                    }
                }

                RefreshHistory();
            }
        }

        private void UpdateStats(System.Collections.Generic.List<FileTransferState> allDone)
        {
            if (LibraryStatsText == null) return;

            if (allDone.Count == 0)
            {
                LibraryStatsText.Text = "No transfers recorded";
                return;
            }

            int sentCount = allDone.Count(t => t.Direction == TransferDirection.Sent);
            int recvCount = allDone.Count(t => t.Direction == TransferDirection.Received);
            long totalBytes = allDone.Sum(t => t.TotalBytes);
            string sizeDisplay = FileTransferState.FormatBytes(totalBytes);

            var dates = allDone.Select(t => t.Timestamp.ToLocalTime()).OrderBy(d => d).ToList();
            var minDate = dates.First();
            var maxDate = dates.Last();

            string rangeDisplay = minDate.Date == maxDate.Date
                ? minDate.ToString("MMM d, yyyy")
                : $"{minDate:MMM d} - {maxDate:MMM d, yyyy}";

            LibraryStatsText.Text = $"Sent: {sentCount} • Received: {recvCount} • Total: {sizeDisplay}  •  Since {rangeDisplay}";
        }

        private void LoadReceivedFiles()
        {
            RefreshHistory();
        }

        private void UpdateLibraryFilesList()
        {
            if (_isUpdatingLibrary) return;
            if (_isLibraryUpdatePending) return;
            _isLibraryUpdatePending = true;

            Dispatcher.UIThread.Post(() =>
            {
                _isLibraryUpdatePending = false;
                if (_isUpdatingLibrary) return;

                var query = FileSearchBox?.Text?.ToLower() ?? "";

                var activeToShow = ActiveReceives.Where(r => string.IsNullOrEmpty(query) || r.FileName.ToLower().Contains(query)).ToList();
                var completedToShow = ReceivedFiles.Where(r => string.IsNullOrEmpty(query) || r.FileName.ToLower().Contains(query)).ToList();

                LibraryFiles.Clear();
                foreach (var file in activeToShow)
                {
                    LibraryFiles.Add(file);
                }
                foreach (var file in completedToShow)
                {
                    LibraryFiles.Add(file);
                }

                if (HistoryEmptyState != null)
                    HistoryEmptyState.IsVisible = LibraryFiles.Count == 0;
            });
        }

        private void OpenDownloadFolder_Click(object sender, RoutedEventArgs e) => _platformService.OpenUrl($"file://{_saveDirectory}");
        private void OpenFileInList_Click(object sender, RoutedEventArgs e) { if ((sender as Button)?.Tag is FileTransferState s) _platformService.OpenFile(s.FilePath); }
        
        private async void CopyFileToClipboard_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is FileTransferState s && !string.IsNullOrEmpty(s.FilePath) && File.Exists(s.FilePath))
            {
                var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                if (clipboard != null)
                {
                    var dataObject = new DataObject();
                    dataObject.Set(DataFormats.Files, new[] { s.FilePath });
                    dataObject.Set(DataFormats.Text, s.FilePath);
                    await clipboard.SetDataObjectAsync(dataObject);
                    ShowToast($"Copied '{s.FileName}' to clipboard");
                }
            }
        }

        private void FileRow_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            if ((sender as Control)?.DataContext is FileTransferState s && !string.IsNullOrEmpty(s.FilePath) && File.Exists(s.FilePath))
            {
                var ext = Path.GetExtension(s.FilePath).ToLowerInvariant();
                if (ext is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".webp")
                {
                    ShowImagePreview(s);
                    return;
                }
                _platformService.OpenFile(s.FilePath);
            }
        }

        private void OpenFolderInList_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is FileTransferState s && !string.IsNullOrEmpty(s.FilePath))
            {
                string? dir = Path.GetDirectoryName(s.FilePath);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                {
                    _platformService.OpenUrl($"file://{dir}");
                }
            }
        }

        private async void ClearCompletedTransfers_Click(object sender, RoutedEventArgs e)
        {
            var completed = ReceivedFiles.Where(f => f.IsCompleted || f.Status == TransferStatus.Failed).ToList();
            int count = completed.Count;
            foreach (var c in completed)
            {
                await _dbHelper.DeleteTransferAsync(c.FileId);
                ReceivedFiles.Remove(c);
            }
            RefreshHistory();
            UpdateLibraryFilesList();
            ShowToast($"Cleared {count} transfer(s)");
        }

        private async void ClearAllHistory_Click(object sender, RoutedEventArgs e)
        {
            await _dbHelper.ClearHistoryAsync();
            ReceivedFiles.Clear();
            if (HistoryEmptyState != null) HistoryEmptyState.IsVisible = true;
            if (HomeEmptyHistoryLabel != null) HomeEmptyHistoryLabel.IsVisible = true;
            ShowToast("Transfer history cleared");
        }

        // ── Settings, Preferences & Sound Effects ─────────────────────────────

        private async void ChangeSaveLocation_Click(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;
            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions { Title = "Choose save folder", AllowMultiple = false });
            if (folders.Count == 0) return;
            _saveDirectory = folders[0].Path.LocalPath;
            if (SettingsSaveLocationLabel != null) SettingsSaveLocationLabel.Text = _saveDirectory;
            _transferManager.StopListening();
            _transferManager.StartListening(_saveDirectory);
        }

        private void ThemeSwitch_Changed(object sender, RoutedEventArgs e)
        {
            if (Application.Current != null && sender is ToggleSwitch toggle)
            {
                Application.Current.RequestedThemeVariant = toggle.IsChecked == true
                    ? Avalonia.Styling.ThemeVariant.Dark
                    : Avalonia.Styling.ThemeVariant.Light;
            }
        }

        private void AutoAcceptSwitch_Changed(object? sender, RoutedEventArgs e)
        {
            _autoAcceptAllTransfers = AutoAcceptToggle?.IsChecked ?? false;
            _dbHelper.SetSetting("AutoAcceptTransfers", _autoAcceptAllTransfers ? "true" : "false");
            ShowToast(_autoAcceptAllTransfers ? "Auto-accept enabled" : "Auto-accept disabled");
        }

        private void SoundSwitch_Changed(object? sender, RoutedEventArgs e)
        {
            _soundEffectsEnabled = SoundToggle?.IsChecked ?? true;
            _dbHelper.SetSetting("SoundEffects", _soundEffectsEnabled ? "true" : "false");
            ShowToast(_soundEffectsEnabled ? "Sound effects enabled" : "Sound effects muted");
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool MessageBeep(uint uType);

        private void PlaySound(string soundType)
        {
            if (!_soundEffectsEnabled) return;
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    if (soundType == "complete")
                        MessageBeep(0x00000040); // MB_ICONASTERISK
                    else if (soundType == "request")
                        MessageBeep(0x00000030); // MB_ICONEXCLAMATION
                }
            }
            catch { }
        }

        private void AccentColor_Click(object? sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is string hex && !string.IsNullOrEmpty(hex))
            {
                ApplyAccentColor(hex);
                _dbHelper.SetSetting("AccentColor", hex);
                ShowToast("Accent color updated");
            }
        }

        private void ApplyAccentColor(string hex)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(hex) || 
                    hex.Equals("#E5A50A", StringComparison.OrdinalIgnoreCase) || 
                    hex.Equals("#F59E0B", StringComparison.OrdinalIgnoreCase) || 
                    hex.Equals("#EC4899", StringComparison.OrdinalIgnoreCase) || 
                    hex.Equals("#D900FF", StringComparison.OrdinalIgnoreCase) ||
                    hex.Equals("#7C3AED", StringComparison.OrdinalIgnoreCase))
                {
                    hex = "#4F46E5";
                }

                var color = Avalonia.Media.Color.Parse(hex);
                var brush = new Avalonia.Media.SolidColorBrush(color);
                this.Resources["BrandVioletBrush"] = brush;
                this.Resources["ElectricIndigoBrush"] = brush;
                this.Resources["SpotifyGreenBrush"] = brush;

                byte rDim = (byte)Math.Max(0, color.R - 20);
                byte gDim = (byte)Math.Max(0, color.G - 20);
                byte bDim = (byte)Math.Max(0, color.B - 20);
                var dimBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(rDim, gDim, bDim));
                this.Resources["BrandVioletDimBrush"] = dimBrush;

                if (Application.Current != null)
                {
                    Application.Current.Resources["BrandVioletBrush"] = brush;
                    Application.Current.Resources["BrandVioletDimBrush"] = dimBrush;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Accent] Apply color error: {ex.Message}");
            }
        }

        // ── Automatic App Updates ─────────────────────────────────────────────

        private async void CheckForUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsCheckUpdateBtn == null || SettingsUpdateStatusText == null) return;

            SettingsCheckUpdateBtn.IsEnabled = false;
            SettingsUpdateStatusText.Text = "Checking for updates...";
            
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("WeShare-Updater");
                
                var response = await client.GetAsync("https://api.github.com/repos/sowmiyan-s/We-Share/releases/latest");
                if (!response.IsSuccessStatusCode)
                {
                    SettingsUpdateStatusText.Text = $"Failed to check updates (HTTP {response.StatusCode})";
                    SettingsCheckUpdateBtn.IsEnabled = true;
                    return;
                }
                
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("tag_name", out var tagProp))
                {
                    var tagName = tagProp.GetString()?.Trim();
                    if (!string.IsNullOrEmpty(tagName))
                    {
                        var latestVersionStr = tagName.TrimStart('v');
                        if (Version.TryParse(latestVersionStr, out var latestVersion) && 
                            Version.TryParse(CurrentVersion, out var currentVersion))
                        {
                            if (latestVersion > currentVersion)
                            {
                                string? downloadUrl = null;
                                if (root.TryGetProperty("assets", out var assetsProp) && assetsProp.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var asset in assetsProp.EnumerateArray())
                                    {
                                        if (asset.TryGetProperty("name", out var nameProp) && 
                                            asset.TryGetProperty("browser_download_url", out var urlProp))
                                        {
                                            var assetName = nameProp.GetString();
                                            if (assetName != null && assetName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                                            {
                                                downloadUrl = urlProp.GetString();
                                                break;
                                            }
                                        }
                                    }
                                }
                                
                                if (!string.IsNullOrEmpty(downloadUrl))
                                {
                                    _latestVersionDownloadUrl = downloadUrl;
                                    _latestVersionName = tagName;
                                    SettingsUpdateStatusText.Text = $"Update available: {tagName}!";
                                    if (SettingsDownloadInstallBtn != null) SettingsDownloadInstallBtn.IsVisible = true;
                                    ShowToast($"Update {tagName} is available!");
                                }
                                else
                                {
                                    SettingsUpdateStatusText.Text = $"Update available ({tagName}), but no installer found.";
                                }
                            }
                            else
                            {
                                SettingsUpdateStatusText.Text = "You are running the latest version.";
                                ShowToast("You are running the latest version.");
                            }
                        }
                        else
                        {
                            SettingsUpdateStatusText.Text = "Failed to parse version information.";
                        }
                    }
                }
                else
                {
                    SettingsUpdateStatusText.Text = "Failed to retrieve release information.";
                }
            }
            catch (Exception ex)
            {
                SettingsUpdateStatusText.Text = $"Error checking updates: {ex.Message}";
                ShowToast("Failed to check for updates");
            }
            finally
            {
                SettingsCheckUpdateBtn.IsEnabled = true;
            }
        }

        private async void DownloadInstallUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_latestVersionDownloadUrl)) return;
            if (SettingsDownloadInstallBtn == null || SettingsCheckUpdateBtn == null || 
                SettingsUpdateProgressPanel == null || SettingsUpdateProgressBar == null || 
                SettingsUpdateProgressPct == null || SettingsUpdateStatusText == null) return;
            
            SettingsDownloadInstallBtn.IsEnabled = false;
            SettingsCheckUpdateBtn.IsEnabled = false;
            SettingsUpdateProgressPanel.IsVisible = true;
            SettingsUpdateProgressBar.Value = 0;
            SettingsUpdateProgressPct.Text = "0%";
            
            try
            {
                using var client = new HttpClient();
                using var response = await client.GetAsync(_latestVersionDownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                
                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var tempPath = Path.Combine(Path.GetTempPath(), $"WeShare_Setup_Update.exe");
                
                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
                {
                    var buffer = new byte[81920];
                    long totalRead = 0;
                    int read;
                    
                    while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, read);
                        totalRead += read;
                        
                        if (totalBytes > 0)
                        {
                            double pct = (double)totalRead / totalBytes * 100;
                            Dispatcher.UIThread.Post(() =>
                            {
                                SettingsUpdateProgressBar.Value = pct;
                                SettingsUpdateProgressPct.Text = $"{pct:F0}%";
                            });
                        }
                    }
                    await fileStream.FlushAsync();
                }
                
                ShowToast("Download complete. Starting installer...");
                
                var psi = new ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true
                };
                Process.Start(psi);
                
                Shutdown();
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                SettingsUpdateStatusText.Text = $"Failed to download update: {ex.Message}";
                ShowToast($"Update download failed: {ex.Message}");
                SettingsDownloadInstallBtn.IsEnabled = true;
                SettingsCheckUpdateBtn.IsEnabled = true;
                SettingsUpdateProgressPanel.IsVisible = false;
            }
        }

        // ── Shutdown & Cleanups ───────────────────────────────────────────────

        public void Shutdown()
        {
            try { _captivePortalService?.Stop(); } catch { }
            _discoveryService?.StopListening();
            _transferManager?.StopListening();
            _webDashboardService?.Stop();

            if (_hotspotService != null)
            {
                try { _hotspotService.StopAsync().GetAwaiter().GetResult(); }
                catch { }
            }

            _wifiConnector?.Cleanup();
            _wifiConnector?.Dispose();
 
            CleanWebSharedDirectory();
            CleanTempZipDirectory();
        }

        // ── Screenshots & Documentation ───────────────────────────────────────

        public async Task CaptureScreenshotsForDocsAsync()
        {
            try
            {
                _isCapturingScreenshots = true;
                string docsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "docs", "screenshot");
                docsDir = Path.GetFullPath(docsDir);
                if (!Directory.Exists(docsDir))
                {
                    docsDir = @"d:\PROJECTS\WE SHARE\docs\screenshot";
                }
                Directory.CreateDirectory(docsDir);

                if (Devices.Count == 0)
                {
                    Devices.Add(new DeviceModel { Id = "dev-1", Name = "MacBook Pro", Type = "Mac", IpAddress = "192.168.1.45" });
                    Devices.Add(new DeviceModel { Id = "dev-2", Name = "iPhone 15", Type = "iOS", IpAddress = "192.168.1.82" });
                    Devices.Add(new DeviceModel { Id = "dev-3", Name = "Galaxy S24", Type = "Android", IpAddress = "192.168.1.110" });
                    Devices.Add(new DeviceModel { Id = "web-1", Name = "iPhone Safari", Type = "Web Client", IpAddress = "192.168.1.88" });
                }

                void HideOverlays()
                {
                    if (ToastBorder != null) ToastBorder.IsVisible = false;
                }

                HideOverlays();
                ShowPanel(HomePanel, "HOME", NavHomeBtn);
                HideOverlays();
                await Task.Delay(400);
                SaveVisualToPng(Path.Combine(docsDir, "Home.png"));

                HideOverlays();
                ShowPanel(SendFilesPanel, "SEND FILES", null);
                HideOverlays();
                await Task.Delay(400);
                SaveVisualToPng(Path.Combine(docsDir, "sending_file.png"));

                HideOverlays();
                ShowPanel(SendDiscoveryPanel, "RADAR DISCOVERY", null);
                HideOverlays();
                await Task.Delay(400);
                SaveVisualToPng(Path.Combine(docsDir, "radar_discovery.png"));

                HideOverlays();
                ShowPanel(ReceiveModePanel, "RECEIVE MODE", null);
                HideOverlays();
                await Task.Delay(400);

                if (Devices.Count > 0)
                {
                    HideOverlays();
                    OpenDeviceSession(Devices[0]);
                    HideOverlays();
                    await Task.Delay(400);
                    SaveVisualToPng(Path.Combine(docsDir, "device_session.png"));
                }

                HideOverlays();
                UpdateWebSharedClientsList();
                ShowPanel(WebSharedPanel, "WEB TRANSFER", NavWebSharedBtn);
                HideOverlays();
                await Task.Delay(400);
                SaveVisualToPng(Path.Combine(docsDir, "web_portal.png"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CaptureScreenshots] Error: {ex}");
            }
            finally
            {
                _isCapturingScreenshots = false;
            }
        }

        private void SaveVisualToPng(string filePath)
        {
            try
            {
                int width = (int)Math.Max(960, Bounds.Width);
                int height = (int)Math.Max(640, Bounds.Height);
                var pixelSize = new Avalonia.PixelSize(width, height);
                var dpi = new Avalonia.Vector(96, 96);
                using var rtb = new Avalonia.Media.Imaging.RenderTargetBitmap(pixelSize, dpi);
                rtb.Render(this);
                rtb.Save(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SaveVisualToPng] Error: {ex.Message}");
            }
        }
    }
}
