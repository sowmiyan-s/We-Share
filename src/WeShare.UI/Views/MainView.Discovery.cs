using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using WeShare.Core.Discovery;
using WeShare.Core.Models;
using WeShare.Core.Network;
using WeShare.Core.Services;
using WeShare.Core.Transfer;

namespace WeShare.UI.Views
{
    public partial class MainView : UserControl
    {
        // ── Device Preferences (Favorites & Custom Nicknames) ─────────────────

        private void LoadDevicePreferences()
        {
            try
            {
                string? favsJson = _dbHelper.GetSetting("FavoriteDevices", "[]");
                if (!string.IsNullOrEmpty(favsJson))
                {
                    var favList = System.Text.Json.JsonSerializer.Deserialize<List<string>>(favsJson);
                    if (favList != null)
                    {
                        _favoriteDeviceIds.Clear();
                        foreach (var id in favList) _favoriteDeviceIds.Add(id);
                    }
                }

                string? nicksJson = _dbHelper.GetSetting("DeviceNicknames", "{}");
                if (!string.IsNullOrEmpty(nicksJson))
                {
                    var nicks = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(nicksJson);
                    if (nicks != null)
                    {
                        _deviceNicknames.Clear();
                        foreach (var kvp in nicks) _deviceNicknames[kvp.Key] = kvp.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Prefs] Load failed: {ex.Message}");
            }
        }

        private void SaveDevicePreferences()
        {
            try
            {
                string favsJson = System.Text.Json.JsonSerializer.Serialize(_favoriteDeviceIds.ToList());
                _dbHelper.SetSetting("FavoriteDevices", favsJson);

                string nicksJson = System.Text.Json.JsonSerializer.Serialize(_deviceNicknames);
                _dbHelper.SetSetting("DeviceNicknames", nicksJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Prefs] Save failed: {ex.Message}");
            }
        }

        private void SortDevices()
        {
            var sorted = Devices.OrderByDescending(d => d.IsFavorite).ThenBy(d => d.DisplayName).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                int oldIndex = Devices.IndexOf(sorted[i]);
                if (oldIndex != i && oldIndex >= 0)
                {
                    Devices.Move(oldIndex, i);
                }
            }
        }

        private void ToggleFavorite_Click(object? sender, RoutedEventArgs e)
        {
            var device = (sender as Button)?.Tag as DeviceModel 
                      ?? ((sender as MenuItem)?.DataContext as DeviceModel) 
                      ?? ((sender as Control)?.DataContext as DeviceModel);
            if (device != null)
            {
                device.IsFavorite = !device.IsFavorite;
                if (device.IsFavorite) _favoriteDeviceIds.Add(device.Id);
                else _favoriteDeviceIds.Remove(device.Id);
                SaveDevicePreferences();
                SortDevices();
                ShowToast(device.IsFavorite ? $"Pinned '{device.DisplayName}' as favorite" : $"Unpinned '{device.DisplayName}'");
            }
        }

        // ── Peer Discovery & Radar Callbacks ──────────────────────────────────

        private async void RefreshDiscovery_Click(object sender, RoutedEventArgs e)
        {
            ShowToast("Refreshing radar...");
            Devices.Clear();
            await _discoveryService.BroadcastPresenceAsync();
            UpdateNetworkLabels();
        }

        private void OnDeviceDiscovered(DeviceModel device)
        {
            if (device == null) return;
            if (device.Id == _localDevice.Id) return;
            if (UdpDiscoveryService.IsOwnAddress(device.IpAddress)) return;
            if (string.Equals(device.Name, _localDevice.Name, StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrEmpty(device.IpAddress) || UdpDiscoveryService.IsOwnAddress(device.IpAddress)))
                return;

            Dispatcher.UIThread.Post(() => {
                if (_favoriteDeviceIds.Contains(device.Id)) device.IsFavorite = true;
                if (_deviceNicknames.TryGetValue(device.Id, out var nick)) device.CustomNickname = nick;

                var existing = Devices.FirstOrDefault(d => d.Id == device.Id);

                // Ensure we don't show the same device multiple times (match by unique ID)
                if (existing == null) 
                {
                    Devices.Add(device);
                    SortDevices();
                }
                else 
                {
                    // Update IP and properties if changed, and refresh last seen
                    existing.IpAddress = device.IpAddress;
                    existing.Port = device.Port;
                    existing.Name = device.Name;
                    existing.Type = device.Type;
                    existing.Role = device.Role;
                    existing.IsReceiver = device.IsReceiver;
                    existing.LastSeen = DateTime.Now;
                    if (_deviceNicknames.TryGetValue(existing.Id, out var existingNick)) existing.CustomNickname = existingNick;
                    existing.IsFavorite = _favoriteDeviceIds.Contains(existing.Id);
                }

                var targetDevice = existing ?? device;
                bool isReceiver = string.Equals(device.Role, "Receiver", StringComparison.OrdinalIgnoreCase) || device.IsReceiver;
                bool isSender = string.Equals(device.Role, "Sender", StringComparison.OrdinalIgnoreCase);

                if (isReceiver)
                {
                    var rExisting = ActiveReceivers.FirstOrDefault(d => d.Id == device.Id);
                    if (rExisting == null)
                    {
                        ActiveReceivers.Add(targetDevice);
                    }
                    else
                    {
                        rExisting.IpAddress = device.IpAddress;
                        rExisting.Port = device.Port;
                        rExisting.Name = device.Name;
                        rExisting.Role = device.Role;
                        rExisting.IsReceiver = device.IsReceiver;
                        rExisting.LastSeen = DateTime.Now;
                    }

                    var sExisting = ActiveSenders.FirstOrDefault(d => d.Id == device.Id);
                    if (sExisting != null) ActiveSenders.Remove(sExisting);
                }
                else if (isSender)
                {
                    var sExisting = ActiveSenders.FirstOrDefault(d => d.Id == device.Id);
                    if (sExisting == null)
                    {
                        ActiveSenders.Add(targetDevice);
                    }
                    else
                    {
                        sExisting.IpAddress = device.IpAddress;
                        sExisting.Port = device.Port;
                        sExisting.Name = device.Name;
                        sExisting.Role = device.Role;
                        sExisting.IsReceiver = device.IsReceiver;
                        sExisting.LastSeen = DateTime.Now;
                    }

                    var rExisting = ActiveReceivers.FirstOrDefault(d => d.Id == device.Id);
                    if (rExisting != null) ActiveReceivers.Remove(rExisting);
                }
                else // Idle or unknown
                {
                    var rExisting = ActiveReceivers.FirstOrDefault(d => d.Id == device.Id);
                    if (rExisting != null) ActiveReceivers.Remove(rExisting);

                    var sExisting = ActiveSenders.FirstOrDefault(d => d.Id == device.Id);
                    if (sExisting != null) ActiveSenders.Remove(sExisting);
                }

                UpdateEmptyState();
            });
        }

        private void OnDeviceLost(DeviceModel device)
        {
            if (device == null) return;
            string deviceId = device.Id;
            Dispatcher.UIThread.Post(() =>
            {
                var recv = ActiveReceivers.FirstOrDefault(d => d.Id == deviceId);
                if (recv != null) ActiveReceivers.Remove(recv);

                var snd = ActiveSenders.FirstOrDefault(d => d.Id == deviceId);
                if (snd != null) ActiveSenders.Remove(snd);

                var dev = Devices.FirstOrDefault(d => d.Id == deviceId);
                if (dev != null)
                {
                    Devices.Remove(dev);
                }

                UpdateEmptyState();
            });
        }

        // ── Autonomous Network & Wi-Fi Hotspot ─────────────────────────────────

        private async Task TryAutoNetworkAsync()
        {
            try
            {
                var ip = UdpDiscoveryService.GetLocalIp();
                bool hasRealIp = ip != "127.0.0.1" && !ip.StartsWith("169.254.");

                if (hasRealIp)
                {
                    // Already connected to a real Wi-Fi or Ethernet network.
                    // Do NOT auto-create a hotspot; preserve current connection and update UI.
                    Dispatcher.UIThread.Post(UpdateNetworkLabels);
                    return;
                }

                // Field / Desert Mode: No active router or network found
                _wifiConnector = new WifiConnectorService();

                // Step 1 — Check if a WeShare peer hotspot is already visible nearby to join
                bool found = await _wifiConnector.IsWeShareHotspotVisibleAsync();
                if (found)
                {
                    var (ok, _) = await _wifiConnector.AutoConnectToWeShareAsync();
                    if (ok)
                    {
                        await Task.Delay(1500); // let DHCP settle
                        Dispatcher.UIThread.Post(() => {
                            UpdateNetworkLabels();
                            ShowToast("Auto-connected to nearby WeShare Hotspot!");
                        });

                        for (int i = 0; i < 4; i++)
                        {
                            await _discoveryService.BroadcastPresenceAsync();
                            await Task.Delay(800);
                        }
                        return;
                    }
                }

                // Step 2 — No existing network or hotspot found: Automatically host a Direct Hotspot
                _hotspotService = new HotspotService();
                if (await _hotspotService.IsSupportedAsync())
                {
                    var (startedHost, _) = await _hotspotService.StartAsync();
                    if (startedHost)
                    {
                        await Task.Delay(1000);
                        Dispatcher.UIThread.Post(() => {
                            UpdateNetworkLabels();
                            ShowToast("No network found — Autonomous Hotspot activated at 192.168.137.1");
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AutoNetwork] Error: {ex.Message}");
            }
        }

        /// <summary>Updates the existing network info labels in-place.</summary>
        private async void UpdateNetworkLabels()
        {
            var ip = UdpDiscoveryService.GetLocalIp();
            bool hasRealIp = ip != "127.0.0.1" && !ip.StartsWith("169.254.");
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
                int webPort = _webDashboardService?.Port ?? 8080;
                string webUrl = $"http://{hostIp}:{webPort}";
                HomeWebPortalText.Text   = webUrl;
                GenerateQrBitmap(webUrl);

                if (StartHotspotBtn != null)
                {
                    StartHotspotBtn.Content = isHotspotRunning ? "Stop Hotspot" : "Direct Hotspot";
                }

                if (WebPortalNetworkInfo != null)
                {
                    WebPortalNetworkInfo.Text = isHotspotRunning 
                        ? "• Direct Hotspot Active (192.168.137.1)" 
                        : (hasRealIp ? $"• {ssid} ({ip}:{webPort})" : "• Offline Standalone Mode");
                }

                if (isHotspotRunning && hasRealIp && ip != _hotspotService!.HotspotIp)
                {
                    string wifiWebUrl = $"http://{ip}:{webPort}";
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
            int activeWebPort = _webDashboardService?.Port ?? 8080;
            _ = Task.Run(() =>
            {
                if (_captivePortalService != null)
                {
                    _captivePortalService.Stop();
                    _captivePortalService = null;
                }

                // Captive portal (ports 80 & 53) should only run when actively hosting a Wi-Fi Hotspot
                if (isHotspotRunning && System.Net.IPAddress.TryParse(hostIpStr, out var parsedIp) && !System.Net.IPAddress.IsLoopback(parsedIp) && parsedIp.ToString() != "127.0.0.1")
                {
                    _captivePortalService = new CaptivePortalService(parsedIp, activeWebPort);
                    _captivePortalService.Start();
                }
            });
        }

        private async void ManualStartHotspot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_hotspotService != null && _hotspotService.IsRunning)
                {
                    var (stopped, _) = await _hotspotService.StopAsync();
                    ShowToast("Wi-Fi Hotspot stopped");
                    UpdateNetworkLabels();
                }
                else
                {
                    _hotspotService ??= new HotspotService();
                    var (started, ip) = await _hotspotService.StartAsync();
                    if (started)
                    {
                        ShowToast($"Hotspot active at {ip} (SSID: WeShare, Pass: weshare1)");
                        UpdateNetworkLabels();
                    }
                    else
                    {
                        ShowToast($"Could not start hotspot: {ip}");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowToast($"Hotspot error: {ex.Message}");
            }
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
            
            // Add a virtual device for this IP, marking it ready to receive
            var device = new DeviceModel { Name = $"Manual Peer ({ip})", IpAddress = ip, Port = 45679, Role = "Receiver", IsReceiver = true };
            if (!Devices.Any(d => IsSameIpAddress(d.IpAddress, ip))) Devices.Add(device);

            _sendTarget = device;
            ShowToast($"Connecting to {ip}:45679...");
            ShowPanel(SendFilesPanel, "SEND FILES", null);
        }

        private static bool IsSameIpAddress(string? ip1, string? ip2)
        {
            if (string.IsNullOrEmpty(ip1) || string.IsNullOrEmpty(ip2))
                return false;

            if (ip1.Equals(ip2, StringComparison.OrdinalIgnoreCase))
                return true;

            if (System.Net.IPAddress.TryParse(ip1, out var parsed1) && System.Net.IPAddress.TryParse(ip2, out var parsed2))
            {
                if (System.Net.IPAddress.IsLoopback(parsed1) && System.Net.IPAddress.IsLoopback(parsed2))
                    return true;

                return parsed1.MapToIPv4().Equals(parsed2.MapToIPv4());
            }

            return false;
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

        // ── Transfer Acceptance Handshake ─────────────────────────────────────

        private TaskCompletionSource<bool>? _acceptTcs;

        private async Task<bool> OnTransferRequested(FileTransferState state)
        {
            bool isBatchAccepted = !string.IsNullOrEmpty(state.BatchId) && state.BatchId == _activeAcceptedBatchId;
            bool isSame = (IsSameIpAddress(_lastAcceptedIp, state.RemoteIp) && (DateTime.Now - _lastAcceptedTime).TotalSeconds < 120);

            // 1. Auto-Accept Logic (Settings, active batch, active session, or dedicated Receive Mode)
            if (_autoAcceptAllTransfers || isSame || isBatchAccepted || _localDevice.IsReceiver)
            {
                _lastAcceptedIp = state.RemoteIp;
                _lastAcceptedTime = DateTime.Now;
                return true;
            }

            // 2. UI Request Queueing
            await _uiRequestLock.WaitAsync();
            try
            {
                // Re-evaluate active session and auto-accept conditions inside the lock
                var currentActiveIpOrId = GetActiveSessionDeviceIpOrId();
                bool currentIsSame = false;
                if (currentActiveIpOrId != null)
                {
                    currentIsSame = (IsSameIpAddress(state.RemoteIp, currentActiveIpOrId) || state.FileId == currentActiveIpOrId);
                }

                if (_autoAcceptAllTransfers || currentIsSame || isBatchAccepted || (IsSameIpAddress(_lastAcceptedIp, state.RemoteIp) && (DateTime.Now - _lastAcceptedTime).TotalSeconds < 120) || _localDevice.IsReceiver)
                {
                    // Extend the auto-accept session since it is accepted
                    _lastAcceptedIp = state.RemoteIp;
                    _lastAcceptedTime = DateTime.Now;
                    return true;
                }

                _acceptTcs = new TaskCompletionSource<bool>();
                _platformService.ShowSystemToast("Incoming File Request", $"{state.PeerName} wants to send {state.FileName} ({FileTransferState.FormatBytes(state.TotalBytes)})");
                PlaySound("request");
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
            _acceptTcs?.TrySetResult(true);
        }

        private void RejectTransfer_Click(object sender, RoutedEventArgs e)
        {
            AcceptRejectPanel.IsVisible = false;
            _acceptTcs?.TrySetResult(false);
        }

        // ── Mutual Connection Management (SHAREit / Quick Share Model) ────────

        private TaskCompletionSource<bool>? _connectionTcs;

        private async Task<bool> OnConnectionRequested(ConnectionRequest req)
        {
            return await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                _connectionTcs?.TrySetResult(false);
                _connectionTcs = new TaskCompletionSource<bool>();

                if (ConnectionRequestPeerName != null) ConnectionRequestPeerName.Text = req.PeerName;
                if (ConnectionRequestDetails != null) ConnectionRequestDetails.Text = $"{req.PeerType} • {req.PeerIp}";
                if (ConnectionRequestRoleDesc != null)
                {
                    ConnectionRequestRoleDesc.Text = req.Role == "Receiver"
                        ? "wants to connect to receive files from you."
                        : "wants to connect to share files with you.";
                }

                if (ConnectionRequestPanel != null) ConnectionRequestPanel.IsVisible = true;
                PlaySound("request");

                bool accepted = await _connectionTcs.Task;
                if (ConnectionRequestPanel != null) ConnectionRequestPanel.IsVisible = false;
                return accepted;
            });
        }

        private void AcceptConnection_Click(object sender, RoutedEventArgs e)
        {
            if (ConnectionRequestPanel != null) ConnectionRequestPanel.IsVisible = false;
            _connectionTcs?.TrySetResult(true);
        }

        private void DeclineConnection_Click(object sender, RoutedEventArgs e)
        {
            if (ConnectionRequestPanel != null) ConnectionRequestPanel.IsVisible = false;
            _connectionTcs?.TrySetResult(false);
        }

        private void ConnectToPeer_Click(object sender, RoutedEventArgs e)
        {
            var device = (sender as Button)?.DataContext as DeviceModel
                      ?? ((sender as Control)?.DataContext as DeviceModel);
            if (device == null) return;

            InitiateConnectionToDevice(device);
        }

        public void InitiateConnectionToDevice(DeviceModel device)
        {
            _sendTarget = device;
            _sessionDevice = device;
            string role = _localDevice.IsReceiver ? "Receiver" : "Sender";
            ShowToast($"Sending connection request to '{device.DisplayName}'...");

            if (device.Type == "Web Client")
            {
                _webDashboardService?.ConnectClientFromHost(device.Id, _localDevice.DisplayName);
                ShowToast($"Connection request sent to '{device.DisplayName}' via Web Portal");
            }
            else
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var resp = await _transferManager.SendConnectRequestAsync(device.IpAddress, device.Port, role);
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (resp.Accepted)
                            {
                                OnDeviceConnected(device);
                            }
                            else
                            {
                                ShowToast($"'{device.DisplayName}' declined the connection request.");
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            ShowToast($"Connection failed: {ex.Message}");
                        });
                    }
                });
            }
        }

        private void OnDeviceConnected(DeviceModel peer)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _sendTarget = peer;
                _sessionDevice = peer;
                peer.ConnectionStatus = "Connected";
                if (SessionDeviceTitle != null) SessionDeviceTitle.Text = peer.DisplayName;
                if (SessionDeviceSub != null) SessionDeviceSub.Text = $"{peer.Type} • {peer.IpAddress}";
                PlaySound("success");
                ShowToast($"Connected with {peer.DisplayName}!");

                if (_localDevice.Role == "Sender" || (SendDiscoveryPanel != null && SendDiscoveryPanel.IsVisible))
                {
                    if (SendQueue.Count > 0)
                    {
                        StartSendSession(peer);
                    }
                    else
                    {
                        ShowPanel(SendFilesPanel, "SEND FILES", NavSendBtn);
                    }
                }
                else if (_localDevice.Role == "Receiver")
                {
                    ShowPanel(ReceiveModePanel, "RECEIVE MODE", NavReceiveBtn);
                }
            });
        }

        private void OnDeviceDisconnected(DeviceModel peer)
        {
            Dispatcher.UIThread.Post(() =>
            {
                peer.ConnectionStatus = "Disconnected";
                ShowToast($"{peer.DisplayName} disconnected.");
                if (_sendTarget != null && _sendTarget.Id == peer.Id)
                {
                    _sendTarget = null;
                    _sessionDevice = null;
                }
            });
        }

        private async void DisconnectSession_Click(object sender, RoutedEventArgs e)
        {
            if (_sendTarget != null)
            {
                var target = _sendTarget;
                _sendTarget = null;
                _sessionDevice = null;
                if (target.Type == "Web Client")
                {
                    _webDashboardService?.DisconnectWebClient(target.Id);
                }
                else
                {
                    await _transferManager.SendDisconnectAsync(target.IpAddress, target.Port);
                }
                ShowToast($"Disconnected from {target.DisplayName}.");
            }
            ShowPanel(HomePanel, "COMMAND CENTER", NavHomeBtn);
        }
    }
}
