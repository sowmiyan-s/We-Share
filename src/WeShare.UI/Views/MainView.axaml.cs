using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WeShare.Core.Data;
using WeShare.Core.Discovery;
using WeShare.Core.Models;
using WeShare.Core.Network;
using WeShare.Core.Transfer;
using WeShare.Core.Services;

namespace WeShare.UI.Views
{
    public partial class MainView : UserControl
    {
        // ── Services ──────────────────────────────────────────────────────────
        private DeviceModel _localDevice;
        private UdpDiscoveryService _discoveryService;
        private TcpTransferManager _transferManager;
        private DatabaseHelper _dbHelper;
        private WebDashboardService _webDashboard;
        private IPlatformService _platformService;

        private string _webUrl = "http://localhost:8080";
        private string _selectedAdapterUrl = "";
        private string _saveDirectory;
        private DeviceModel? _sendTarget;

        // Observable collections
        public ObservableCollection<DeviceModel> Devices { get; } = new();
        private readonly ObservableCollection<string> _sendQueue = new();
        private readonly ObservableCollection<FileTransferState> _activeReceives = new();

        public MainView() : this(App.PlatformService) { }

        public MainView(IPlatformService? platformService)
        {
            InitializeComponent();

            _saveDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            _dbHelper = new DatabaseHelper();
            _platformService = platformService ?? new Services.WindowsPlatformService(); // Fallback for designer
            _localDevice = new DeviceModel { Port = 45679, Name = Environment.MachineName, Type = _platformService.GetDeviceType() };

            // Bind list sources
            DevicesList.ItemsSource    = Devices;
            SendQueueList.ItemsSource  = _sendQueue;
            IncomingList.ItemsSource   = _activeReceives;

            Devices.CollectionChanged += (_, _) => UpdateEmptyState();
            _sendQueue.CollectionChanged += (_, _) =>
                Dispatcher.UIThread.Post(UpdateQueueUI);
            _activeReceives.CollectionChanged += (_, _) =>
                Dispatcher.UIThread.Post(() => RecvEmptyState.IsVisible = _activeReceives.Count == 0);

            // Sync name boxes
            DeviceNameInput.Text    = _localDevice.Name;
            SettingsDeviceName.Text = _localDevice.Name;
            SaveLocationLabel.Text  = _saveDirectory;

            // Discovery
            _discoveryService = new UdpDiscoveryService(_localDevice);
            _discoveryService.DeviceDiscovered += OnDeviceDiscovered;
            _discoveryService.StartListening();

            // Transfer
            _transferManager = new TcpTransferManager(45679);
            _transferManager.LocalName = _localDevice.Name; // Sync name
            _transferManager.TransferStarted   += OnTransferStarted;
            _transferManager.TransferProgress  += OnTransferProgress;
            _transferManager.TransferCompleted += OnTransferCompleted;
            _transferManager.TransferFailed    += OnTransferFailed;
            _transferManager.TransferRequestCallback = OnTransferRequested;
            _transferManager.StartListening(_saveDirectory);

            // Web dashboard
            _webDashboard = new WebDashboardService(_saveDirectory, _localDevice);
            _webDashboard.SetPeersProvider(() => Devices);
            _webDashboard.Start();

            var localIp = UdpDiscoveryService.GetLocalIp();
            _webUrl           = $"http://{localIp}:8080";
            if (this.FindControl<TextBlock>("WebAccessUrl") is TextBlock wUrl) wUrl.Text = _webUrl;
            if (this.FindControl<TextBlock>("ConnectUrl") is TextBlock cUrl) cUrl.Text = _webUrl;
            UpdateQRCode(_webUrl);

            DispatcherTimer.RunOnce(() =>
            {
                if (this.FindControl<Grid>("SplashGrid") is Grid splash)
                {
                    splash.IsVisible = false;
                }
            }, TimeSpan.FromSeconds(3));

            // Broadcast loop
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    await _discoveryService.BroadcastPresenceAsync();
                    await Task.Delay(5000);
                }
            });

            // Initialize Save Location
            _saveDirectory = _platformService.GetDefaultSavePath();
            SaveLocationLabel.Text = _saveDirectory;

            // Mobile adjustments
            if (_platformService.GetDeviceType() == "Phone")
            {
                TitleBarSpacer.IsVisible = false;
                MainLayout.ColumnDefinitions[0].Width = new GridLength(0); // Hide sidebar on mobile by default
                Sidebar.IsVisible = false;
                _localDevice.Type = "Phone";
                _localDevice.Name = "My Mobile Device"; // Default for mobile
                DeviceNameInput.Text = _localDevice.Name;
                SettingsDeviceName.Text = _localDevice.Name;
                MyDeviceCard.IsVisible = false; // Hide on mobile
            }

            NavHome_Click(this, new RoutedEventArgs());
        }

        // ── Empty state ───────────────────────────────────────────────────────
        private void UpdateEmptyState() =>
            Dispatcher.UIThread.Post(() => EmptyState.IsVisible = Devices.Count == 0);

        // ── Navigation ────────────────────────────────────────────────────────
        private void HideAllPanels()
        {
            HomePanel.IsVisible        = false;
            RadarGrid.IsVisible        = false;
            SettingsPanel.IsVisible    = false;
            SendPanel.IsVisible        = false;
            ReceivePanel.IsVisible     = false;
            HistoryPanel.IsVisible     = false;
            ReceiveModePanel.IsVisible = false;
            AcceptRejectPanel.IsVisible = false;
        }

        private void NavHome_Click(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            HomePanel.IsVisible = true;
            PageTitle.Text = "Home";
            SetActiveNav(null); // No active nav button for home
        }

        private void NavRadar_Click(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            RadarGrid.IsVisible = true;
            PageTitle.Text = "Nearby Devices";
            SetActiveNav(NavRadarBtn);
        }

        private async void NavSend_Click(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            SendPanel.IsVisible = true;
            PageTitle.Text = "Manage Files";
            SetActiveNav(NavSendBtn);
            
            if (_sendQueue.Count == 0)
            {
                await BrowseFilesInternalAsync();
            }
            UpdateQueueUI();
        }

        private void NavReceiveMode_Click(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            ReceiveModePanel.IsVisible = true;
            PageTitle.Text = "Receive Mode";
            SetActiveNav(NavRecvBtn);

            string localIp = UdpDiscoveryService.GetLocalIp();
            StatusText.Text = "Waiting for nearby devices...";
            
            HotspotSSIDLabel.Text = $"Device: {_localDevice.Name}";
            HotspotPWDLabel.Text  = $"Network IP: {localIp}";

            // Connection string for manual scan (fallback)
            string conn = $"WESH_IP:{localIp};PORT:45679;NAME:{_localDevice.Name}";
            UpdateQRCode(conn, ReceiveQRImage);
        }

        private void NavReceive_Click(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            ReceivePanel.IsVisible = true;
            PageTitle.Text = "Receive";
            SetActiveNav(NavRecvBtn);
        }

        private void NavHistory_Click(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            HistoryPanel.IsVisible = true;
            PageTitle.Text = "History";
            SetActiveNav(NavHistBtn);
            LoadHistory();
        }

        private void NavSettings_Click(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            SettingsPanel.IsVisible = true;
            PageTitle.Text = "Settings";
            SetActiveNav(NavSettBtn);
            LoadAdapters();
        }

        private void SetActiveNav(Button? active)
        {
            foreach (var btn in new[] { NavHomeBtn, NavRadarBtn, NavSendBtn, NavRecvBtn, NavHistBtn, NavSettBtn })
                btn.Classes.Set("Active", btn == active);
        }

        private void SendFile_Click(object sender, RoutedEventArgs e)
        {
            var device = (sender as Button)?.DataContext as DeviceModel;
            if (device == null) return;

            _sendTarget = device;
            SendTargetLabel.Text = device.Name;
            
            HideAllPanels();
            SendPanel.IsVisible = true;
            PageTitle.Text = "Sending Files";
            
            SendNow_Click(this, new RoutedEventArgs());
        }

        // ── Send panel ────────────────────────────────────────────────────────
        private void SelectSendDevice_Click(object sender, RoutedEventArgs e)
        {
            var device = (sender as Button)?.DataContext as DeviceModel;
            if (device == null) return;
            _sendTarget = device;
            SendTargetLabel.Text = $"→ {device.Name}  ({device.IpAddress})";
        }

        private async void BrowseFiles_Click(object sender, RoutedEventArgs e)
        {
            await BrowseFilesInternalAsync();
        }

        private async Task BrowseFilesInternalAsync()
        {
            var files = await PickFilesAsync();
            foreach (var f in files)
                if (!_sendQueue.Contains(f)) _sendQueue.Add(f);
            UpdateQueueUI();
        }

        private void RemoveFromQueue_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is string path)
            {
                _sendQueue.Remove(path);
                UpdateQueueUI();
            }
        }

        private void UpdateQueueUI()
        {
            SendQueueCountLabel.Text = $"{_sendQueue.Count} file(s) selected";
            QueueEmptyLabel.IsVisible = _sendQueue.Count == 0;
            GoToRadarBtn.IsEnabled = _sendQueue.Count > 0;
        }

        private void GoToRadar_Click(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            RadarGrid.IsVisible = true;
            PageTitle.Text = "Select Receiver";
            StatusText.Text = $"Ready to send {_sendQueue.Count} file(s). Select a device above.";
        }

        private void ClearQueue_Click(object sender, RoutedEventArgs e) 
        {
            _sendQueue.Clear();
            UpdateQueueUI();
        }

        private void OnDropZoneDrop(object sender, DragEventArgs e)
        {
            var paths = e.Data.GetFiles()?.Select(f => f.Path.LocalPath).ToList();
            if (paths == null) return;
            foreach (var p in paths)
                if (File.Exists(p) && !_sendQueue.Contains(p)) _sendQueue.Add(p);
            UpdateQueueUI();
        }

        private async void SendNow_Click(object sender, RoutedEventArgs e)
        {
            if (_sendTarget == null)
            {
                StatusText.Text = "⚠ Select a target device first.";
                return;
            }
            if (_sendQueue.Count == 0) return;

            var toSend = _sendQueue.ToList();
            _sendQueue.Clear();
            UpdateQueueUI();
            SendProgressBorder.IsVisible = true;

            foreach (var path in toSend)
            {
                SendProgressFile.Text = Path.GetFileName(path);
                SendProgressBar.Value = 0;
                await _transferManager.SendFileAsync(_sendTarget.IpAddress, _sendTarget.Port, path);
            }

            SendProgressBorder.IsVisible = false;
            StatusText.Text = $"✓ Sent {toSend.Count} file(s) to {_sendTarget.Name}";
            ShowToast($"Sent {toSend.Count} file(s) to {_sendTarget.Name}", "IconSend");
        }

        private async Task<System.Collections.Generic.List<string>> PickFilesAsync()
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return new();
            var result = await topLevel.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions { Title = "Select files to send", AllowMultiple = true });
            return result.Select(f => f.Path.LocalPath).ToList();
        }

        // ── Receive panel ─────────────────────────────────────────────────────
        private async void ChangeSaveLocation_Click(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;
            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions { Title = "Choose save folder", AllowMultiple = false });
            if (folders.Count == 0) return;
            _saveDirectory = folders[0].Path.LocalPath;
            SaveLocationLabel.Text = _saveDirectory;
            // Restart listener on new directory
            _transferManager.StopListening();
            _transferManager.StartListening(_saveDirectory);
        }

        // ── History ───────────────────────────────────────────────────────────
        private void LoadHistory()
        {
            var records = _dbHelper.GetAllTransfers();
            HistoryList.ItemsSource = records;
            HistEmptyState.IsVisible = records.Count == 0;
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            _dbHelper.ClearHistory();
            LoadHistory();
        }

        // ── Settings: Adapter list ────────────────────────────────────────────
        private void RefreshAdapters_Click(object sender, RoutedEventArgs e) => LoadAdapters();

        private void LoadAdapters() =>
            AdaptersList.ItemsSource = NetworkHelper.GetActiveAdapters();

        private async void UseAdapter_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is not NetworkAdapterInfo adapter) return;

            _selectedAdapterUrl = $"http://{adapter.IpAddress}:8080";
            if (this.FindControl<TextBlock>("ConnectUrl") is TextBlock cUrl) cUrl.Text = _selectedAdapterUrl;
            if (this.FindControl<TextBlock>("WebAccessUrl") is TextBlock wUrl) wUrl.Text = _selectedAdapterUrl;
            _webUrl             = _selectedAdapterUrl;

            UpdateQRCode(_selectedAdapterUrl);

            if (this.FindControl<Button>("CopyHotspotUrlBtn") is Button copyBtn)
            {
                copyBtn.Content = "✓ URL Set!";
                await Task.Delay(1500);
                copyBtn.Content = "Copy URL";
            }
        }

        private async void CreateHotspot_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (_platformService.IsHotspotRunning)
            {
                await _platformService.StopHotspotAsync();
                ShowToast("Hotspot stopped.");
                if (btn != null) btn.Content = "Create Mobile Hotspot";
                UpdateQRCode(_webUrl); // Revert to web url
                return;
            }

            ShowToast("Starting hotspot... Please allow Admin prompt if asked.");
            var (success, msg) = await _platformService.StartHotspotAsync("WeShare-WiFi", "weshare123");
            if (success)
            {
                ShowToast("Hotspot created! SSID: 'WeShare-WiFi'");
                if (btn != null) btn.Content = "Stop Hotspot";
                
                // Set Dashboard URL to the Hotspot IP
                _selectedAdapterUrl = $"http://{_platformService.HotspotIp}:8080";
                if (this.FindControl<TextBlock>("ConnectUrl") is TextBlock cUrl) cUrl.Text = _selectedAdapterUrl;
                if (this.FindControl<TextBlock>("WebAccessUrl") is TextBlock wUrl) wUrl.Text = _selectedAdapterUrl;
                
                // Show Wi-Fi Connection QR code for easy connection
                UpdateQRCode($"WIFI:S:WeShare-WiFi;T:WPA;P:weshare123;;");
            }
            else
            {
                ShowToast($"Failed: {msg}", "IconClose", "#EF4444");
            }
        }

        private void UpdateQRCode(string data, Image? targetOverride = null)
        {
            try
            {
                byte[] png = QRCodeHelper.GeneratePng(data, 10);
                using var ms = new MemoryStream(png);
                var bitmap = new Bitmap(ms);

                if (targetOverride != null)
                {
                    targetOverride.Source = bitmap;
                }
                else if (this.FindControl<Image>("QRCodeImage") is Image qrImg)
                {
                    qrImg.Source = bitmap;
                }
            }
            catch { }
        }

        private async void CopyHotspotUrl_Click(object sender, RoutedEventArgs e)
        {
            var url = _selectedAdapterUrl.StartsWith("http") ? _selectedAdapterUrl : _webUrl;
            var cb = TopLevel.GetTopLevel(this)?.Clipboard;
            if (cb == null) return;
            await cb.SetTextAsync(url);
            CopyHotspotUrlBtn.Content = "✓ Copied";
            await Task.Delay(2000);
            CopyHotspotUrlBtn.Content = "📋 Copy URL";
        }

        private TaskCompletionSource<bool>? _acceptTcs;
        private async Task<bool> OnTransferRequested(FileTransferState state)
        {
            _acceptTcs = new TaskCompletionSource<bool>();
            
            Dispatcher.UIThread.Post(() =>
            {
                HideAllPanels();
                ReceiveModePanel.IsVisible = false; // Close QR panel if open
                AcceptRejectPanel.IsVisible = true;
                IncomingFileName.Text = state.FileName;
                IncomingPeerName.Text = $"From: {state.PeerName} ({state.RemoteIp})";
                IncomingFileSize.Text = FileTransferState.FormatBytes(state.TotalBytes);
                PageTitle.Text = "Incoming Request";
            });

            bool result = await _acceptTcs.Task;
            
            Dispatcher.UIThread.Post(() =>
            {
                AcceptRejectPanel.IsVisible = false;
                if (result)
                {
                    ReceivePanel.IsVisible = true;
                    PageTitle.Text = "Receiving...";
                }
                else
                {
                    NavHome_Click(this, new RoutedEventArgs());
                }
            });

            return result;
        }

        private void AcceptTransfer_Click(object sender, RoutedEventArgs e) => _acceptTcs?.TrySetResult(true);
        private void RejectTransfer_Click(object sender, RoutedEventArgs e) => _acceptTcs?.TrySetResult(false);

        // ── Transfer events ───────────────────────────────────────────────────
        private void OnDeviceDiscovered(DeviceModel device)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (!Devices.Any(d => d.Id == device.Id))
                {
                    Devices.Add(device);
                    StatusText.Text = $"{Devices.Count} device{(Devices.Count == 1 ? "" : "s")} found";
                }
            });
        }

        private void OnTransferStarted(FileTransferState state)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (state.Direction == TransferDirection.Received)
                {
                    _activeReceives.Add(state);
                    RecvEmptyState.IsVisible = false;
                }
                StatusText.Text = $"↓ Receiving {state.FileName}…";
                _dbHelper.SaveTransfer(state);
            });
        }

        private void OnTransferProgress(FileTransferState state)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (this.FindControl<ProgressBar>("TransferProgress") is ProgressBar tp)
                {
                    tp.IsVisible = true;
                    tp.Value = state.ProgressPercentage;
                }
                if (this.FindControl<TextBlock>("SpeedText") is TextBlock st)
                {
                    st.Text = $"{state.SpeedMbPerSec:F1} MB/s";
                }

                if (state.Direction == TransferDirection.Sent)
                {
                    SendProgressBar.Value  = state.ProgressPercentage;
                    SendProgressPct.Text   = $"{state.ProgressPercentage:F0}%";
                    SendProgressSpeed.Text = $"{state.SpeedMbPerSec:F1} MB/s";
                    StatusText.Text = $"↑ {state.FileName} — {state.ProgressPercentage:F0}%";
                }
                else
                {
                    StatusText.Text = $"↓ {state.FileName} — {state.ProgressPercentage:F0}%";
                }
            });
        }

        private void OnTransferCompleted(FileTransferState state)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (this.FindControl<ProgressBar>("TransferProgress") is ProgressBar tp) tp.IsVisible = false;
                SendProgressBorder.IsVisible = false;
                if (this.FindControl<TextBlock>("SpeedText") is TextBlock st) st.Text = "";

                if (state.Direction == TransferDirection.Received)
                {
                    var existing = _activeReceives.FirstOrDefault(s => s.FileId == state.FileId);
                    if (existing != null) _activeReceives.Remove(existing);
                }

                StatusText.Text = $"✓ {state.FileName} — done ({FileTransferState.FormatBytes(state.TotalBytes)})";
                _dbHelper.SaveTransfer(state);
                ShowToast($"{state.FileName} received!");

                // Refresh history if visible
                if (HistoryPanel.IsVisible) LoadHistory();
            });
        }

        private void OnTransferFailed(FileTransferState state)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (this.FindControl<ProgressBar>("TransferProgress") is ProgressBar tp) tp.IsVisible = false;
                SendProgressBorder.IsVisible = false;
                if (this.FindControl<TextBlock>("SpeedText") is TextBlock st) st.Text = "";

                if (state.Direction == TransferDirection.Received)
                {
                    var existing = _activeReceives.FirstOrDefault(s => s.FileId == state.FileId);
                    if (existing != null) _activeReceives.Remove(existing);
                }

                StatusText.Text = $"✕ Transfer failed: {state.FileName}";
                _dbHelper.SaveTransfer(state);
                ShowToast($"Failed: {state.FileName}", "IconClose", "#EF4444");
            });
        }

        private async void ShowToast(string message, string iconKey = "IconRadar", string color = "#38BDF8")
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (Resources.TryGetValue(iconKey, out var geometry) && geometry is StreamGeometry g)
                    ToastPath.Data = g;
                
                ToastPath.Fill = Avalonia.Media.Brush.Parse(color);
                ToastMessage.Text = message;
                ToastBorder.IsVisible = true;
                ToastBorder.Opacity = 1;
                ToastBorder.Margin = new Thickness(0, 0, 0, 120);
            });

            await Task.Delay(3000);

            Dispatcher.UIThread.Post(() =>
            {
                ToastBorder.Opacity = 0;
                ToastBorder.Margin = new Thickness(0, 0, 0, 100);
            });

            await Task.Delay(300);
            Dispatcher.UIThread.Post(() => ToastBorder.IsVisible = false);
        }

        // ── Copy URL (sidebar) ────────────────────────────────────────────────
        private async void CopyUrl_Click(object sender, RoutedEventArgs e)
        {
            var cb = TopLevel.GetTopLevel(this)?.Clipboard;
            if (cb == null) return;
            await cb.SetTextAsync(_webUrl);
            CopyUrlBtn.Content = "✓ Copied";
            await Task.Delay(2000);
            CopyUrlBtn.Content = "Copy";
        }

        // ── Device name sync ─────────────────────────────────────────────────
        private void DeviceNameInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb && _localDevice != null)
            {
                _localDevice.Name = string.IsNullOrWhiteSpace(tb.Text) ? "Unknown" : tb.Text;
                if (SettingsDeviceName.Text != _localDevice.Name)
                    SettingsDeviceName.Text = _localDevice.Name;
                if (_transferManager != null) _transferManager.LocalName = _localDevice.Name;
            }
        }

        private void SettingsDeviceName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb && _localDevice != null)
            {
                _localDevice.Name = string.IsNullOrWhiteSpace(tb.Text) ? "Unknown" : tb.Text;
                if (DeviceNameInput.Text != _localDevice.Name)
                    DeviceNameInput.Text = _localDevice.Name;
                if (_transferManager != null) _transferManager.LocalName = _localDevice.Name;
            }
        }

        public void Shutdown()
        {
            _discoveryService.StopListening();
            _transferManager.StopListening();
            _webDashboard.Stop();
            if (_platformService.IsHotspotRunning)
            {
                _platformService.StopHotspotAsync().Wait();
            }
        }
    }
}
