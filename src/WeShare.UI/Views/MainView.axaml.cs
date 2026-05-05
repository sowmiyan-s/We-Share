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
    public class QueueItem
    {
        public string Name { get; set; } = "";
        public long Size { get; set; }
        public Func<Task<Stream>> OpenStream { get; set; } = null!;
    }

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
        public ObservableCollection<QueueItem> SendQueue { get; } = new();
        public ObservableCollection<FileTransferState> ActiveReceives { get; } = new();
        public ObservableCollection<FileTransferState> ReceivedFiles { get; } = new();


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
            SendQueueList.ItemsSource  = SendQueue;
            IncomingList.ItemsSource   = ActiveReceives;

            Devices.CollectionChanged += (_, _) => UpdateEmptyState();
            SendQueue.CollectionChanged += (_, _) =>
                Dispatcher.UIThread.Post(UpdateQueueUI);
            ActiveReceives.CollectionChanged += (_, _) =>
                Dispatcher.UIThread.Post(() => RecvEmptyState.IsVisible = ActiveReceives.Count == 0);

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

            _saveDirectory = _platformService.GetDefaultSavePath();
            SaveLocationLabel.Text = _saveDirectory;
            SettingsSaveLocationLabel.Text = _saveDirectory;

            // Mobile adjustments for a "Perfect" stage
            if (_platformService.GetDeviceType() == "Phone")
            {
                TitleBarSpacer.IsVisible = false;
                Sidebar.IsVisible = false;
                BottomNav.IsVisible = true;
                MainLayout.ColumnDefinitions[0].Width = new GridLength(0);
                
                _localDevice.Type = "Phone";
                _localDevice.Name = "My Mobile Device";
                DeviceNameInput.Text = _localDevice.Name;
                SettingsDeviceName.Text = _localDevice.Name;
                MyDeviceCard.IsVisible = false;

                // Content area adjustments for touch & small screens
                ContentArea.Margin = new Thickness(0, 0, 0, 80); 
                PageTitle.FontSize = 22;
                PageTitle.Margin = new Thickness(20, 10, 20, 0);
                
                // Dashboard adjustments for mobile stacking
                HomeActionsStack.Orientation = Avalonia.Layout.Orientation.Vertical;
                HomeActionsStack.Spacing = 20;
                SendBox.Width = 300;
                SendBox.Height = 160;
                ReceiveBox.Width = 300;
                ReceiveBox.Height = 160;

                // Toast position for mobile (higher up)
                ToastBorder.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
                ToastBorder.Margin = new Thickness(20, 60, 20, 0);
                ToastBorder.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
            }

            ReceivedFilesList.ItemsSource = ReceivedFiles;
            
            // Heartbeat timer to remove stale devices
            DispatcherTimer.Run(() => {
                var stale = Devices.Where(d => (DateTime.Now - d.LastSeen).TotalSeconds > 30).ToList();
                foreach (var s in stale) Devices.Remove(s);
                if (stale.Count > 0) UpdateEmptyState();
                return true;
            }, TimeSpan.FromSeconds(5));

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
            AboutPanel.IsVisible       = false;
            ReceiveModePanel.IsVisible = false;
            AcceptRejectPanel.IsVisible = false;
            FilesPanel.IsVisible       = false;
            LegalPanel.IsVisible       = false;
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
            
            if (SendQueue.Count == 0)
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

        private void NavFiles_Click(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            FilesPanel.IsVisible = true;
            PageTitle.Text = "My Files";
            SetActiveNav(NavFilesBtn);
            LoadReceivedFiles();
        }

        private void NavSettings_Click(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            SettingsPanel.IsVisible = true;
            PageTitle.Text = "Settings";
            SetActiveNav(NavSettBtn);
            LoadAdapters();
        }

        private void NavAbout_Click(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            AboutPanel.IsVisible = true;
            PageTitle.Text = "About Us";
            SetActiveNav(null);
        }

        private void NavTOS_Click(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            LegalPanel.IsVisible = true;
            LegalTitle.Text = "Terms of Service";
            LegalContent.Text = "1. LOCAL NETWORK ONLY: All transfers occur over your local Wi-Fi. No data leaves your home network.\n\n2. SECURITY: You are responsible for ensuring your network is secure. Do not share files over public unencrypted Wi-Fi.\n\n3. NO WARRANTY: The software is provided 'as-is' without warranty of any kind.\n\n4. FAIR USE: Do not use this tool to transfer copyrighted material without permission.";
            PageTitle.Text = "Legal";
        }

        private void NavPrivacy_Click(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            LegalPanel.IsVisible = true;
            LegalTitle.Text = "Privacy Policy";
            LegalContent.Text = "1. DATA COLLECTION: We do NOT collect any personal information. All file names and contents are processed locally.\n\n2. ANALYTICS: We do not use tracking cookies or third-party analytics.\n\n3. PERMISSIONS: We only request permissions necessary for file transfer (Storage, Wi-Fi, Network).\n\n4. ADVERTISING: This application is ad-free and does not sell your data.";
            PageTitle.Text = "Legal";
        }

        private void SetActiveNav(Button? active)
        {
            foreach (var btn in new[] { NavHomeBtn, NavRadarBtn, NavSendBtn, NavRecvBtn, NavHistBtn, NavSettBtn, NavFilesBtn })
                if (btn != null) btn.Classes.Set("Active", btn == active);
        }

        private void ScanQR_Click(object sender, RoutedEventArgs e)
        {
            ShowToast("Opening Scanner...", "IconQR");
            // Simulation of opening camera
            DispatcherTimer.RunOnce(() => {
                ShowToast("Scanner not available in this build. Please enter IP manually.", "IconInfo", "#FFB000");
                ManualAdd_Click(this, new RoutedEventArgs());
            }, TimeSpan.FromSeconds(1.5));
        }

        private async void ManualAdd_Click(object sender, RoutedEventArgs e)
        {
            // Simple manual IP prompt (In a real app, use a proper dialog)
            // For now, we'll navigate to Settings where the manual IP info is shown
            ShowToast("Use the Web Dashboard URL in Settings to connect manually.", "IconInfo");
            NavSettings_Click(this, new RoutedEventArgs());
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

        private async void AddFiles_Click(object sender, RoutedEventArgs e)
        {
            var files = await PickFilesAsync();
            foreach (var f in files)
            {
                if (!SendQueue.Any(q => q.Name == f.Name))
                    SendQueue.Add(f);
            }
            UpdateQueueUI();
        }

        private async Task BrowseFilesInternalAsync()
        {
            var files = await PickFilesAsync();
            foreach (var f in files)
                if (!SendQueue.Any(q => q.Name == f.Name)) SendQueue.Add(f);
            UpdateQueueUI();
        }

        private void ShareForWeb_Click(object sender, RoutedEventArgs e)
        {
            if (SendQueue.Count == 0)
            {
                ShowToast("Add files to the queue first!", "IconClose", "#EF4444");
                return;
            }

            ShowToast("Shared to Web Dashboard!", "IconRadar", "#22C55E");
            SendQueue.Clear();
            UpdateQueueUI();
            
            // Navigate to Settings to show QR code
            NavSettings_Click(this, new RoutedEventArgs());
        }

        private void RemoveFromQueue_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is QueueItem item)
            {
                SendQueue.Remove(item);
                UpdateQueueUI();
            }
        }

        private void UpdateQueueUI()
        {
            SendQueueCountLabel.Text = $"{SendQueue.Count} file(s) selected";
            QueueEmptyLabel.IsVisible = SendQueue.Count == 0;
            GoToRadarBtn.IsEnabled = SendQueue.Count > 0;
        }

        private async void PairQR_Click(object sender, RoutedEventArgs e)
        {
            // For now, allow manual IP pairing since camera scanning requires more setup
            ShowToast("Coming soon: Native QR Scan. Use Manual IP in Settings.", "IconQR");
            NavSettings_Click(this, new RoutedEventArgs());
        }

        private void GoToRadar_Click(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            RadarGrid.IsVisible = true;
            PageTitle.Text = "Select Receiver";
            StatusText.Text = $"Ready to send {SendQueue.Count} file(s). Select a device above.";
        }

        private void ClearQueue_Click(object sender, RoutedEventArgs e) 
        {
            SendQueue.Clear();
            UpdateQueueUI();
        }

        private void OnDropZoneDrop(object sender, DragEventArgs e)
        {
            var files = e.Data.GetFiles();
            if (files == null) return;
            foreach (var f in files)
            {
                if (f is IStorageFile sFile && !SendQueue.Any(q => q.Name == f.Name))
                {
                    SendQueue.Add(new QueueItem { Name = sFile.Name, Size = 0L, OpenStream = () => sFile.OpenReadAsync() }); 
                }
            }
            UpdateQueueUI();
        }

        private async void SendNow_Click(object sender, RoutedEventArgs e)
        {
            if (_sendTarget == null)
            {
                StatusText.Text = "⚠ Select a target device first.";
                return;
            }
            if (SendQueue.Count == 0) return;

            var toSend = SendQueue.ToList();
            SendQueue.Clear();
            UpdateQueueUI();
            SendProgressBorder.IsVisible = true;

            foreach (var item in toSend)
            {
                SendProgressFile.Text = item.Name;
                SendProgressBar.Value = 0;
                using var stream = await item.OpenStream();
                await _transferManager.SendFileAsync(_sendTarget.IpAddress, _sendTarget.Port, item.Name, stream, item.Size);
            }

            SendProgressBorder.IsVisible = false;
            StatusText.Text = "Finished sending files";
            ShowToast($"Sent {toSend.Count} file(s)", "IconSend");
        }

        private async Task<System.Collections.Generic.List<QueueItem>> PickFilesAsync()
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return new();
            var result = await topLevel.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions { Title = "Select files to send", AllowMultiple = true });
            
            var list = new System.Collections.Generic.List<QueueItem>();
            foreach (var f in result)
            {
                var props = await f.GetBasicPropertiesAsync();
                list.Add(new QueueItem { Name = f.Name, Size = (long)(props.Size ?? 0), OpenStream = () => f.OpenReadAsync() });
            }
            return list;
        }

        private async void SendClipboard_Click(object sender, RoutedEventArgs e)
        {
            var cb = TopLevel.GetTopLevel(this)?.Clipboard;
            if (cb == null) return;
            var text = await cb.GetTextAsync();
            if (string.IsNullOrWhiteSpace(text))
            {
                ShowToast("Clipboard is empty!", "IconClose", "#EF4444");
                return;
            }

            try
            {
                string tempFile = Path.Combine(Path.GetTempPath(), "shared_clipboard.txt");
                await File.WriteAllTextAsync(tempFile, text);
                
                var info = new FileInfo(tempFile);
                if (!SendQueue.Any(q => q.Name == info.Name))
                    SendQueue.Add(new QueueItem { Name = info.Name, Size = info.Length, OpenStream = () => Task.FromResult<Stream>(new FileStream(tempFile, FileMode.Open, FileAccess.Read)) });
                UpdateQueueUI();
                
                HideAllPanels();
                RadarGrid.IsVisible = true;
                PageTitle.Text = "Select Receiver";
                StatusText.Text = "Clipboard text ready. Select a device to send.";
                ShowToast("Clipboard captured!", "IconClipboard");
            }
            catch (Exception ex)
            {
                ShowToast($"Error: {ex.Message}", "IconClose", "#EF4444");
            }
        }

        private void OpenGitHub_Click(object sender, RoutedEventArgs e)
        {
            _platformService.OpenUrl("https://github.com/sowmiyan-s/We-Share");
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

        private void LoadHistory()
        {
            var records = _dbHelper.GetAllTransfers();
            HistoryList.ItemsSource = records;
            HistEmptyState.IsVisible = records.Count == 0;
        }

        // ── Files Management ──────────────────────────────────────────────────
        private void LoadReceivedFiles()
        {
            try
            {
                ReceivedFiles.Clear();
                var history = _dbHelper.GetAllTransfers()
                    .Where(t => t.Direction == TransferDirection.Received && t.Status == TransferStatus.Done)
                    .OrderByDescending(t => t.Timestamp);
                
                foreach (var item in history)
                    ReceivedFiles.Add(item);

                FilesEmptyLabel.IsVisible = ReceivedFiles.Count == 0;
            }
            catch { }
        }

        private void OpenDownloadFolder_Click(object sender, RoutedEventArgs e)
        {
            _platformService.OpenUrl($"file://{_saveDirectory}");
        }

        private void OpenFileInList_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is FileTransferState state)
            {
                _platformService.OpenFile(state.FilePath);
            }
        }

        private void ShareFileInList_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is FileTransferState state)
            {
                _platformService.ShareFile(state.FilePath);
            }
        }

        private void DeleteFileInList_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is FileTransferState state)
            {
                try
                {
                    if (File.Exists(state.FilePath)) File.Delete(state.FilePath);
                    _dbHelper.DeleteTransfer(state.FileId);
                    LoadReceivedFiles();
                    ShowToast("File deleted", "IconClose", "#EF4444");
                }
                catch { }
            }
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            _dbHelper.ClearHistory();
            LoadHistory();
        }

        // ── Settings ──────────────────────────────────────────────────────────
        private void LoadAdapters() { }

        private void RefreshAdapters_Click(object sender, RoutedEventArgs e) => LoadAdapters();

        private async void UseAdapter_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is not NetworkAdapterInfo adapter) return;

            _selectedAdapterUrl = $"http://{adapter.IpAddress}:8080";
            if (this.FindControl<TextBlock>("ConnectUrl") is TextBlock cUrl) cUrl.Text = _selectedAdapterUrl;
            if (this.FindControl<TextBlock>("WebAccessUrl") is TextBlock wUrl) wUrl.Text = _selectedAdapterUrl;
            _webUrl             = _selectedAdapterUrl;

            UpdateQRCode(_selectedAdapterUrl);
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
            if (sender is Button btn)
            {
                btn.Content = "✓ Copied";
                await Task.Delay(2000);
                btn.Content = "📋 Copy URL";
            }
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
                // Strict Duplicate Fix: Use IP as the primary key for network identity
                var existing = Devices.FirstOrDefault(d => d.IpAddress == device.IpAddress);
                
                if (existing == null)
                {
                    Devices.Add(device);
                    StatusText.Text = "Connected to nearby devices";
                }
                else
                {
                    // Update existing device info silently
                    existing.Name = device.Name;
                    existing.Port = device.Port;
                    existing.Type = device.Type;
                    existing.LastSeen = DateTime.Now;
                }
                
                UpdateEmptyState();
            });
        }

        private void OnTransferStarted(FileTransferState state)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (state.Direction == TransferDirection.Received)
                {
                    ActiveReceives.Add(state);
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
                    SendProgressSpeed.Text = "Sending...";
                    StatusText.Text = "Sending files...";
                }
                else
                {
                    StatusText.Text = "Receiving files...";
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
                    var existing = ActiveReceives.FirstOrDefault(s => s.FileId == state.FileId);
                    if (existing != null) ActiveReceives.Remove(existing);
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
                    var existing = ActiveReceives.FirstOrDefault(s => s.FileId == state.FileId);
                    if (existing != null) ActiveReceives.Remove(existing);
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
