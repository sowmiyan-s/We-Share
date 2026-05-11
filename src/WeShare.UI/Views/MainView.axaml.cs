using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.Controls.Shapes;
using Avalonia.Styling;
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
        public string Path { get; set; } = "";
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

        private readonly MdnsService _mdns = new();
        private string _webUrl = "http://localhost:8080";
        private string _selectedAdapterUrl = "";
        private string? _lastUrlLog;
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
            _platformService = platformService ?? new Services.StubPlatformService(); // Fallback for designer
            _localDevice = new DeviceModel { Port = 45679, Name = Environment.MachineName, Type = _platformService.GetDeviceType() };

            // Bind list sources
            SendQueueList.ItemsSource  = SendQueue;
            IncomingList.ItemsSource   = ActiveReceives;

            Devices.CollectionChanged += (_, _) => UpdateEmptyState();
            SendQueue.CollectionChanged += (_, _) =>
                Dispatcher.UIThread.Post(UpdateQueueUI);
            ActiveReceives.CollectionChanged += (_, _) =>
                Dispatcher.UIThread.Post(() => RecvEmptyState.IsVisible = ActiveReceives.Count == 0);
            
            SendFilesPanel.AddHandler(DragDrop.DragOverEvent, OnDragOver);
            SendFilesPanel.AddHandler(DragDrop.DropEvent, OnDrop);
            
            this.AddHandler(DragDrop.DragEnterEvent, (s, e) => { if (e.Data.Contains(DataFormats.Files)) DragOverlay.IsVisible = true; });
            this.AddHandler(DragDrop.DragLeaveEvent, (s, e) => DragOverlay.IsVisible = false);
            this.AddHandler(DragDrop.DropEvent, (s, e) => { DragOverlay.IsVisible = false; OnDrop(s, e); });

            // Sync name boxes
            SidebarDeviceName.Text  = _localDevice.Name;
            SettingsDeviceName.Text = _localDevice.Name;
            SettingsSaveLocationLabel.Text  = _saveDirectory;

            // Discovery
            _discoveryService = new UdpDiscoveryService(_localDevice);
            _discoveryService.DeviceDiscovered += OnDeviceDiscovered;
            _discoveryService.StartListening();
            _mdns.Start("weshare-" + _localDevice.Name.ToLower().Replace(" ", "-"), 8080);

            // Bluetooth Discovery & Advertising
            _platformService.StartBluetoothAdvertising(_localDevice);
            _platformService.StartBluetoothDiscovery(OnDeviceDiscovered);

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
            _webDashboard.WebFileReceived += (peer, path, size) => 
            {
                var state = new FileTransferState
                {
                    FileId = Guid.NewGuid().ToString(),
                    FileName = System.IO.Path.GetFileName(path),
                    FilePath = path,
                    PeerName = peer,
                    TotalBytes = size,
                    TransferredBytes = size,
                    Status = TransferStatus.Done,
                    Direction = TransferDirection.Received,
                    Timestamp = DateTime.Now
                };
                Dispatcher.UIThread.Post(() => {
                    _dbHelper.SaveTransfer(state);
                    ReceivedFiles.Add(state);
                    ShowToast($"Received {state.FileName} from {peer}");
                });
            };
            _webDashboard.WebClientConnected += (name, ip) =>
            {
                Dispatcher.UIThread.Post(() => {
                    var existing = Devices.FirstOrDefault(d => d.IpAddress == ip);
                    if (existing == null)
                    {
                        Devices.Add(new DeviceModel { Name = name, IpAddress = ip, Type = "Mobile" });
                        ShowToast($"{name} joined via Web Portal");
                        _platformService.ShowSystemToast("Device Connected", $"{name} is ready at {_webUrl}", _webUrl);
                    }
                });
            };
            _webDashboard.Start();
            
            // Refresh IP periodically
            var ipTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
            ipTimer.Tick += (s, e) => UpdateWebUrl();
            ipTimer.Start();

            UpdateWebUrl();

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
                    // Also refresh BT advertising if IP changed
                    _platformService.StartBluetoothAdvertising(_localDevice); 
                    await Task.Delay(5000);
                }
            });

            _saveDirectory = _platformService.GetDefaultSavePath();
            SettingsSaveLocationLabel.Text = _saveDirectory;

            // Mobile adjustments for a "Perfect" stage
            if (_platformService.GetDeviceType() == "Phone")
            {
                Sidebar.IsVisible = false;
                BottomNav.IsVisible = true;
                MainLayout.ColumnDefinitions[0].Width = new GridLength(0);
                
                _localDevice.Type = "Phone";
                _localDevice.Name = "My Mobile Device";
                SidebarDeviceName.Text = _localDevice.Name;
                SettingsDeviceName.Text = _localDevice.Name;

                // Content area adjustments for touch & small screens
                ContentArea.Margin = new Thickness(0, 0, 0, 80); 
                PageTitle.FontSize = 22;
                PageTitle.Margin = new Thickness(20, 10, 20, 0);

                // Toast position for mobile (higher up)
                ToastBorder.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
                ToastBorder.Margin = new Thickness(20, 60, 20, 0);
                ToastBorder.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
            }


            // Heartbeat timer to remove stale devices
            DispatcherTimer.Run(() => {
                var stale = Devices.Where(d => (DateTime.Now - d.LastSeen).TotalSeconds > 30).ToList();
                foreach (var s in stale) Devices.Remove(s);
                if (stale.Count > 0) UpdateEmptyState();
                return true;
            }, TimeSpan.FromSeconds(5));

            UpdateEmptyState();
            NavHome_Click(this, new RoutedEventArgs());
        }

        // ── Empty state ───────────────────────────────────────────────────────
        private void UpdateEmptyState() 
        { 
            Dispatcher.UIThread.Post(() => {
                if (RadarEmptyHint != null) RadarEmptyHint.IsVisible = Devices.Count == 0;
            });
        }

        private void NavigateToPanel(Control panel, string title, Button? navBtn = null)
        {
            HomePanel.IsVisible          = false;
            SettingsPanel.IsVisible      = false;
            ReceivePanel.IsVisible       = false;
            ReceiveModePanel.IsVisible   = false;
            FilesPanel.IsVisible         = false;
            MobileConnectPanel.IsVisible = false;
            SendFilesPanel.IsVisible     = false;
            SendDiscoveryPanel.IsVisible = false;
            SendStepWizard.IsVisible     = false;

            panel.IsVisible = true;
            PageTitle.Text  = title;
            if (navBtn != null) SetActiveNav(navBtn);
        }

        private void NavHome_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPanel(HomePanel, "HOME", NavHomeBtn);
        }

        private void HomeSend_Click(object sender, RoutedEventArgs e)
        {
            NavSendFiles_Click(sender, e);
        }

        private void HomeReceive_Click(object sender, RoutedEventArgs e)
        {
            NavReceiveMode_Click(sender, e);
        }

        private void NavMobile_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPanel(MobileConnectPanel, "WEB SHARING", NavWebBtn);
            RefreshQrCode();
        }

        private async void StartHotspot_Click(object sender, RoutedEventArgs e)
        {
            string ssid = HotspotNameInput?.Text ?? ("WeShare-" + Environment.MachineName);
            string pass = "12345678"; // Simple password for now or generate one

            var result = await _platformService.StartHotspotAsync(ssid, pass);
            if (result.Success)
            {
                if (this.FindControl<Border>("HotspotInfoPanel") is Border infoPanel)
                   infoPanel.IsVisible = true;
                
                if (this.FindControl<TextBlock>("HotspotNameText") is TextBlock nameText)
                   nameText.Text = ssid;
                   
                if (this.FindControl<TextBlock>("HotspotPassText") is TextBlock passText)
                   passText.Text = pass;

                ShowToast("Hotspot Started successfully");
                RefreshQrCode();
            }
            else
            {
                ShowToast("Failed to start Hotspot: " + result.Message);
            }
        }


        private void NavSend_Click(object sender, RoutedEventArgs e) => NavSendFiles_Click(sender, e);

        private void NavSendFiles_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPanel(SendFilesPanel, "SEND FILES", null);
            SendStepWizard.IsVisible = true;
            Step1Indicator.Foreground = SolidColorBrush.Parse("#6366f1");
            Step2Indicator.Foreground = SolidColorBrush.Parse("#40FFFFFF");
            UpdateQueueUI();
        }

        private void NavSendDiscovery_Click(object sender, RoutedEventArgs e)
        {
            if (SendQueue.Count == 0)
            {
                ShowToast("Please add some files first");
                return;
            }
            NavigateToPanel(SendDiscoveryPanel, "SEND FILES", null);
            SendStepWizard.IsVisible = true;
            Step1Indicator.Foreground = SolidColorBrush.Parse("#40FFFFFF");
            Step2Indicator.Foreground = SolidColorBrush.Parse("#6366f1");
        }

        private void NavReceiveMode_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPanel(ReceiveModePanel, "RECEIVE FILE", null);
            _platformService.StartBluetoothAdvertising(_localDevice);
        }

        private void NavReceive_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPanel(ReceivePanel, "RECEIVING...", null);
        }

        private void NavFiles_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPanel(FilesPanel, "LIBRARY", NavFilesBtn);
        }

        private async void CopyUrl_Click(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.Clipboard != null)
            {
                await topLevel.Clipboard.SetTextAsync("http://weshare.local:8080");
                ShowToast("URL copied to clipboard");
            }
        }

        private void NavSettings_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPanel(SettingsPanel, "SETTINGS", NavSettBtn);
        }

        private void SetActiveNav(Button activeBtn)
        {
            var buttons = new[] { NavHomeBtn, NavFilesBtn, NavWebBtn, NavSettBtn };
            foreach (var btn in buttons)
                if (btn != null) btn.Classes.Set("Active", btn == activeBtn);
        }

        private void UpdateWebUrl()
        {
            var ip = UdpDiscoveryService.GetLocalIp();
            _webUrl = $"http://{ip}:8080";
            if (_lastUrlLog != _webUrl)
            {
                Console.WriteLine($"[WebDashboard] Detected Local URL: {_webUrl}");
                _lastUrlLog = _webUrl;
            }
            
            Dispatcher.UIThread.Post(() => {
                string mdnsName = "weshare-" + _localDevice.Name.ToLower().Replace(" ", "-") + ".local";
                ConnectUrlText.Text = $"http://{mdnsName}:8080";
                ConnectIpText.Text  = _webUrl;
                ToolTip.SetTip(ConnectUrlText, _webUrl);
                
                try 
                {
                    // Generate QR for direct IP as it is more robust across all mobile devices
                    var qrBytes = QrCodeService.GenerateQrCodePng(_webUrl);
                    using var ms = new MemoryStream(qrBytes);
                    BigQrCode.Source = new Bitmap(ms);
                }
                catch { }
            });
        }

        private void RefreshQrCode()
        {
            UpdateWebUrl();
        }

        private async void FixFirewall_Click(object sender, RoutedEventArgs e)
        {
            ShowToast("Requesting Administrator access to fix firewall...");
            string script = "netsh advfirewall firewall add rule name=\"WeShare Web Portal\" dir=in action=allow protocol=TCP localport=8080";
            var (success, msg) = await _platformService.RunElevatedAsync("cmd.exe", $"/c \"{script}\"");
            
            if (success)
                ShowToast("Firewall rule added successfully!");
            else
                ShowToast("Failed to add firewall rule. Please run as Admin.");
        }

        private void DeleteFileInList_Click(object sender, RoutedEventArgs e)
        {
            var file = (sender as Button)?.Tag as FileTransferState;
            if (file == null) return;
            
            try 
            { 
                if (System.IO.File.Exists(file.FilePath))
                    System.IO.File.Delete(file.FilePath);
                
                _dbHelper.DeleteTransfer(file.FileId);
                ReceivedFiles.Remove(file);
                ShowToast("File deleted");
            }
            catch (Exception ex) { ShowToast($"Error deleting: {ex.Message}"); }
        }

        private async void SendFile_Click(object sender, RoutedEventArgs e)
        {
            var device = (sender as Button)?.DataContext as DeviceModel;
            if (device == null || SendQueue.Count == 0) return;
            
            _sendTarget = device;
            ShowToast($"Sending to {device.Name}...");
            
            var toSend = SendQueue.ToList();
            SendQueue.Clear();
            UpdateQueueUI();
            
            NavigateToPanel(SendFilesPanel, "SENDING...");
            SendProgressBorder.IsVisible = true;
 
            foreach (var item in toSend)
            {
                SendProgressFile.Text = item.Name;
                SendProgressBar.Value = 0;
                using var stream = await item.OpenStream();
                await _transferManager.SendFileAsync(device.IpAddress, device.Port, item.Name, stream, item.Size);
            }
 
            SendProgressBorder.IsVisible = false;
            ShowToast($"Successfully sent {toSend.Count} file(s)");
            NavHome_Click(this, new RoutedEventArgs());
        }

        private async void SendToAll_Click(object sender, RoutedEventArgs e)
        {
            if (SendQueue.Count == 0)
            {
                ShowToast("Select some files first!");
                return;
            }
            if (Devices.Count == 0)
            {
                ShowToast("No devices found yet");
                return;
            }

            var toSend = SendQueue.ToList();
            SendQueue.Clear();
            UpdateQueueUI();
            
            ShowToast($"Broadcasting {toSend.Count} file(s) to {Devices.Count} devices...");
            
            foreach (var device in Devices)
            {
                _ = Task.Run(async () => {
                    foreach (var item in toSend)
                    {
                        using var stream = await item.OpenStream();
                        await _transferManager.SendFileAsync(device.IpAddress, device.Port, item.Name, stream, item.Size);
                    }
                });
            }
            
            NavHome_Click(this, new RoutedEventArgs());
        }

        private async void BrowseFiles_Click(object sender, RoutedEventArgs e) => await BrowseFilesInternalAsync();

        private async Task BrowseFilesInternalAsync()
        {
            var files = await PickFilesAsync();
            foreach (var f in files)
            {
                if (!SendQueue.Any(q => q.Name == f.Name)) 
                {
                    SendQueue.Add(f);
                    _webDashboard.ShareForWeb(f.Path);
                }
            }
            UpdateQueueUI();
        }

        private void OnDragOver(object? sender, DragEventArgs e)
        {
            if (e.Data.Contains(DataFormats.Files))
                e.DragEffects = DragDropEffects.Copy;
            else
                e.DragEffects = DragDropEffects.None;
        }

        private async void OnDrop(object? sender, DragEventArgs e)
        {
            var files = e.Data.GetFiles();
            if (files != null)
            {
                foreach (var f in files)
                {
                    var path = f.Path.LocalPath;
                    if (string.IsNullOrEmpty(path)) continue;
                    
                    if (File.Exists(path))
                    {
                        var info = new FileInfo(path);
                        if (!SendQueue.Any(q => q.Path == path))
                        {
                            SendQueue.Add(new QueueItem { 
                                Name = info.Name, 
                                Path = path, 
                                Size = info.Length, 
                                OpenStream = () => Task.FromResult<Stream>(File.OpenRead(path)) 
                            });
                            _webDashboard.ShareForWeb(path);
                        }
                    }
                }
                UpdateQueueUI();
                NavSendFiles_Click(this, new RoutedEventArgs());
            }
        }

        private void RemoveFromQueue_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is QueueItem item)
            {
                SendQueue.Remove(item);
                UpdateQueueUI();
                _webDashboard.ClearSharedFiles();
                foreach (var q in SendQueue) _webDashboard.ShareForWeb(q.Path);
            }
        }

        private void UpdateQueueUI()
        {
            QueueEmptyLabel.IsVisible = SendQueue.Count == 0;
            SendFooter.IsVisible = SendQueue.Count > 0;
            string targetName = _sendTarget != null ? _sendTarget.Name : "None selected";
            SendSummaryText.Text = $"{SendQueue.Count} file(s) | Target: {targetName}";
        }

        private async void SendNow_Click(object sender, RoutedEventArgs e)
        {
            if (SendQueue.Count == 0) return;
            if (_sendTarget == null)
            {
                ShowToast("Please select a device from step 1 first");
                return;
            }

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
            _sendTarget = null;
            UpdateQueueUI();
            ShowToast($"Successfully sent {toSend.Count} file(s)");
            NavHome_Click(this, new RoutedEventArgs());
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
                list.Add(new QueueItem { Name = f.Name, Path = f.Path.LocalPath ?? "", Size = (long)(props.Size ?? 0), OpenStream = () => f.OpenReadAsync() });
            }
            return list;
        }

        private async void ChangeSaveLocation_Click(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;
            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions { Title = "Choose save folder", AllowMultiple = false });
            if (folders.Count == 0) return;
            _saveDirectory = folders[0].Path.LocalPath;
            SettingsSaveLocationLabel.Text = _saveDirectory;
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

        private void LoadHistory() { }

        private void FileSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var query = FileSearchBox.Text?.ToLower() ?? "";
            var history = _dbHelper.GetAllTransfers()
                .Where(t => t.Direction == TransferDirection.Received && t.Status == TransferStatus.Done)
                .Where(t => t.FileName.ToLower().Contains(query))
                .OrderByDescending(t => t.Timestamp);
            
            ReceivedFiles.Clear();
            foreach (var item in history) ReceivedFiles.Add(item);
        }

        private void LoadReceivedFiles()
        {
            ReceivedFiles.Clear();
            var history = _dbHelper.GetAllTransfers()
                .Where(t => t.Direction == TransferDirection.Received && t.Status == TransferStatus.Done)
                .OrderByDescending(t => t.Timestamp);
            foreach (var item in history) ReceivedFiles.Add(item);
        }

        private void OpenDownloadFolder_Click(object sender, RoutedEventArgs e) => _platformService.OpenUrl($"file://{_saveDirectory}");
        private void OpenFileInList_Click(object sender, RoutedEventArgs e) { if ((sender as Button)?.Tag is FileTransferState s) _platformService.OpenFile(s.FilePath); }

        private void ClearAllHistory_Click(object sender, RoutedEventArgs e)
        {
            _dbHelper.ClearHistory();
            ReceivedFiles.Clear();
            ShowToast("Transfer history cleared");
        }

        private void UpdateStatusText()
        {
            // Removed from UI
        }

        private TaskCompletionSource<bool>? _acceptTcs;
        private async Task<bool> OnTransferRequested(FileTransferState state)
        {
            _acceptTcs = new TaskCompletionSource<bool>();
            Dispatcher.UIThread.Post(() => {
                AcceptRejectPanel.IsVisible = true;
                IncomingFileName.Text = state.FileName;
                IncomingPeerName.Text = $"FROM: {state.PeerName}";
                IncomingFileSize.Text = FileTransferState.FormatBytes(state.TotalBytes);
            });
            return await _acceptTcs.Task;
        }

        private void AcceptTransfer_Click(object sender, RoutedEventArgs e)
        {
            AcceptRejectPanel.IsVisible = false;
            NavReceive_Click(this, new RoutedEventArgs());
            _acceptTcs?.TrySetResult(true);
        }

        private void RejectTransfer_Click(object sender, RoutedEventArgs e)
        {
            AcceptRejectPanel.IsVisible = false;
            NavHome_Click(this, new RoutedEventArgs());
            _acceptTcs?.TrySetResult(false);
        }

        private void OnDeviceDiscovered(DeviceModel device)
        {
            Dispatcher.UIThread.Post(() => {
                var existing = Devices.FirstOrDefault(d => d.IpAddress == device.IpAddress);
                if (existing == null) Devices.Add(device);
                else { existing.LastSeen = DateTime.Now; }
            });
        }

        private void OnTransferStarted(FileTransferState state)
        {
            Dispatcher.UIThread.Post(() => {
                if (state.Direction == TransferDirection.Received) { ActiveReceives.Add(state); RecvEmptyState.IsVisible = false; }
                _dbHelper.SaveTransfer(state);
            });
        }

        private void OnTransferProgress(FileTransferState state)
        {
            Dispatcher.UIThread.Post(() => {
                if (state.Direction == TransferDirection.Sent) 
                { 
                    SendProgressBar.Value = state.ProgressPercentage; 
                    SendProgressPct.Text = $"{state.ProgressPercentage:F0}%"; 
                }
                
                GlobalActivityBorder.IsVisible = true;
                GlobalProgressBar.Value = state.ProgressPercentage;
            });
        }

        private void OnTransferCompleted(FileTransferState state)
        {
            Dispatcher.UIThread.Post(() => {
                SendProgressBorder.IsVisible = false;
                GlobalActivityBorder.IsVisible = false;
                if (state.Direction == TransferDirection.Received) { var ex = ActiveReceives.FirstOrDefault(s => s.FileId == state.FileId); if (ex != null) ActiveReceives.Remove(ex); }
            });
        }

        private void OnTransferFailed(FileTransferState state) => ShowToast($"Transfer failed");

        public void Shutdown()
        {
            _discoveryService?.StopListening();
            _transferManager?.StopListening();
            _webDashboard?.Stop();
        }

        private void ShowToast(string message)
        {
            Dispatcher.UIThread.Post(async () => {
                ToastMessage.Text = message;
                ToastBorder.IsVisible = true;
                await Task.Delay(3000);
                ToastBorder.IsVisible = false;
            });
        }
    }
}
