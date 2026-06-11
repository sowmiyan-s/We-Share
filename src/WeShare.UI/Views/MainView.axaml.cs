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
        private CaptivePortalService? _captivePortalService;

        private string _saveDirectory;
        private DeviceModel? _sendTarget;
        private Avalonia.Media.Imaging.Bitmap? _qrBitmap;

        // Observable collections
        public ObservableCollection<DeviceModel> Devices { get; } = new();
        public ObservableCollection<QueueItem> SendQueue { get; } = new();
        public ObservableCollection<FileTransferState> ActiveReceives { get; } = new();
        public ObservableCollection<FileTransferState> ReceivedFiles { get; } = new();
        public ObservableCollection<FileTransferState> LibraryFiles { get; } = new();
        public ObservableCollection<StagedWebFile> StagedWebFiles { get; } = new();
        
        // Concurrency and Session Management
        private readonly System.Threading.SemaphoreSlim _uiRequestLock = new(1, 1);
        private string? _lastAcceptedIp;
        private DateTime _lastAcceptedTime;
        private bool _isUpdatingLibrary = false;
        private bool _isLibraryUpdatePending = false;
        private string? _currentSendingFileId;
        private string _currentDateFilter = "All";


        public MainView() : this(App.PlatformService) { }

        public MainView(IPlatformService? platformService)
        {
            InitializeComponent();
            CleanTempZipDirectory();

            _saveDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            _dbHelper = new DatabaseHelper();
            _platformService = platformService ?? new Services.StubPlatformService();
            _localDevice = new DeviceModel { Port = 45679, Name = Environment.MachineName, Type = _platformService.GetDeviceType() };

            // Bind list sources
            SendQueueList.ItemsSource = SendQueue;
            IncomingList.ItemsSource  = ActiveReceives;
            ReceivedFilesList.ItemsSource = LibraryFiles;



            Devices.CollectionChanged += (_, _) => UpdateEmptyState();
            SendQueue.CollectionChanged += (_, _) =>
                Dispatcher.UIThread.Post(UpdateQueueUI);
            ActiveReceives.CollectionChanged += (_, _) =>
                Dispatcher.UIThread.Post(() => {
                    RecvEmptyState.IsVisible = ActiveReceives.Count == 0;
                    UpdateLibraryFilesList();
                });
            ReceivedFiles.CollectionChanged += (_, _) => UpdateLibraryFilesList();

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
            CleanWebSharedDirectory();
            
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
                try
                {
                    using var assetStream = Avalonia.Platform.AssetLoader.Open(new Uri("avares://WeShare.UI/Assets/logo.png"));
                    using var ms = new MemoryStream();
                    assetStream.CopyTo(ms);
                    _webDashboardService.LogoBytes = ms.ToArray();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WebDashboard] Failed to load logo asset: {ex.Message}");
                }
                _webDashboardService.SetPeersProvider(() => Devices.ToList());
                _webDashboardService.WebClientConnected += OnWebClientConnected;
                _webDashboardService.WebClientConnectedEx += OnWebClientConnectedEx;
                _webDashboardService.WebClientDisconnectedEx += OnWebClientDisconnectedEx;
                _webDashboardService.WebFileSharedCallback = OnWebFileSharedCallback;
                _webDashboardService.WebTransferStarted += OnTransferStarted;
                _webDashboardService.WebTransferProgress += OnTransferProgress;
                _webDashboardService.WebTransferCompleted += OnWebTransferCompleted;
                _webDashboardService.WebTransferFailed += OnTransferFailed;
                _webDashboardService.IsSessionActiveFilter = (string targetIpOrId) =>
                {
                    var activeIpOrId = GetActiveSessionDeviceIpOrId();
                    if (activeIpOrId != null && activeIpOrId != targetIpOrId)
                    {
                        return true;
                    }
                    return false;
                };
                _webDashboardService.Start();
            }
            catch (Exception ex)
            {
                ShowToast($"Web Portal start failed: {ex.Message}");
            }


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
                ToastBorder.Margin              = new Thickness(20, 48, 20, 0);
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
            UpdateNetworkLabels();
            _ = Task.Run(TryAutoNetworkAsync);
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
            // Show "LOOKING FOR DEVICES..." hint on the send-discovery radar when empty.
            // RadarEmptyHint lives inside SendDiscoveryPanel so it only renders when visible.
            RadarEmptyHint.IsVisible = Devices.Count == 0;
        }

        private void ShowPanel(Control panel, string title, Button? navBtn = null)
        {
            if (SendDiscoveryPanel != null && SendDiscoveryPanel.IsVisible && panel != SendDiscoveryPanel)
            {
                try { _platformService.StopBluetoothDiscovery(); } catch { }
            }

            if (HomePanel != null) HomePanel.IsVisible = false;
            if (SettingsPanel != null) SettingsPanel.IsVisible = false;
            if (AboutPanel != null) AboutPanel.IsVisible = false;
            if (ReceiveModePanel != null) ReceiveModePanel.IsVisible = false;
            if (FilesPanel != null) FilesPanel.IsVisible = false;
            if (SendFilesPanel != null) SendFilesPanel.IsVisible = false;
            if (SendDiscoveryPanel != null) SendDiscoveryPanel.IsVisible = false;
            if (TransfersPanel != null) TransfersPanel.IsVisible = false;
            if (WebSharedPanel != null) WebSharedPanel.IsVisible = false;
            if (SendStepWizard != null) SendStepWizard.IsVisible = false;

            bool wasReceiver = _localDevice.IsReceiver;
            _localDevice.IsReceiver = (panel == ReceiveModePanel);

            panel.IsVisible = true;
            PageTitle.Text = title;
            SetActiveNav(navBtn);

            if (wasReceiver != _localDevice.IsReceiver)
            {
                if (_discoveryService != null)
                {
                    _ = _discoveryService.BroadcastPresenceAsync();
                }

                if (_localDevice.IsReceiver)
                {
                    try { _platformService.StartBluetoothAdvertising(_localDevice); } catch { }
                }
                else
                {
                    try { _platformService.StopBluetoothAdvertising(); } catch { }
                }
            }
        }

        private bool HasActiveTransfer()
        {
            return _isSending || ActiveReceives.Count > 0;
        }

        private void NavHome_Click(object? sender, RoutedEventArgs e)
        {
            if (HasActiveTransfer())
            {
                ShowToast("Active transfer in progress — please wait");
                return;
            }
            ShowPanel(HomePanel, "HOME", NavHomeBtn);
        }
        private void NavFiles_Click(object? sender, RoutedEventArgs e)
        {
            if (HasActiveTransfer())
            {
                ShowToast("Active transfer in progress — please wait");
                return;
            }
            ShowPanel(FilesPanel, "LIBRARY", NavFilesBtn);
        }
        private void NavTransfers_Click(object? sender, RoutedEventArgs e) => ShowPanel(TransfersPanel, "TRANSFERS", NavTransfersBtn);
        private void NavWebShared_Click(object? sender, RoutedEventArgs e)
        {
            if (HasActiveTransfer())
            {
                ShowToast("Active transfer in progress — please wait");
                return;
            }
            ShowPanel(WebSharedPanel, "WEB SHARED", NavWebSharedBtn);
            UpdateWebSharedClientsList();
        }
        private void NavSettings_Click(object? sender, RoutedEventArgs e)
        {
            if (HasActiveTransfer())
            {
                ShowToast("Active transfer in progress — please wait");
                return;
            }
            ShowPanel(SettingsPanel, "SETTINGS", NavSettBtn);
        }
        private void NavAbout_Click(object? sender, RoutedEventArgs e)
        {
            if (HasActiveTransfer())
            {
                ShowToast("Active transfer in progress — please wait");
                return;
            }
            ShowPanel(AboutPanel, "ABOUT", NavAboutBtn);
        }

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

            // Update the "LOOKING FOR DEVICES..." hint immediately, then trigger a
            // fresh broadcast so newly-arrived receivers appear on the radar quickly.
            UpdateEmptyState();
            _ = _discoveryService.BroadcastPresenceAsync();

            try { _platformService.StartBluetoothDiscovery(OnDeviceDiscovered); } catch { }
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

        private async void CopyWebLink_Click(object sender, RoutedEventArgs e)
        {
            var url = HomeWebPortalText?.Text ?? "";
            if (string.IsNullOrEmpty(url)) return;
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(url);
                ShowToast($"Copied: {url}");
            }
        }

        private async void CopyWifiWebLink_Click(object sender, RoutedEventArgs e)
        {
            var url = HomeWifiWebPortalText?.Text ?? "";
            if (string.IsNullOrEmpty(url)) return;
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(url);
                ShowToast($"Copied: {url}");
            }
        }

        private void GenerateQrBitmap(string url)
        {
            try
            {
                var pngBytes = WeShare.Core.Services.QrCodeService.GenerateQrCodePng(url);
                using var ms = new MemoryStream(pngBytes);
                _qrBitmap = new Avalonia.Media.Imaging.Bitmap(ms);
                if (HomeQrImage != null)
                    HomeQrImage.Source = _qrBitmap;
            }
            catch { /* QR generation is non-critical */ }
        }

        private void SetActiveNav(Button? activeBtn)
        {
            var buttons = new[] { NavHomeBtn, NavFilesBtn, NavTransfersBtn, NavWebSharedBtn, NavSettBtn, NavAboutBtn };
            foreach (var btn in buttons)
                if (btn != null) btn.Classes.Set("Active", btn == activeBtn);
        }

        private async void RefreshHistory()
        {
            var history = await _dbHelper.GetAllTransfersAsync();
            var query = FileSearchBox.Text?.ToLower() ?? "";
            
            _isUpdatingLibrary = true;
            try
            {
                ReceivedFiles.Clear();
                var receivedDone = history.Where(t => t.Direction == TransferDirection.Received && t.Status == TransferStatus.Done).ToList();
                
                var now = DateTime.Now;
                var filteredByDate = receivedDone.Where(t =>
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
                
                UpdateStats(receivedDone);
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

        private string? GetActiveSessionDeviceIpOrId()
        {
            if (_isSending && _sendTarget != null)
            {
                return !string.IsNullOrEmpty(_sendTarget.IpAddress) ? _sendTarget.IpAddress : _sendTarget.Id;
            }
            if (ActiveReceives.Count > 0)
            {
                var first = ActiveReceives[0];
                return first.RemoteIp;
            }
            return null;
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
                UpdateLibraryFilesList();
                ShowToast("File deleted");
            }
            catch (Exception ex) { ShowToast($"Error deleting: {ex.Message}"); }
        }



        private bool _isSending = false;
        private void SendFile_Click(object sender, RoutedEventArgs e)
        {
            var device = (sender as Button)?.DataContext as DeviceModel;
            if (device == null) return;

            var activeIpOrId = GetActiveSessionDeviceIpOrId();
            if (activeIpOrId != null)
            {
                bool isSame = (device.IpAddress == activeIpOrId || device.Id == activeIpOrId);
                if (!isSame)
                {
                    ShowToast("Session active: Can only send to/receive from the active device.");
                    return;
                }
            }

            // If we're already sending to this device, just add to the queue
            if (_isSending && _sendTarget?.IpAddress == device.IpAddress)
            {
                ShowPanel(SendFilesPanel, "SEND FILES", null);
                return;
            }

            if (!string.IsNullOrEmpty(device.Ssid))
            {
                ShowToast($"Connecting to WeShare hotspot \"{device.Ssid}\"...");
                _ = Task.Run(async () => {
                    bool ok = await _platformService.ConnectToWifiAsync(device.Ssid, device.Password ?? "");
                    if (ok)
                    {
                        await Task.Delay(1500); // let DHCP settle
                        Dispatcher.UIThread.Post(() => {
                            ShowToast("Connected to WeShare hotspot! Initiating transfer...");
                            StartSendSession(device);
                        });
                    }
                    else
                    {
                        Dispatcher.UIThread.Post(() => {
                            ShowToast("Failed to connect to WeShare hotspot");
                        });
                    }
                });
                return;
            }

            StartSendSession(device);
        }

        private void StartSendSession(DeviceModel device)
        {
            var activeIpOrId = GetActiveSessionDeviceIpOrId();
            if (activeIpOrId != null)
            {
                bool isSame = (device.IpAddress == activeIpOrId || device.Id == activeIpOrId);
                if (!isSame)
                {
                    ShowToast("Session active: Can only send to/receive from the active device.");
                    return;
                }
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
                    // Peek at the first item WITHOUT removing it — we only dequeue after success.
                    // If the transfer fails, the item stays in the queue so the user can retry.
                    QueueItem? item = null;
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (SendQueue.Count > 0)
                            item = SendQueue[0];
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
                    });

                    try
                    {
                        if (device.Type == "Web Client")
                        {
                            // Batch all remaining web client files into a single notification
                            var allPaths = new System.Collections.Generic.List<string>();
                            var allItems = new System.Collections.Generic.List<QueueItem>();
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                foreach (var qi in SendQueue)
                                {
                                    allPaths.Add(qi.Path);
                                    allItems.Add(qi);
                                }
                            });

                            if (allPaths.Count > 1)
                            {
                                bool sent = _webDashboardService?.ShareMultipleForWebClient(device.Id, allPaths) ?? false;
                                if (!sent)
                                {
                                    ShowToast("Web client disconnected — cannot send files");
                                    _isSending = false;
                                    break;
                                }
                                await Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    SendQueue.Clear();
                                    UpdateQueueUI();
                                });
                            }
                            else
                            {
                                bool sent = _webDashboardService?.ShareForWebClient(device.Id, item.Path) ?? false;
                                if (!sent)
                                {
                                    ShowToast("Web client disconnected — cannot send file");
                                    _isSending = false;
                                    break;
                                }
                                await Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    if (SendQueue.Count > 0 && SendQueue[0] == item)
                                        SendQueue.RemoveAt(0);
                                    UpdateQueueUI();
                                });
                            }
                        }
                        else
                        {
                            using var stream = await item.OpenStream();
                            await _transferManager.SendFileAsync(device.IpAddress, device.Port, item.Name, stream, item.Size, item.Path);

                            // Transfer succeeded — now remove it from the queue
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                if (SendQueue.Count > 0 && SendQueue[0] == item)
                                    SendQueue.RemoveAt(0);
                                UpdateQueueUI();
                            });
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        // Transfer failed — item is still at position 0 so the user can retry.
                        // Stop processing the rest of the queue for this session.
                        ShowToast($"Transfer failed: {ex.Message}");
                        _isSending = false;
                        break;
                    }
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
                _isSending = false;
                Dispatcher.UIThread.Post(() =>
                {
                    SendProgressBorder.IsVisible = false;
                    ShowPanel(HomePanel, "HOME", NavHomeBtn);
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
                            Avalonia.Media.Imaging.Bitmap? thumbnail = null;
                            var ext = Path.GetExtension(path).ToLower();
                            if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".webp")
                            {
                                thumbnail = LoadThumbnail(path);
                            }
                            else
                            {
                                thumbnail = await LoadWindowsShellThumbnailAsync(path);
                            }
                            SendQueue.Add(new QueueItem {
                                Name = info.Name,
                                Path = path,
                                Size = info.Length,
                                OpenStream = () => Task.FromResult<Stream>(File.OpenRead(path)),
                                Thumbnail = thumbnail
                            });
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
            }
        }

        private void UpdateQueueUI()
        {
            QueueEmptyLabel.IsVisible = SendQueue.Count == 0;
            SendFooter.IsVisible      = SendQueue.Count > 0;
            if (ZipSendBtn != null)
            {
                ZipSendBtn.IsVisible = SendQueue.Count > 1;
            }
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

            SendProgressBorder.IsVisible = true;
            int sentCount = 0;

            // Process each item one at a time; only remove it from the queue AFTER
            // it succeeds.  This way a failure leaves remaining files intact for retry.
            if (_sendTarget.Type == "Web Client" && SendQueue.Count > 1)
            {
                // Batch all files into a single notification for web clients
                var allPaths = SendQueue.Select(q => q.Path).ToList();
                bool sent = _webDashboardService?.ShareMultipleForWebClient(_sendTarget.Id, allPaths) ?? false;
                if (!sent)
                {
                    SendProgressBorder.IsVisible = false;
                    ShowToast("Web client disconnected — cannot send files");
                    return;
                }
                sentCount = SendQueue.Count;
                SendQueue.Clear();
            }
            else
            {
            while (SendQueue.Count > 0)
            {
                var item = SendQueue[0];
                SendProgressFile.Text = item.Name;
                SendProgressBar.Value = 0;

                try
                {
                    if (_sendTarget.Type == "Web Client")
                    {
                        bool sent = _webDashboardService?.ShareForWebClient(_sendTarget.Id, item.Path) ?? false;
                        if (!sent)
                        {
                            SendProgressBorder.IsVisible = false;
                            ShowToast("Web client disconnected — cannot send file");
                            return;
                        }
                    }
                    else
                    {
                        using var stream = await item.OpenStream();
                        await _transferManager.SendFileAsync(_sendTarget.IpAddress, _sendTarget.Port, item.Name, stream, item.Size, item.Path);
                    }

                    // Success — remove from front of queue
                    if (SendQueue.Count > 0 && SendQueue[0] == item)
                        SendQueue.RemoveAt(0);
                    sentCount++;
                    UpdateQueueUI();
                }
                catch (Exception ex)
                {
                    SendProgressBorder.IsVisible = false;
                    ShowToast($"Transfer failed: {ex.Message}");
                    return;
                }
            }
            }

            SendProgressBorder.IsVisible = false;
            _sendTarget = null;
            UpdateQueueUI();
            ShowToast($"Successfully sent {sentCount} file(s)");
            ShowPanel(HomePanel, "HOME", NavHomeBtn);
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
                var localPath = f.Path.LocalPath ?? "";
                var ext = Path.GetExtension(localPath).ToLower();
                Avalonia.Media.Imaging.Bitmap? thumbnail = null;
                if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".webp")
                {
                    thumbnail = await LoadThumbnailAsync(f);
                }
                else
                {
                    thumbnail = await LoadWindowsShellThumbnailAsync(localPath);
                }
                list.Add(new QueueItem { 
                    Name = f.Name, 
                    Path = localPath, 
                    Size = (long)(props.Size ?? 0), 
                    OpenStream = () => f.OpenReadAsync(),
                    Thumbnail = thumbnail
                });
            }
            return list;
        }

        private async Task<Avalonia.Media.Imaging.Bitmap?> LoadWindowsShellThumbnailAsync(string path)
        {
            DebugLog($"LoadWindowsShellThumbnailAsync called for: '{path}'");
            try
            {
                if (System.IO.File.Exists(path))
                {
                    var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(path);
                    if (file != null)
                    {
                        using var thumbnail = await file.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.SingleItem, 80);
                        if (thumbnail != null)
                        {
                            using var stream = System.IO.WindowsRuntimeStreamExtensions.AsStreamForRead(thumbnail);
                            var bmp = new Avalonia.Media.Imaging.Bitmap(stream);
                            DebugLog($"Successfully loaded Windows shell thumbnail for '{path}' (size: {bmp.Size})");
                            return bmp;
                        }
                        else
                        {
                            DebugLog("GetThumbnailAsync returned null");
                        }
                    }
                    else
                    {
                        DebugLog("GetFileFromPathAsync returned null");
                    }
                }
                else
                {
                    DebugLog($"File does not exist: '{path}'");
                }
            }
            catch (Exception ex)
            {
                DebugLog($"Failed to load Windows shell thumbnail for '{path}': {ex.Message}\n{ex.StackTrace}");
            }
            return null;
        }

        private static void DebugLog(string message)
        {
            try
            {
                System.IO.File.AppendAllText(@"D:\PROJECTS\WE SHARE\thumbnail_debug.log", $"[{DateTime.Now:HH:mm:ss}] {message}\n");
            }
            catch {}
        }

        private async Task<Avalonia.Media.Imaging.Bitmap?> LoadThumbnailAsync(IStorageFile file)
        {
            DebugLog($"LoadThumbnailAsync called for file: '{file.Name}', path='{file.Path}'");
            try
            {
                var ext = System.IO.Path.GetExtension(file.Name).ToLower();
                DebugLog($"Resolved extension: '{ext}'");
                if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".webp")
                {
                    using var stream = await file.OpenReadAsync();
                    DebugLog($"Opened read stream. Length={stream.Length}, CanSeek={stream.CanSeek}");
                    var bmp = Avalonia.Media.Imaging.Bitmap.DecodeToWidth(stream, 80);
                    DebugLog($"Successfully decoded bitmap. Size: {bmp.Size.Width}x{bmp.Size.Height}");
                    return bmp;
                }
                else
                {
                    DebugLog($"Not a supported extension: '{ext}'");
                }
            }
            catch (Exception ex)
            {
                DebugLog($"Failed to load thumbnail for '{file.Name}': {ex.Message}\n{ex.StackTrace}");
            }
            return null;
        }

        private Avalonia.Media.Imaging.Bitmap? LoadThumbnail(string? path)
        {
            DebugLog($"LoadThumbnail called for path: '{path}'");
            if (string.IsNullOrEmpty(path))
            {
                DebugLog("Path is null or empty.");
                return null;
            }
            try
            {
                var ext = System.IO.Path.GetExtension(path).ToLower();
                DebugLog($"Resolved extension: '{ext}'");
                if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".webp")
                {
                    using var stream = System.IO.File.OpenRead(path);
                    DebugLog($"Opened file stream. Length={stream.Length}, CanSeek={stream.CanSeek}");
                    var bmp = Avalonia.Media.Imaging.Bitmap.DecodeToWidth(stream, 80);
                    DebugLog($"Successfully decoded file bitmap. Size: {bmp.Size.Width}x{bmp.Size.Height}");
                    return bmp;
                }
                else
                {
                    DebugLog($"Not a supported extension: '{ext}'");
                }
            }
            catch (Exception ex)
            {
                DebugLog($"Failed to load '{path}': {ex.Message}\n{ex.StackTrace}");
            }
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

        private void UpdateStats(System.Collections.Generic.List<FileTransferState> receivedDone)
        {
            if (LibraryStatsText == null) return;

            if (receivedDone.Count == 0)
            {
                LibraryStatsText.Text = "No transfers recorded";
                return;
            }

            long totalBytes = 0;
            foreach (var item in receivedDone)
            {
                totalBytes += item.TotalBytes;
            }

            string sizeDisplay = FileTransferState.FormatBytes(totalBytes);

            var dates = receivedDone.Select(t => t.Timestamp.ToLocalTime()).OrderBy(d => d).ToList();
            var minDate = dates.First();
            var maxDate = dates.Last();

            string rangeDisplay = minDate.Date == maxDate.Date
                ? minDate.ToString("MMM d, yyyy")
                : $"{minDate:MMM d} - {maxDate:MMM d, yyyy}";

            LibraryStatsText.Text = $"Total Received: {receivedDone.Count} files ({sizeDisplay})  •  Active since {rangeDisplay}";
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

        private async Task TryAutoNetworkAsync()
        {
            try
            {
                var ip = UdpDiscoveryService.GetLocalIp();
                bool hasRealIp = ip != "127.0.0.1";

                if (hasRealIp)
                {
                    // Already connected to a network — run direct host hotspot so users can connect
                    _hotspotService = new HotspotService();
                    if (await _hotspotService.IsSupportedAsync())
                    {
                        ShowToast("Starting WeShare hotspot...");
                        var (started, _) = await _hotspotService.StartAsync();
                        if (started)
                        {
                            await Task.Delay(1000);
                            UpdateNetworkLabels();
                            ShowToast($"Hotspot active — connect devices to \"{HotspotService.TargetSsid}\"");
                        }
                    }
                    return;
                }

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
                var (startedHost, _) = await _hotspotService.StartAsync();
                if (startedHost)
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

        /// <summary>Updates the existing network info labels in-place.</summary>
        private async void UpdateNetworkLabels()
        {
            var ip = UdpDiscoveryService.GetLocalIp();
            bool hasRealIp = ip != "127.0.0.1";
            string ssid = "Not Connected";
            string password = "None";

            bool isHotspotRunning = _hotspotService != null && _hotspotService.IsRunning;

            if (isHotspotRunning)
            {
                ssid = HotspotService.TargetSsid;
                password = HotspotService.TargetPassword;
            }
            else if (hasRealIp)
            {
                var detectedSsid = await _platformService.GetCurrentWifiSsidAsync();
                ssid = !string.IsNullOrEmpty(detectedSsid) ? detectedSsid : "Local Wi-Fi Network";
                password = "None (Already Connected)";
            }

            Dispatcher.UIThread.Post(() =>
            {
                string info = hasRealIp
                    ? $"{ip}:{_localDevice.Port}"
                    : $"Wi-Fi: {ssid} / {password}";

                SidebarNetworkInfo.Text  = info;
                HomeNetworkInfoText.Text = ssid;
                HomeWifiPasswordText.Text = password;
                
                string hostIp = isHotspotRunning ? _hotspotService!.HotspotIp : ip;
                string webUrl = isHotspotRunning ? $"http://{hostIp}" : $"http://{hostIp}:8080";
                HomeWebPortalText.Text   = webUrl;
                GenerateQrBitmap(webUrl);

                if (isHotspotRunning && hasRealIp && ip != _hotspotService!.HotspotIp)
                {
                    string wifiWebUrl = $"http://{ip}:8080";
                    if (HomeWifiWebPortalText != null) HomeWifiWebPortalText.Text = wifiWebUrl;
                    if (HomeWifiWebPortalPanel != null) HomeWifiWebPortalPanel.IsVisible = true;
                    if (WebPortalLabel != null) WebPortalLabel.Text = "Web Portal (Hotspot Gateway)";
                }
                else
                {
                    if (HomeWifiWebPortalPanel != null) HomeWifiWebPortalPanel.IsVisible = false;
                    if (WebPortalLabel != null) WebPortalLabel.Text = "Web Portal (Local Share)";
                }
            });

            string hostIpStr = isHotspotRunning ? _hotspotService!.HotspotIp : ip;
            _ = Task.Run(() =>
            {
                if (_captivePortalService != null)
                {
                    _captivePortalService.Stop();
                    _captivePortalService = null;
                }

                if (System.Net.IPAddress.TryParse(hostIpStr, out var parsedIp) && !System.Net.IPAddress.IsLoopback(parsedIp) && parsedIp.ToString() != "127.0.0.1")
                {
                    _captivePortalService = new CaptivePortalService(parsedIp);
                    _captivePortalService.Start();
                }
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
            var activeIpOrId = GetActiveSessionDeviceIpOrId();
            if (activeIpOrId != null)
            {
                bool isSame = (state.RemoteIp == activeIpOrId || state.FileId == activeIpOrId);
                if (!isSame)
                {
                    return false;
                }
            }

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
                _platformService.ShowSystemToast("Incoming File Request", $"{state.PeerName} wants to send {state.FileName} ({FileTransferState.FormatBytes(state.TotalBytes)})");
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
            ShowPanel(HomePanel, "HOME", NavHomeBtn);
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
                    {
                        Devices.Remove(existing);
                        UpdateEmptyState();
                    }
                    return;
                }

                // Ensure we don't show the same device multiple times (match by unique ID)
                if (existing == null) 
                {
                    Devices.Add(device);
                    UpdateEmptyState();
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
                else 
                { 
                    SendProgressBorder.IsVisible = true; 
                    _currentSendingFileId = state.FileId;
                    SendSpeedGraph?.Clear();
                }
                
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
                    SendSpeedGraph?.AddSpeed(state.SpeedMbPerSec);
                }

                state.SpeedPoints.Add(state.SpeedMbPerSec);
                if (state.SpeedPoints.Count > 40)
                {
                    state.SpeedPoints.RemoveAt(0);
                }

                GlobalActivityBorder.IsVisible = true;
                GlobalProgressBar.Value        = state.ProgressPercentage;

                // Live speed badge on Home
                if (HomeSpeedBadge != null)
                {
                    string dir = state.Direction == TransferDirection.Sent ? "↑" : "↓";
                    HomeSpeedBadge.Text       = $"{dir} {state.SpeedMbPerSec:F1} MB/s";
                    HomeSpeedBadge.IsVisible  = true;
                }
            });
        }

        private void OnTransferCompleted(FileTransferState state)
        {
            Dispatcher.UIThread.Post(async () => {
                SendProgressBorder.IsVisible   = false;
                GlobalActivityBorder.IsVisible = false;
                if (HomeSpeedBadge != null) HomeSpeedBadge.IsVisible = false;
                if (state.Direction == TransferDirection.Received)
                {
                    var ex = ActiveReceives.FirstOrDefault(s => s.FileId == state.FileId);
                    if (ex != null) ActiveReceives.Remove(ex);
                    await _dbHelper.SaveTransferAsync(state);
                    ReceivedFiles.Insert(0, state);
                    ShowToast($"Received: {state.FileName}");
                    _platformService.ShowSystemToast("File Received", $"{state.FileName} from {state.PeerName}", state.FilePath);
                }
                else
                {
                    _currentSendingFileId = null;
                    await _dbHelper.SaveTransferAsync(state);
                    CleanTempZipFile(state.FilePath);
                }
            });
        }

        private void OnTransferFailed(FileTransferState state) 
        {
            Dispatcher.UIThread.Post(async () => {
                SendProgressBorder.IsVisible   = false;
                GlobalActivityBorder.IsVisible = false;
                if (HomeSpeedBadge != null) HomeSpeedBadge.IsVisible = false;
                if (state.Direction == TransferDirection.Received)
                {
                    var ex = ActiveReceives.FirstOrDefault(s => s.FileId == state.FileId);
                    if (ex != null) ActiveReceives.Remove(ex);
                }
                else
                {
                    _currentSendingFileId = null;
                    CleanTempZipFile(state.FilePath);
                }
                await _dbHelper.SaveTransferAsync(state);
                string reason = !string.IsNullOrEmpty(state.ErrorMessage) ? state.ErrorMessage : "Connection failed or rejected";
                ShowToast($"Transfer failed: {reason}");
                _platformService.ShowSystemToast("Transfer Failed", $"{state.FileName}: {reason}");
            });
        }

        private void CancelActiveSend_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_currentSendingFileId))
            {
                _transferManager.CancelTransfer(_currentSendingFileId);
                ShowToast("Sending cancelled");
            }
        }

        private void CancelIncoming_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is FileTransferState state)
            {
                _transferManager.CancelTransfer(state.FileId);
                ShowToast("Receiving cancelled");
            }
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


        private void OnWebTransferCompleted(FileTransferState state)
        {
            Dispatcher.UIThread.Post(async () => {
                SendProgressBorder.IsVisible   = false;
                GlobalActivityBorder.IsVisible = false;
                if (HomeSpeedBadge != null) HomeSpeedBadge.IsVisible = false;

                var ex = ActiveReceives.FirstOrDefault(s => s.FileId == state.FileId);
                if (ex != null) ActiveReceives.Remove(ex);

                if (state.Direction == TransferDirection.Received)
                {
                    try
                    {
                        if (System.IO.File.Exists(state.FilePath))
                        {
                            string filename = Path.GetFileName(state.FilePath);
                            string ext = Path.GetExtension(filename);
                            string category = TcpTransferManager.GetCategoryFolder(ext);
                            string targetDir = Path.Combine(_saveDirectory, category);
                            Directory.CreateDirectory(targetDir);

                            string destPath = GetUniqueFilePath(targetDir, filename);
                            System.IO.File.Move(state.FilePath, destPath);
                            state.FilePath = destPath;

                            await _dbHelper.SaveTransferAsync(state);
                            ReceivedFiles.Insert(0, state);
                            ShowToast($"Received via Web Portal: {state.FileName}");
                            _platformService.ShowSystemToast("File Received", $"{state.FileName} from {state.PeerName}", state.FilePath);
                            UpdateEmptyState();
                            RefreshHistory();
                        }
                    }
                    catch (Exception ex2)
                    {
                        Console.WriteLine($"[WebDashboard] Error finalizing received file: {ex2.Message}");
                    }
                }
            });
        }

        private void OnWebClientConnected(string type, string remoteIp)
        {
            Dispatcher.UIThread.Post(() => {
                ShowToast($"Web client connected from {remoteIp}");
            });
        }

        private void OnWebClientConnectedEx(WebDashboardService.WebClientInfo client)
        {
            Dispatcher.UIThread.Post(() => {
                var existing = Devices.FirstOrDefault(d => d.Id == client.ClientId);
                if (existing != null)
                {
                    existing.Name = client.Name;
                    existing.IpAddress = client.IpAddress;
                    existing.LastSeen = DateTime.Now;
                }
                else
                {
                    Devices.Add(new DeviceModel
                    {
                        Id = client.ClientId,
                        Name = client.Name,
                        IpAddress = client.IpAddress,
                        Type = "Web Client",
                        LastSeen = DateTime.Now,
                        Port = 8080
                    });
                }
                UpdateEmptyState();
                ShowToast($"Web client '{client.Name}' connected");
                if (WebSharedPanel != null && WebSharedPanel.IsVisible)
                {
                    UpdateWebSharedClientsList();
                }
            });
        }

        private void OnWebClientDisconnectedEx(string clientId)
        {
            Dispatcher.UIThread.Post(() => {
                var existing = Devices.FirstOrDefault(d => d.Id == clientId);
                if (existing != null)
                {
                    Devices.Remove(existing);
                    UpdateEmptyState();
                    ShowToast($"Web client '{existing.Name}' disconnected");
                }

                // Cleanup staged files for this disconnected client
                var toRemove = StagedWebFiles.Where(f => f.ClientId == clientId).ToList();
                foreach (var file in toRemove)
                {
                    try
                    {
                        if (File.Exists(file.FilePath))
                        {
                            File.Delete(file.FilePath);
                        }
                    }
                    catch { }
                    StagedWebFiles.Remove(file);
                }
                UpdateWebSharedClientsList();
            });
        }

        public void Shutdown()
        {
            try { _captivePortalService?.Stop(); } catch { }
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
 
            CleanWebSharedDirectory();
            CleanTempZipDirectory();
        }

        // CTS to cancel the previous toast's hide-delay when a new toast fires
        private CancellationTokenSource? _toastCts;

        private void ShowToast(string message)
        {
            Dispatcher.UIThread.Post(async () =>
            {
                _toastCts?.Cancel();
                _toastCts = new CancellationTokenSource();
                var token = _toastCts.Token;

                ToastMessage.Text     = message;
                ToastBorder.Opacity   = 0;
                ToastBorder.IsVisible = true;
                ToastBorder.Classes.Add("ToastVisible");

                // Fade-in handled by animation; wait for display duration
                try
                {
                    await Task.Delay(100, token);   // let fade-in start
                    ToastBorder.Opacity = 1;
                    await Task.Delay(2800, token);  // visible time

                    // Fade out manually
                    for (int i = 10; i >= 0; i--)
                    {
                        token.ThrowIfCancellationRequested();
                        ToastBorder.Opacity = i / 10.0;
                        await Task.Delay(20, token);
                    }
                    ToastBorder.IsVisible = false;
                    ToastBorder.Classes.Remove("ToastVisible");
                }
                catch (OperationCanceledException) { /* newer toast took over */ }
            });
        }

        // ── ZIP Multi-file Send & Cleanups ────────────────────────────────────
        private string GetTempZipDirectory()
        {
            return @"d:\PROJECTS\WE SHARE\temp_zip_send";
        }

        private void CleanTempZipDirectory()
        {
            try
            {
                var tempDir = GetTempZipDirectory();
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch { }
        }

        private void CleanTempZipFile(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            try
            {
                var tempDir = GetTempZipDirectory();
                if (Path.GetFullPath(filePath).StartsWith(Path.GetFullPath(tempDir), StringComparison.OrdinalIgnoreCase))
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
            }
            catch { }
        }

        private async void ZipSend_Click(object sender, RoutedEventArgs e)
        {
            if (SendQueue.Count <= 1)
            {
                ShowToast("Add multiple files to send as a ZIP archive");
                return;
            }

            try
            {
                var tempDir = GetTempZipDirectory();
                Directory.CreateDirectory(tempDir);

                var zipName = $"WeShare_Archive_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
                var zipPath = Path.Combine(tempDir, zipName);

                ShowToast("Creating ZIP archive...");

                // Compress queued files to the ZIP archive on a background thread to keep UI fluid
                await Task.Run(async () =>
                {
                    using (var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (var archive = new System.IO.Compression.ZipArchive(fs, System.IO.Compression.ZipArchiveMode.Create))
                    {
                        foreach (var item in SendQueue.ToList())
                        {
                            var entry = archive.CreateEntry(item.Name, System.IO.Compression.CompressionLevel.Fastest);
                            using (var entryStream = entry.Open())
                            using (var fileStream = await item.OpenStream())
                            {
                                await fileStream.CopyToAsync(entryStream);
                            }
                        }
                    }
                });

                var zipInfo = new FileInfo(zipPath);

                // Clear queue and replace it with the single ZIP file
                SendQueue.Clear();
                SendQueue.Add(new QueueItem
                {
                    Name = zipName,
                    Path = zipPath,
                    Size = zipInfo.Length,
                    OpenStream = () => Task.FromResult<Stream>(File.OpenRead(zipPath)),
                    Thumbnail = null
                });

                ShowToast("ZIP archive created!");
                NavSendDiscovery_Click(this, new RoutedEventArgs());
            }
            catch (Exception ex)
            {
                ShowToast($"ZIP creation failed: {ex.Message}");
            }
        }

        // ── Web Shared Staging Helpers ─────────────────────────────────────────

        private async Task<bool> OnWebFileSharedCallback(FileTransferState state)
        {
            // Prompt user using the standard dialog popup
            bool accepted = await OnTransferRequested(state);

            if (accepted)
            {
                try
                {
                    if (File.Exists(state.FilePath))
                    {
                        string filename = Path.GetFileName(state.FilePath);
                        string ext = Path.GetExtension(filename);
                        string category = TcpTransferManager.GetCategoryFolder(ext);
                        string targetDir = Path.Combine(_saveDirectory, category);
                        Directory.CreateDirectory(targetDir);

                        string destPath = GetUniqueFilePath(targetDir, filename);
                        File.Move(state.FilePath, destPath);
                        state.FilePath = destPath;

                        await _dbHelper.SaveTransferAsync(state);
                        Dispatcher.UIThread.Post(() => {
                            ReceivedFiles.Insert(0, state);
                            ShowToast($"Received via Web Portal: {state.FileName}");
                            UpdateEmptyState();
                            _platformService.ShowSystemToast("File Received", $"{state.FileName} from {state.PeerName}", state.FilePath);
                        });
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.UIThread.Post(() => {
                        ShowToast($"Failed to save file: {ex.Message}");
                    });
                    return false;
                }
            }
            else
            {
                try
                {
                    if (File.Exists(state.FilePath))
                    {
                        File.Delete(state.FilePath);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WebShared] Error deleting rejected file: {ex.Message}");
                }
                Dispatcher.UIThread.Post(() => {
                    ShowToast($"Rejected web file: {state.FileName}");
                });
            }

            return accepted;
        }

        private void UpdateWebSharedClientsList()
        {
            var webClients = Devices.Where(d => d.Type == "Web Client").ToList();
            WebClientsListBox.ItemsSource = webClients;

            var selected = WebClientsListBox.SelectedItem as DeviceModel;
            if (selected == null || !webClients.Any(c => c.Id == selected.Id))
            {
                WebClientsListBox.SelectedItem = webClients.FirstOrDefault();
            }
            UpdateWebSharedFilesList();
        }

        private void UpdateWebSharedFilesList()
        {
            var selectedClient = WebClientsListBox.SelectedItem as DeviceModel;
            if (selectedClient == null)
            {
                WebSharedFilesList.ItemsSource = null;
                WebFilesCountText.Text = "0 files";
                WebFilesEmptyLabel.IsVisible = true;
                return;
            }

            var clientFiles = StagedWebFiles.Where(f => f.ClientId == selectedClient.Id).ToList();
            WebSharedFilesList.ItemsSource = clientFiles;
            WebFilesCountText.Text = $"{clientFiles.Count} file{(clientFiles.Count == 1 ? "" : "s")}";
            WebFilesEmptyLabel.IsVisible = clientFiles.Count == 0;
        }

        private void WebClientsListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            UpdateWebSharedFilesList();
        }

        private async void AcceptWebSharedFile_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is StagedWebFile stagedFile)
            {
                try
                {
                    if (File.Exists(stagedFile.FilePath))
                    {
                        string destPath = GetUniqueFilePath(_saveDirectory, stagedFile.FileName);
                        File.Move(stagedFile.FilePath, destPath);

                        var state = new FileTransferState
                        {
                            FileName = Path.GetFileName(destPath),
                            FilePath = destPath,
                            TotalBytes = stagedFile.Size,
                            TransferredBytes = stagedFile.Size,
                            Status = TransferStatus.Done,
                            Direction = TransferDirection.Received,
                            PeerName = stagedFile.ClientName,
                            Timestamp = DateTime.UtcNow
                        };

                        await _dbHelper.SaveTransferAsync(state);
                        ReceivedFiles.Insert(0, state);
                        StagedWebFiles.Remove(stagedFile);
                        UpdateWebSharedFilesList();
                        ShowToast($"File accepted and saved: {state.FileName}");
                    }
                    else
                    {
                        ShowToast("Source file does not exist.");
                        StagedWebFiles.Remove(stagedFile);
                        UpdateWebSharedFilesList();
                    }
                }
                catch (Exception ex)
                {
                    ShowToast($"Failed to accept file: {ex.Message}");
                }
            }
        }

        private void RejectWebSharedFile_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is StagedWebFile stagedFile)
            {
                try
                {
                    if (File.Exists(stagedFile.FilePath))
                    {
                        File.Delete(stagedFile.FilePath);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WebShared] Error deleting rejected file: {ex.Message}");
                }

                StagedWebFiles.Remove(stagedFile);
                UpdateWebSharedFilesList();
                ShowToast($"Rejected file: {stagedFile.FileName}");
            }
        }

        private void CleanWebSharedDirectory()
        {
            try
            {
                string webSharedDir = Path.Combine(_saveDirectory, "web_shared");
                if (Directory.Exists(webSharedDir))
                {
                    var files = Directory.GetFiles(webSharedDir);
                    foreach (var f in files)
                    {
                        try { File.Delete(f); } catch { }
                    }
                }
            }
            catch { }
        }

        private static string GetUniqueFilePath(string dir, string filename)
        {
            string baseName = Path.GetFileNameWithoutExtension(filename);
            string ext = Path.GetExtension(filename);
            string dest = Path.Combine(dir, filename);
            int count = 1;
            while (File.Exists(dest))
            {
                dest = Path.Combine(dir, $"{baseName} ({count}){ext}");
                count++;
            }
            return dest;
        }
    }

    public class StagedWebFile
    {
        public string ClientId { get; set; } = "";
        public string ClientName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string FileName => Path.GetFileName(FilePath);
        public long Size { get; set; }
        public string SizeDisplay
        {
            get
            {
                string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                int order = 0;
                double len = Size;
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len /= 1024;
                }
                return $"{len:0.##} {sizes[order]}";
            }
        }

        public string FileTypeBadge
        {
            get
            {
                string ext = Path.GetExtension(FilePath).ToLowerInvariant().TrimStart('.');
                if (string.IsNullOrEmpty(ext)) return "DIR";
                return ext.ToUpperInvariant();
            }
        }
 
        public string FileColor
        {
            get
            {
                string ext = Path.GetExtension(FilePath).ToLowerInvariant().TrimStart('.');
                if (string.IsNullOrEmpty(ext)) return "#475569";
                if (System.Linq.Enumerable.Contains(new[] { "png", "jpg", "jpeg", "gif", "webp", "bmp", "svg" }, ext)) return "#0ea5e9";
                if (System.Linq.Enumerable.Contains(new[] { "mp4", "mkv", "avi", "mov", "webm", "flv", "wmv" }, ext)) return "#10b981";
                if (System.Linq.Enumerable.Contains(new[] { "mp3", "wav", "flac", "ogg", "m4a", "aac" }, ext)) return "#ec4899";
                if (System.Linq.Enumerable.Contains(new[] { "pdf", "doc", "docx", "txt", "rtf", "md", "xls", "xlsx", "csv", "ppt", "pptx" }, ext)) return "#3b82f6";
                if (System.Linq.Enumerable.Contains(new[] { "zip", "rar", "tar", "gz", "7z" }, ext)) return "#f59e0b";
                if (System.Linq.Enumerable.Contains(new[] { "exe", "msi", "bat", "sh" }, ext)) return "#6366f1";
                return "#64748b";
            }
        }
    }
}
