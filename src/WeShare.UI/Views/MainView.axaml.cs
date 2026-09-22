using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using WeShare.Core.Data;
using WeShare.Core.Discovery;
using WeShare.Core.Models;
using WeShare.Core.Network;
using WeShare.Core.Services;
using WeShare.Core.Transfer;

namespace WeShare.UI.Views
{
    public partial class MainView : UserControl
    {
        public const string CurrentVersion = "1.1.0";
        private string? _latestVersionDownloadUrl;
        private string? _latestVersionName;

        // ── Core Services ─────────────────────────────────────────────────────
        private DeviceModel _localDevice;
        private UdpDiscoveryService _discoveryService;
        private TcpTransferManager _transferManager = null!;
        private DatabaseHelper _dbHelper;
        private IPlatformService _platformService;
        private WebDashboardService? _webDashboardService;
        private HotspotService? _hotspotService;
        private WifiConnectorService? _wifiConnector;
        private CaptivePortalService? _captivePortalService;

        private string _saveDirectory;
        private DeviceModel? _sendTarget;
        private Avalonia.Media.Imaging.Bitmap? _qrBitmap;

        // ── Observable Collections ────────────────────────────────────────────
        public ObservableCollection<DeviceModel> Devices { get; } = new();
        public ObservableCollection<DeviceModel> ActiveReceivers { get; } = new();
        public ObservableCollection<DeviceModel> ActiveSenders { get; } = new();
        public ObservableCollection<QueueItem> SendQueue { get; } = new();
        public ObservableCollection<FileTransferState> ActiveSends { get; } = new();
        public ObservableCollection<FileTransferState> ActiveReceives { get; } = new();
        public ObservableCollection<FileTransferState> ReceivedFiles { get; } = new();
        public ObservableCollection<FileTransferState> LibraryFiles { get; } = new();
        public ObservableCollection<StagedWebFile> StagedWebFiles { get; } = new();
        public ObservableCollection<QueueItem> WebClientSendQueue { get; } = new();
        public ObservableCollection<BatchCheckItem> BatchManifestItems { get; } = new();
        public ObservableCollection<ActiveBatchFileItem> ActiveTransferBatchFiles { get; } = new();

        // ── Transfer & Session State ──────────────────────────────────────────
        private bool _isTransferInProgress = false;
        private long _activeBatchTotalBytes = 0;
        private long _activeBatchTransferredBytes = 0;
        private int _activeBatchTotalCount = 0;
        private int _activeBatchCompletedCount = 0;
        private string _activeTransferPeerName = "";
        private bool _activeTransferIsSender = true;
        private DateTime _transferStartTime;
        private DeviceModel? _selectedWebClient;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Generic.List<QueueItem>> _lastDeclinedItems = new();
        private TaskCompletionSource<System.Collections.Generic.List<string>>? _batchManifestTcs;
        private TaskCompletionSource<bool>? _resendRequestTcs;
        private ResendRequest? _currentResendRequest;
        
        // ── Concurrency & Prefs ────────────────────────────────────────────────
        private readonly System.Threading.SemaphoreSlim _uiRequestLock = new(1, 1);
        private string? _lastAcceptedIp;
        private DateTime _lastAcceptedTime;
        private string? _activeAcceptedBatchId;
        private string? _currentBatchSenderIp;
        private BatchManifest? _currentPendingBatchManifest;
        private bool _isUpdatingLibrary = false;
        private bool _isLibraryUpdatePending = false;
        private string? _currentSendingFileId;
        private string _currentDateFilter = "All";
        private bool _autoAcceptAllTransfers = false;
        private bool _soundEffectsEnabled = true;
        private string? _activePreviewFilePath;
        private DeviceModel? _editingNicknameDevice;
        private readonly Dictionary<string, string> _deviceNicknames = new();
        private readonly HashSet<string> _favoriteDeviceIds = new();
        private CancellationTokenSource? _toastCts;
        private bool _isCapturingScreenshots = false;

        public MainView() : this(App.PlatformService) { }

        public MainView(IPlatformService? platformService)
        {
            InitializeComponent();
            CleanTempZipDirectory();

            _saveDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            _dbHelper = new DatabaseHelper();
            _platformService = platformService ?? new Services.StubPlatformService();
            var savedName = _dbHelper.GetSetting("DeviceName", "");
            string initialDeviceName = !string.IsNullOrWhiteSpace(savedName) ? savedName : Environment.MachineName;
            _localDevice = new DeviceModel { Port = 45679, Name = initialDeviceName, Type = _platformService.GetDeviceType() };

            // Bind list sources
            SendQueueList.ItemsSource = SendQueue;
            IncomingList.ItemsSource  = ActiveReceives;
            OutgoingList.ItemsSource  = ActiveSends;
            ReceivedFilesList.ItemsSource = LibraryFiles;
            if (WebClientSendQueueList != null) WebClientSendQueueList.ItemsSource = WebClientSendQueue;
            if (ActiveTransferBatchList != null) ActiveTransferBatchList.ItemsSource = ActiveTransferBatchFiles;

            Devices.CollectionChanged += (_, _) => Dispatcher.UIThread.Post(UpdateEmptyState);
            ActiveReceivers.CollectionChanged += (_, _) => Dispatcher.UIThread.Post(UpdateEmptyState);
            ActiveSenders.CollectionChanged += (_, _) => Dispatcher.UIThread.Post(UpdateEmptyState);
            SendQueue.CollectionChanged += (_, _) =>
                Dispatcher.UIThread.Post(UpdateQueueUI);
            ActiveSends.CollectionChanged += (_, _) =>
                Dispatcher.UIThread.Post(UpdateTransfersVisibility);
            ActiveReceives.CollectionChanged += (_, _) =>
                Dispatcher.UIThread.Post(() => {
                    RecvEmptyState.IsVisible = ActiveReceives.Count == 0;
                    UpdateTransfersVisibility();
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
                _discoveryService.DeviceLost += OnDeviceLost;
                _discoveryService.StartListening();
            }
            catch (Exception ex)
            {
                _discoveryService = new UdpDiscoveryService(_localDevice);
                _discoveryService.DeviceDiscovered += OnDeviceDiscovered;
                _discoveryService.DeviceLost += OnDeviceLost;
                ShowToast($"Discovery failed: {ex.Message}");
            }

            _saveDirectory = _platformService.GetDefaultSavePath();
            SettingsSaveLocationLabel.Text = _saveDirectory;
            CleanWebSharedDirectory();

            var autoAcceptVal = _dbHelper.GetSetting("AutoAcceptTransfers", "false");
            _autoAcceptAllTransfers = autoAcceptVal == "true";
            if (AutoAcceptToggle != null) AutoAcceptToggle.IsChecked = _autoAcceptAllTransfers;

            var soundVal = _dbHelper.GetSetting("SoundEffects", "true");
            _soundEffectsEnabled = soundVal == "true";
            if (SoundToggle != null) SoundToggle.IsChecked = _soundEffectsEnabled;

            var savedAccent = _dbHelper.GetSetting("AccentColor", "#4F46E5");
            if (!string.IsNullOrEmpty(savedAccent)) ApplyAccentColor(savedAccent);

            LoadDevicePreferences();
            
            // Transfer – listen for incoming file sends
            try
            {
                _transferManager = new TcpTransferManager(45679);
                ConfigureTransferManager(_transferManager);
                _transferManager.StartListening(_saveDirectory);
                _localDevice.Port = _transferManager.BoundPort;
            }
            catch (Exception)
            {
                try
                {
                    _transferManager = new TcpTransferManager(0);
                    ConfigureTransferManager(_transferManager);
                    _transferManager.StartListening(_saveDirectory);
                    _localDevice.Port = _transferManager.BoundPort;
                }
                catch (Exception)
                {
                    ShowToast("Could not start transfer service. Please check your network.", 4000);
                }
            }

            // Web Dashboard – start port 8080 web server
            try
            {
                _webDashboardService = new WebDashboardService(_saveDirectory, _localDevice);
                try
                {
                    using var assetStream = Avalonia.Platform.AssetLoader.Open(new Uri("avares://WeShare.UI/Assets/app_logo.png"));
                    using var ms = new MemoryStream();
                    assetStream.CopyTo(ms);
                    _webDashboardService.LogoBytes = ms.ToArray();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WebDashboard] Failed to load logo asset: {ex.Message}");
                }
                try
                {
                    using var sendStream = Avalonia.Platform.AssetLoader.Open(new Uri("avares://WeShare.UI/Assets/send.png"));
                    using var msSend = new MemoryStream();
                    sendStream.CopyTo(msSend);
                    _webDashboardService.SendIconBytes = msSend.ToArray();
                }
                catch { }
                try
                {
                    using var recvStream = Avalonia.Platform.AssetLoader.Open(new Uri("avares://WeShare.UI/Assets/receive.png"));
                    using var msRecv = new MemoryStream();
                    recvStream.CopyTo(msRecv);
                    _webDashboardService.ReceiveIconBytes = msRecv.ToArray();
                }
                catch { }
                try
                {
                    using var findStream = Avalonia.Platform.AssetLoader.Open(new Uri("avares://WeShare.UI/Assets/find.png"));
                    using var msFind = new MemoryStream();
                    findStream.CopyTo(msFind);
                    _webDashboardService.FindIconBytes = msFind.ToArray();
                }
                catch { }
                _webDashboardService.SetPeersProvider(() => Devices.ToList());
                _webDashboardService.WebClientConnected += OnWebClientConnected;
                _webDashboardService.WebClientConnectedEx += OnWebClientConnectedEx;
                _webDashboardService.WebClientRoleChanged += OnWebClientRoleChanged;
                _webDashboardService.WebClientDisconnectedEx += OnWebClientDisconnectedEx;
                _webDashboardService.WebClientHeartbeat += OnWebClientHeartbeat;
                _webDashboardService.WebFileShared += OnWebFileShared;
                _webDashboardService.WebFileSharedCallback = OnWebFileSharedCallback;
                _webDashboardService.WebTransferStarted += OnTransferStarted;
                _webDashboardService.WebTransferProgress += OnTransferProgress;
                _webDashboardService.WebTransferCompleted += OnWebTransferCompleted;
                _webDashboardService.WebTransferFailed += OnTransferFailed;
                _webDashboardService.ConnectionRequestCallback = OnConnectionRequested;
                _webDashboardService.BatchManifestCallback = OnBatchManifestRequested;
                _webDashboardService.ResendRequestCallback = OnResendRequested;
                _webDashboardService.BatchTransferCompleted += OnBatchTransferCompleted;
                _webDashboardService.WebSessionEstablished += OnDeviceConnected;
                _webDashboardService.WebSessionTerminated += (clientId) => {
                    Dispatcher.UIThread.Post(() => {
                        if (_sendTarget != null && _sendTarget.Id == clientId) {
                            _sendTarget = null;
                            _sessionDevice = null;
                            ShowToast("Web client disconnected.");
                            if (DeviceSessionPanel.IsVisible) {
                                ShowPanel(HomePanel, "COMMAND CENTER", NavHomeBtn);
                            }
                        }
                    });
                };
                _webDashboardService.IsSessionActiveFilter = null;
                _webDashboardService.IsSessionActiveFilterEx = null;
                _webDashboardService.Start();
            }
            catch (Exception ex)
            {
                ShowToast($"Web Portal start failed: {ex.Message}");
            }

            // Broadcast our presence so receivers can see us
            _ = Task.Run(async () =>
            {
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

            // Set version labels dynamically
            AboutVersionText.Text = $"v{CurrentVersion}";
            SettingsVersionText.Text = $"Current Version: v{CurrentVersion}";

            UpdateEmptyState();
            NavHome_Click(this, new RoutedEventArgs());

            // First-run onboarding check
            var onboardingDone = _dbHelper.GetSetting("OnboardingCompleted", "");
            if (string.IsNullOrEmpty(onboardingDone))
            {
                if (WelcomeDeviceNameInput != null) WelcomeDeviceNameInput.Text = _localDevice.Name;
                if (FirstRunWelcomeModal != null)
                {
                    ShowWelcomeStep(1);
                    FirstRunWelcomeModal.IsVisible = true;
                }
            }

            // Network check — auto-start hotspot or auto-join if no network
            UpdateNetworkLabels();
            _ = Task.Run(TryAutoNetworkAsync);
        }
    }
}
