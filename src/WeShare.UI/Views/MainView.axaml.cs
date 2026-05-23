using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.Styling;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
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
        public Avalonia.Media.Imaging.Bitmap? Thumbnail { get; set; }
    }

    public partial class MainView : UserControl
    {
        // ── Services ──────────────────────────────────────────────────────────
        private DeviceModel _localDevice;
        private UdpDiscoveryService _discoveryService;
        private TcpTransferManager _transferManager;
        private DatabaseHelper _dbHelper;
        private IPlatformService _platformService;
        private WebDashboardService? _webDashboardService;
        private HotspotService? _hotspotService;
        private WifiConnectorService? _wifiConnector;

        private string _saveDirectory;
        private DeviceModel? _sendTarget;

        // Observable collections
        public ObservableCollection<DeviceModel> Devices { get; } = new();
        public ObservableCollection<QueueItem> SendQueue { get; } = new();
        public ObservableCollection<FileTransferState> ActiveReceives { get; } = new();
        public ObservableCollection<FileTransferState> ReceivedFiles { get; } = new();
        
        // Concurrency and Session Management
        private readonly System.Threading.SemaphoreSlim _uiRequestLock = new(1, 1);
        private string? _lastAcceptedIp;
        private DateTime _lastAcceptedTime;


        public MainView() : this(App.PlatformService) { }

        public MainView(IPlatformService? platformService)
        {
            InitializeComponent();

            _saveDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            _dbHelper = new DatabaseHelper();
            _platformService = platformService ?? new Services.StubPlatformService();
            _localDevice = new DeviceModel { Port = 45679, Name = Environment.MachineName, Type = _platformService.GetDeviceType() };

            // Bind list sources
            SendQueueList.ItemsSource = SendQueue;
            IncomingList.ItemsSource  = ActiveReceives;

            Devices.CollectionChanged += (_, _) => UpdateEmptyState();
            SendQueue.CollectionChanged += (_, _) =>
                Dispatcher.UIThread.Post(UpdateQueueUI);
            ActiveReceives.CollectionChanged += (_, _) =>
                Dispatcher.UIThread.Post(() => RecvEmptyState.IsVisible = ActiveReceives.Count == 0);

            SendFilesPanel.AddHandler(DragDrop.DragOverEvent, OnDragOver);
            SendFilesPanel.AddHandler(DragDrop.DropEvent, OnDrop);

            this.AddHandler(DragDrop.DragEnterEvent, (s, e) => { if (e.Data.Contains(DataFormats.Files)) DragOverlay.IsVisible = true; });
            this.AddHandler(DragDrop.DragLeaveEvent, (s, e) => DragOverlay.IsVisible = false);
            this.AddHandler(DragDrop.DropEvent,      (s, e) => { DragOverlay.IsVisible = false; OnDrop(s, e); });

            // Sync name boxes
            SidebarDeviceName.Text  = _localDevice.Name;
            HomeDeviceNameText.Text = _localDevice.Name;
            SettingsDeviceName.Text = _localDevice.Name;
            SettingsSaveLocationLabel.Text = _saveDirectory;

            // Discovery – listen for other devices broadcasting
            try
            {
                _discoveryService = new UdpDiscoveryService(_localDevice);
                _discoveryService.DeviceDiscovered += OnDeviceDiscovered;
                _discoveryService.StartListening();
            }
            catch (Exception ex)
            {
                _discoveryService = new UdpDiscoveryService(_localDevice); // Ensure it's not null
                ShowToast($"Discovery failed: {ex.Message}");
            }

            _saveDirectory = _platformService.GetDefaultSavePath();
            SettingsSaveLocationLabel.Text = _saveDirectory;
            
            // Transfer – listen for incoming file sends
            try
            {
                _transferManager = new TcpTransferManager(45679);
                ConfigureTransferManager(_transferManager);
                _transferManager.StartListening(_saveDirectory);
                _localDevice.Port = _transferManager.BoundPort;
            }
            catch (Exception ex)
            {
                _transferManager = new TcpTransferManager(0); // Use random port if default fails
                ConfigureTransferManager(_transferManager);
                _transferManager.StartListening(_saveDirectory);
                _localDevice.Port = _transferManager.BoundPort;
                ShowToast($"Transfer service error: {ex.Message}");
            }

            // Web Dashboard – start port 8080 web server
            try
            {
                _webDashboardService = new WebDashboardService(_saveDirectory, _localDevice);
                _webDashboardService.SetPeersProvider(() => Devices.ToList());
                _webDashboardService.WebFileReceived += OnWebFileReceived;
                _webDashboardService.WebClientConnected += OnWebClientConnected;
                _webDashboardService.Start();
            }
            catch (Exception ex)
            {
                ShowToast($"Web Portal start failed: {ex.Message}");
            }

            // Synchronize the SendQueue with the Web Portal shared files
            SendQueue.CollectionChanged += (s, e) => {
                if (_webDashboardService != null)
                {
                    _webDashboardService.ClearSharedFiles();
                    foreach (var item in SendQueue)
                    {
                        if (!string.IsNullOrEmpty(item.Path))
                        {
                            _webDashboardService.ShareForWeb(item.Path);
                        }
                    }
                }
            };

            // Broadcast our presence so receivers can see us
            _ = Task.Run(async () =>
            {
                // Initial burst for quick discovery
                for (int i = 0; i < 3; i++)
                {
                    await _discoveryService.BroadcastPresenceAsync();
                    await Task.Delay(1000);
                }

                while (true)
                {
                    await _discoveryService.BroadcastPresenceAsync();
                    await Task.Delay(5000);
                }
            });

            // Mobile layout adjustments
            if (_platformService.GetDeviceType() == "Phone")
            {
                Sidebar.IsVisible = false;
                BottomNav.IsVisible = true;
                MainLayout.ColumnDefinitions[0].Width = new GridLength(0);

                _localDevice.Type = "Phone";
                _localDevice.Name = "My Mobile Device";
                SidebarDeviceName.Text  = _localDevice.Name;
                HomeDeviceNameText.Text = _localDevice.Name;
                SettingsDeviceName.Text = _localDevice.Name;

                ContentArea.Margin = new Thickness(0, 0, 0, 80);
                PageTitle.FontSize = 22;
                PageTitle.Margin   = new Thickness(20, 10, 20, 0);

                ToastBorder.VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Top;
                ToastBorder.Margin              = new Thickness(20, 60, 20, 0);
                ToastBorder.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
            }

            // Hide splash after 3 s
            DispatcherTimer.RunOnce(() =>
            {
                if (this.FindControl<Grid>("SplashGrid") is Grid splash)
                    splash.IsVisible = false;
            }, TimeSpan.FromSeconds(3));

            // Heartbeat – remove stale devices
            DispatcherTimer.Run(() => {
                var stale = Devices.Where(d => (DateTime.Now - d.LastSeen).TotalSeconds > 30).ToList();
                foreach (var s in stale) Devices.Remove(s);
                if (stale.Count > 0) UpdateEmptyState();
                return true;
            }, TimeSpan.FromSeconds(5));

            // Load received history
            LoadReceivedFiles();

            UpdateEmptyState();
            NavHome_Click(this, new RoutedEventArgs());

            // Network check — auto-start hotspot or auto-join if no network
            var ip = UdpDiscoveryService.GetLocalIp();
            SidebarNetworkInfo.Text  = $"{ip}:{_localDevice.Port}";
            HomeNetworkInfoText.Text = SidebarNetworkInfo.Text;
            HomeWebPortalText.Text   = $"http://{ip}:8080";

            if (ip == "127.0.0.1")
            {
                // No network detected — run auto-network logic in background
                _ = Task.Run(TryAutoNetworkAsync);
            }
        }

        private void TitleBar_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (this.VisualRoot is Window window)
            {
                window.BeginMoveDrag(e);
            }
        }

        // ── Empty state ───────────────────────────────────────────────────────
        private void UpdateEmptyState()
        {
            // Empty state handled visually or not needed
        }

        private void ShowPanel(Control panel, string title, Button? navBtn = null)
        {
            HomePanel.IsVisible = false;
            SettingsPanel.IsVisible = false;
            AboutPanel.IsVisible = false;
            ReceiveModePanel.IsVisible = false;
            FilesPanel.IsVisible = false;
            SendFilesPanel.IsVisible = false;
            SendDiscoveryPanel.IsVisible = false;
            TransfersPanel.IsVisible = false;
            SendStepWizard.IsVisible = false;

            bool wasReceiver = _localDevice.IsReceiver;
            _localDevice.IsReceiver = (panel == ReceiveModePanel);

            panel.IsVisible = true;
            PageTitle.Text = title;
            SetActiveNav(navBtn);

            if (wasReceiver != _localDevice.IsReceiver && _discoveryService != null)
            {
                _ = _discoveryService.BroadcastPresenceAsync();
            }
        }

        private void NavHome_Click(object? sender, RoutedEventArgs e) => ShowPanel(HomePanel, "HOME", NavHomeBtn);
        private void NavFiles_Click(object? sender, RoutedEventArgs e) => ShowPanel(FilesPanel, "LIBRARY", NavFilesBtn);
        private void NavTransfers_Click(object? sender, RoutedEventArgs e) => ShowPanel(TransfersPanel, "TRANSFERS", NavTransfersBtn);
        private void NavSettings_Click(object? sender, RoutedEventArgs e) => ShowPanel(SettingsPanel, "SETTINGS", NavSettBtn);
        private void NavAbout_Click(object? sender, RoutedEventArgs e) => ShowPanel(AboutPanel, "ABOUT", NavAboutBtn);

        private void HomeSend_Click(object sender, RoutedEventArgs e)
        {
            SendStepWizard.IsVisible = true;
            ShowPanel(SendFilesPanel, "SEND FILES", NavHomeBtn);
        }

        private void HomeReceive_Click(object sender, RoutedEventArgs e) => NavTransfers_Click(sender, e);

        private void NavSendFiles_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel(SendFilesPanel, "SEND FILES", null);
            SendStepWizard.IsVisible = true;
            Step1Indicator.Foreground = SolidColorBrush.Parse("#6366F1");
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
            ShowPanel(SendDiscoveryPanel, "SEND FILES", null);
            SendStepWizard.IsVisible = true;
            Step1Indicator.Foreground = SolidColorBrush.Parse("#40FFFFFF");
            Step2Indicator.Foreground = SolidColorBrush.Parse("#6366F1");
        }

        private void NavReceiveMode_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel(ReceiveModePanel, "RECEIVE FILE", null);
            ShowToast("Visible to senders on your network");
        }

        private void CancelSending_Click(object sender, RoutedEventArgs e)
        {
            SendQueue.Clear();
            _sendTarget = null;
            _isSending = false;
            NavHome_Click(sender, e);
        }


        private void OpenGitHub_Click(object sender, RoutedEventArgs e)
            => _platformService.OpenUrl("https://github.com/sowmiyan-s/We-Share");

        private void SetActiveNav(Button? activeBtn)
        {
            var buttons = new[] { NavHomeBtn, NavFilesBtn, NavTransfersBtn, NavSettBtn, NavAboutBtn };
            foreach (var btn in buttons)
                if (btn != null) btn.Classes.Set("Active", btn == activeBtn);
        }

        private async void RefreshHistory()
        {
            var history = await _dbHelper.GetAllTransfersAsync();
            var query = FileSearchBox.Text?.ToLower() ?? "";
            
            ReceivedFiles.Clear();
            foreach (var h in history.Where(t => t.Direction == TransferDirection.Received && t.Status == TransferStatus.Done))
            {
                if (string.IsNullOrEmpty(query) || h.FileName.ToLower().Contains(query))
                    ReceivedFiles.Add(h);
            }
            HistoryEmptyState.IsVisible = ReceivedFiles.Count == 0;
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
                if (System.IO.File.Exists(file.FilePath))
                    System.IO.File.Delete(file.FilePath);

                await _dbHelper.DeleteTransferAsync(file.FileId);
                ReceivedFiles.Remove(file);
                HistoryEmptyState.IsVisible = ReceivedFiles.Count == 0;
                ShowToast("File deleted");
            }
            catch (Exception ex) { ShowToast($"Error deleting: {ex.Message}"); }
        }



        private bool _isSending = false;
        private void SendFile_Click(object sender, RoutedEventArgs e)
        {
            var device = (sender as Button)?.DataContext as DeviceModel;
            if (device == null) return;

            // If we're already sending to this device, just add to the queue
            if (_isSending && _sendTarget?.IpAddress == device.IpAddress)
            {
                ShowPanel(SendFilesPanel, "SEND FILES", null);
                return;
            }

            _sendTarget = device;
            ShowToast($"Connecting to {device.Name}...");

            ShowPanel(TransfersPanel, "TRANSFERS", NavTransfersBtn);
            SendProgressBorder.IsVisible = true;

            if (!_isSending)
            {
                _isSending = true;
                _ = Task.Run(() => ProcessSendQueueAsync(device));
            }
        }

        private async Task ProcessSendQueueAsync(DeviceModel device)
        {
            try
            {
                while (true)
                {
                    // Dequeue the next item on the UI thread (ObservableCollection is not thread-safe)
                    QueueItem? item = null;
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (SendQueue.Count > 0)
                        {
                            item = SendQueue[0];
                            SendQueue.RemoveAt(0);
                        }
                    });

                    if (item == null)
                    {
                        // Queue empty — wait briefly then check once more before exiting
                        await Task.Delay(1000);
                        bool hasMore = false;
                        await Dispatcher.UIThread.InvokeAsync(() => hasMore = SendQueue.Count > 0);
                        if (!hasMore)
                        {
                            _isSending = false;
                            break;
                        }
                        continue;
                    }

                    Dispatcher.UIThread.Post(() =>
                    {
                        SendProgressFile.Text = item.Name;
                        SendProgressBar.Value = 0;
                        UpdateQueueUI();
                    });

                    using var stream = await item.OpenStream();
                    await _transferManager.SendFileAsync(device.IpAddress, device.Port, item.Name, stream, item.Size);
                }

                ShowToast("All transfers completed");
            }
            catch (Exception ex)
            {
                ShowToast($"Transfer error: {ex.Message}");
                _isSending = false;
            }
            finally
            {
                Dispatcher.UIThread.Post(() =>
                {
                    SendProgressBorder.IsVisible = false;
                    NavHome_Click(this, new RoutedEventArgs());
                });
            }
        }

        private async void BrowseFiles_Click(object sender, RoutedEventArgs e) => await BrowseFilesInternalAsync();

        private async Task BrowseFilesInternalAsync()
        {
            var files = await PickFilesAsync();
            foreach (var f in files)
            {
                if (!SendQueue.Any(q => q.Name == f.Name))
                    SendQueue.Add(f);
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

        private void OnDrop(object? sender, DragEventArgs e)
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
                            SendQueue.Add(new QueueItem {
                                Name = info.Name,
                                Path = path,
                                Size = info.Length,
                                OpenStream = () => Task.FromResult<Stream>(File.OpenRead(path)),
                                Thumbnail = LoadThumbnail(path)
                            });
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
            }
        }

        private void UpdateQueueUI()
        {
            QueueEmptyLabel.IsVisible = SendQueue.Count == 0;
            SendFooter.IsVisible      = SendQueue.Count > 0;
            string targetName = _sendTarget != null ? _sendTarget.Name : "None selected";
            SendSummaryText.Text = $"{SendQueue.Count} file(s) | Target: {targetName}";
        }

        private async void SendNow_Click(object sender, RoutedEventArgs e)
        {
            if (SendQueue.Count == 0) return;
            if (_sendTarget == null)
            {
                ShowToast("Please select a receiver from the radar first");
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
                list.Add(new QueueItem { 
                    Name = f.Name, 
                    Path = f.Path.LocalPath ?? "", 
                    Size = (long)(props.Size ?? 0), 
                    OpenStream = () => f.OpenReadAsync(),
                    Thumbnail = LoadThumbnail(f.Path.LocalPath)
                });
            }
            return list;
        }

        private Avalonia.Media.Imaging.Bitmap? LoadThumbnail(string? path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            try
            {
                var ext = System.IO.Path.GetExtension(path).ToLower();
                if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".webp")
                {
                    using var stream = System.IO.File.OpenRead(path);
                    return Avalonia.Media.Imaging.Bitmap.DecodeToWidth(stream, 80);
                }
            }
            catch { }
            return null;
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

        private void FileSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshHistory();
        }

        private void LoadReceivedFiles()
        {
            RefreshHistory();
        }

        private void OpenDownloadFolder_Click(object sender, RoutedEventArgs e) => _platformService.OpenUrl($"file://{_saveDirectory}");
        private void OpenFileInList_Click(object sender, RoutedEventArgs e) { if ((sender as Button)?.Tag is FileTransferState s) _platformService.OpenFile(s.FilePath); }

        private async void ClearAllHistory_Click(object sender, RoutedEventArgs e)
        {
            await _dbHelper.ClearHistoryAsync();
            ReceivedFiles.Clear();
            HistoryEmptyState.IsVisible = true;
            HomeEmptyHistoryLabel.IsVisible = true;
            ShowToast("Transfer history cleared");
        }

        // ── Incoming request ─────────────────────────────────────────────────
        private async void RefreshDiscovery_Click(object sender, RoutedEventArgs e)
        {
            ShowToast("Refreshing radar...");
            Devices.Clear();
            await _discoveryService.BroadcastPresenceAsync();
            UpdateNetworkLabels();
        }

        // ── Auto-Network (Desert Mode) ─────────────────────────────────────────
        private async Task TryAutoNetworkAsync()
        {
            try
            {
                _wifiConnector = new WifiConnectorService();

                // Step 1 — Try to JOIN an existing WeShare hotspot (client role)
                ShowToast("No network — scanning for WeShare hotspot...");
                bool found = await _wifiConnector.IsWeShareHotspotVisibleAsync();

                if (found)
                {
                    ShowToast("WeShare hotspot found! Connecting automatically...");
                    var (ok, _) = await _wifiConnector.AutoConnectToWeShareAsync();
                    if (ok)
                    {
                        await Task.Delay(1500); // let DHCP settle
                        UpdateNetworkLabels();
                        ShowToast("Connected! Scanning for devices...");

                        // Burst-broadcast so the host sees us immediately
                        for (int i = 0; i < 4; i++)
                        {
                            await _discoveryService.BroadcastPresenceAsync();
                            await Task.Delay(800);
                        }
                        return;
                    }
                }

                // Step 2 — No existing hotspot found — become the HOST
                _hotspotService = new HotspotService();
                if (!await _hotspotService.IsSupportedAsync())
                {
                    ShowToast("No network. Use \"CONNECT VIA IP\" to connect manually.");
                    return;
                }

                ShowToast("Starting WeShare hotspot...");
                var (started, _) = await _hotspotService.StartAsync();
                if (started)
                {
                    await Task.Delay(1000);
                    UpdateNetworkLabels();
                    ShowToast($"Hotspot active — other PC will auto-connect to \"{HotspotService.TargetSsid}\"");
                }
            }
            catch (Exception ex)
            {
                ShowToast($"Auto-network error: {ex.Message}");
            }
        }

        /// <summary>Updates the existing network info labels in-place (no new UI elements).</summary>
        private void UpdateNetworkLabels()
        {
            Dispatcher.UIThread.Post(() =>
            {
                var ip = UdpDiscoveryService.GetLocalIp();
                bool hasRealIp = ip != "127.0.0.1";

                string info = hasRealIp
                    ? $"{ip}:{_localDevice.Port}"
                    : $"📶 {HotspotService.TargetSsid} / {HotspotService.TargetPassword}";

                SidebarNetworkInfo.Text  = info;
                HomeNetworkInfoText.Text = info;
                HomeWebPortalText.Text   = hasRealIp ? $"http://{ip}:8080" : "http://192.168.137.1:8080";
            });
        }

        private void ManualConnect_Click(object sender, RoutedEventArgs e)
        {
            ManualIPDialog.IsVisible = true;
            ManualIPInput.Focus();
        }

        private void CloseManualIP_Click(object sender, RoutedEventArgs e) => ManualIPDialog.IsVisible = false;

        private void ManualIPConnect_Click(object sender, RoutedEventArgs e)
        {
            string ip = ManualIPInput.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(ip)) return;

            ManualIPDialog.IsVisible = false;
            
            // Add a virtual device for this IP
            var device = new DeviceModel { Name = $"Manual Peer ({ip})", IpAddress = ip, Port = 45679 };
            if (!Devices.Any(d => d.IpAddress == ip)) Devices.Add(device);

            _sendTarget = device;
            ShowToast($"Connecting to {ip}:45679...");
            ShowPanel(SendFilesPanel, "SEND FILES", null);
        }

        private TaskCompletionSource<bool>? _acceptTcs;
        private async Task<bool> OnTransferRequested(FileTransferState state)
        {
            // 1. Auto-Accept Logic (Session)
            if (_lastAcceptedIp == state.RemoteIp && (DateTime.Now - _lastAcceptedTime).TotalSeconds < 60)
            {
                return true;
            }

            // 2. UI Request Queueing
            await _uiRequestLock.WaitAsync();
            try
            {
                _acceptTcs = new TaskCompletionSource<bool>();
                Dispatcher.UIThread.Post(() => {
                    AcceptRejectPanel.IsVisible = true;
                    IncomingFileName.Text = state.FileName;
                    IncomingPeerName.Text = $"FROM: {state.PeerName}";
                    IncomingFileSize.Text = FileTransferState.FormatBytes(state.TotalBytes);
                });
                
                bool accepted = await _acceptTcs.Task;
                if (accepted)
                {
                    _lastAcceptedIp = state.RemoteIp;
                    _lastAcceptedTime = DateTime.Now;
                }
                return accepted;
            }
            finally
            {
                _uiRequestLock.Release();
            }
        }

        private void AcceptTransfer_Click(object sender, RoutedEventArgs e)
        {
            AcceptRejectPanel.IsVisible = false;
            NavTransfers_Click(this, new RoutedEventArgs());
            _acceptTcs?.TrySetResult(true);
        }

        private void RejectTransfer_Click(object sender, RoutedEventArgs e)
        {
            AcceptRejectPanel.IsVisible = false;
            NavHome_Click(this, new RoutedEventArgs());
            _acceptTcs?.TrySetResult(false);
        }

        // ── Discovery callbacks ───────────────────────────────────────────────
        private void OnDeviceDiscovered(DeviceModel device)
        {
            Dispatcher.UIThread.Post(() => {
                var existing = Devices.FirstOrDefault(d => d.Id == device.Id);

                if (!device.IsReceiver)
                {
                    if (existing != null)
                        Devices.Remove(existing);
                    return;
                }

                // Ensure we don't show the same device multiple times (match by unique ID)
                if (existing == null) 
                {
                    Devices.Add(device);
                }
                else 
                {
                    // Update IP if it changed, and refresh last seen
                    existing.IpAddress = device.IpAddress;
                    existing.LastSeen = DateTime.Now;
                }
            });
        }

        private void SettingsDeviceName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SettingsDeviceName != null && !string.IsNullOrEmpty(SettingsDeviceName.Text))
            {
                _localDevice.Name = SettingsDeviceName.Text;
                SidebarDeviceName.Text = _localDevice.Name;
                HomeDeviceNameText.Text = _localDevice.Name;
            }
        }

        // ── Transfer callbacks ────────────────────────────────────────────────
        private void OnTransferStarted(FileTransferState state)
        {
            Dispatcher.UIThread.Post(async () => {
                if (state.Direction == TransferDirection.Received) { ActiveReceives.Add(state); RecvEmptyState.IsVisible = false; }
                else { SendProgressBorder.IsVisible = true; }
                
                // Switch to Transfers view automatically when any transfer starts
                NavTransfers_Click(this, new RoutedEventArgs());
                
                await _dbHelper.SaveTransferAsync(state);
            });
        }

        private void OnTransferProgress(FileTransferState state)
        {
            Dispatcher.UIThread.Post(() => {
                if (state.Direction == TransferDirection.Sent)
                {
                    SendProgressBar.Value  = state.ProgressPercentage;
                    SendProgressPct.Text   = $"{state.ProgressPercentage:F0}%";
                    SendProgressSpeed.Text = $"{state.SpeedMbPerSec:F2} MB/s | ETA: {state.ETA:mm\\:ss}";
                }

                GlobalActivityBorder.IsVisible = true;
                GlobalProgressBar.Value        = state.ProgressPercentage;
            });
        }

        private void OnTransferCompleted(FileTransferState state)
        {
            Dispatcher.UIThread.Post(async () => {
                SendProgressBorder.IsVisible   = false;
                GlobalActivityBorder.IsVisible = false;
                if (state.Direction == TransferDirection.Received)
                {
                    var ex = ActiveReceives.FirstOrDefault(s => s.FileId == state.FileId);
                    if (ex != null) ActiveReceives.Remove(ex);
                    await _dbHelper.SaveTransferAsync(state);
                    ReceivedFiles.Insert(0, state);
                    ShowToast($"Received: {state.FileName}");
                }
                else
                {
                    await _dbHelper.SaveTransferAsync(state);
                }
            });
        }

        private void OnTransferFailed(FileTransferState state) 
        {
            Dispatcher.UIThread.Post(async () => {
                SendProgressBorder.IsVisible   = false;
                GlobalActivityBorder.IsVisible = false;
                if (state.Direction == TransferDirection.Received)
                {
                    var ex = ActiveReceives.FirstOrDefault(s => s.FileId == state.FileId);
                    if (ex != null) ActiveReceives.Remove(ex);
                }
                await _dbHelper.SaveTransferAsync(state);
                string reason = !string.IsNullOrEmpty(state.ErrorMessage) ? state.ErrorMessage : "Connection failed or rejected";
                ShowToast($"Transfer failed: {reason}");
            });
        }

        private void ConfigureTransferManager(TcpTransferManager manager)
        {
            manager.LocalName = _localDevice.Name;
            manager.TransferStarted   += OnTransferStarted;
            manager.TransferProgress  += OnTransferProgress;
            manager.TransferCompleted += OnTransferCompleted;
            manager.TransferFailed    += OnTransferFailed;
            manager.TransferRequestCallback = OnTransferRequested;
        }

        private void OnWebFileReceived(string peerName, string filePath, long size)
        {
            var state = new FileTransferState
            {
                FileName = Path.GetFileName(filePath),
                FilePath = filePath,
                TotalBytes = size,
                TransferredBytes = size,
                Status = TransferStatus.Done,
                Direction = TransferDirection.Received,
                PeerName = peerName,
                Timestamp = DateTime.UtcNow
            };
            Dispatcher.UIThread.Post(async () => {
                await _dbHelper.SaveTransferAsync(state);
                ReceivedFiles.Insert(0, state);
                ShowToast($"Received via Web Portal: {state.FileName}");
                UpdateEmptyState();
            });
        }

        private void OnWebClientConnected(string type, string remoteIp)
        {
            Dispatcher.UIThread.Post(() => {
                ShowToast($"Web client connected from {remoteIp}");
            });
        }

        public void Shutdown()
        {
            _discoveryService?.StopListening();
            _transferManager?.StopListening();
            _webDashboardService?.Stop();

            // Stop the hotspot and wait for it — this ensures the Desert Mode
            // hostednetwork is shut down and the user's original Wi-Fi is restored
            // before the process exits.
            if (_hotspotService != null)
            {
                try { _hotspotService.StopAsync().GetAwaiter().GetResult(); }
                catch { }
            }

            // Remove the temporary WeShare Wi-Fi profile and reconnect to the
            // original network the user was on before joining the hotspot.
            _wifiConnector?.Cleanup();
            _wifiConnector?.Dispose();
        }

        // CTS to cancel the previous toast's hide-delay when a new toast fires
        private CancellationTokenSource? _toastCts;

        private void ShowToast(string message)
        {
            Dispatcher.UIThread.Post(async () =>
            {
                // Cancel any currently-visible toast so it doesn't hide the new one
                _toastCts?.Cancel();
                _toastCts = new CancellationTokenSource();
                var token = _toastCts.Token;

                ToastMessage.Text     = message;
                ToastBorder.IsVisible = true;
                try
                {
                    await Task.Delay(3000, token);
                    ToastBorder.IsVisible = false;
                }
                catch (TaskCanceledException) { /* a newer toast took over — do nothing */ }
            });
        }
    }
}
