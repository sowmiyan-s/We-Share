using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using WeShare.Core.Models;
using WeShare.Core.Services;
using WeShare.Core.Transfer;

namespace WeShare.UI.Views
{
    public partial class MainView : UserControl
    {
        // ── QR Code Generation ────────────────────────────────────────────────

        private void GenerateQrBitmap(string url)
        {
            try
            {
                var pngBytes = QrCodeService.GenerateQrCodePng(url);
                using var ms = new MemoryStream(pngBytes);
                _qrBitmap = new Avalonia.Media.Imaging.Bitmap(ms);
                if (HomeQrImage != null)
                    HomeQrImage.Source = _qrBitmap;
            }
            catch { /* QR generation is non-critical */ }
        }

        private void ToggleQrCode_Click(object? sender, RoutedEventArgs e)
        {
            if (WebPortalQrDrawer != null)
            {
                WebPortalQrDrawer.IsVisible = !WebPortalQrDrawer.IsVisible;
            }
        }

        private void OpenWebPortalBrowser_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                string url = HomeWebPortalText?.Text ?? "";
                if (!string.IsNullOrEmpty(url))
                {
                    Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                ShowToast($"Could not open browser: {ex.Message}");
            }
        }

        // ── Web Client Discovery & Session Events ─────────────────────────────

        private void OnWebClientConnected(string type, string remoteIp)
        {
            // Silent client registration - avoid spammy popup toasts
        }

        private void OnWebClientConnectedEx(WebDashboardService.WebClientInfo client)
        {
            if (client == null) return;
            if (client.ClientId == _localDevice.Id) return;

            Dispatcher.UIThread.Post(() => {
                string role = client.Role ?? "Receiver";
                var existing = Devices.FirstOrDefault(d => d.Id == client.ClientId);
                if (existing != null)
                {
                    existing.Name = client.Name;
                    existing.IpAddress = client.IpAddress;
                    existing.Role = role;
                    existing.IsReceiver = (role == "Receiver");
                    existing.LastSeen = DateTime.Now;
                }
                else
                {
                    existing = new DeviceModel
                    {
                        Id = client.ClientId,
                        Name = client.Name,
                        IpAddress = client.IpAddress,
                        Type = "Web Client",
                        Role = role,
                        IsReceiver = (role == "Receiver"),
                        LastSeen = DateTime.Now,
                        Port = 8080
                    };
                    Devices.Add(existing);
                }

                if (role == "Receiver")
                {
                    if (!ActiveReceivers.Any(d => d.Id == client.ClientId))
                        ActiveReceivers.Add(existing);
                    var s = ActiveSenders.FirstOrDefault(d => d.Id == client.ClientId);
                    if (s != null) ActiveSenders.Remove(s);
                }
                else if (role == "Sender")
                {
                    if (!ActiveSenders.Any(d => d.Id == client.ClientId))
                        ActiveSenders.Add(existing);
                    var r = ActiveReceivers.FirstOrDefault(d => d.Id == client.ClientId);
                    if (r != null) ActiveReceivers.Remove(r);
                }
                else
                {
                    var r = ActiveReceivers.FirstOrDefault(d => d.Id == client.ClientId);
                    if (r != null) ActiveReceivers.Remove(r);
                    var s = ActiveSenders.FirstOrDefault(d => d.Id == client.ClientId);
                    if (s != null) ActiveSenders.Remove(s);
                }

                UpdateEmptyState();
                if (WebSharedPanel != null && WebSharedPanel.IsVisible)
                {
                    UpdateWebSharedClientsList();
                }
            });
        }

        private void OnWebClientRoleChanged(string clientId, string role)
        {
            Dispatcher.UIThread.Post(() => {
                var existing = Devices.FirstOrDefault(d => d.Id == clientId);
                if (existing != null)
                {
                    existing.Role = role;
                    existing.IsReceiver = (role == "Receiver");
                    existing.LastSeen = DateTime.Now;

                    if (role == "Receiver")
                    {
                        if (!ActiveReceivers.Any(d => d.Id == clientId))
                            ActiveReceivers.Add(existing);
                        var s = ActiveSenders.FirstOrDefault(d => d.Id == clientId);
                        if (s != null) ActiveSenders.Remove(s);
                    }
                    else if (role == "Sender")
                    {
                        if (!ActiveSenders.Any(d => d.Id == clientId))
                            ActiveSenders.Add(existing);
                        var r = ActiveReceivers.FirstOrDefault(d => d.Id == clientId);
                        if (r != null) ActiveReceivers.Remove(r);
                    }
                    else
                    {
                        var r = ActiveReceivers.FirstOrDefault(d => d.Id == clientId);
                        if (r != null) ActiveReceivers.Remove(r);
                        var s = ActiveSenders.FirstOrDefault(d => d.Id == clientId);
                        if (s != null) ActiveSenders.Remove(s);
                    }

                    UpdateEmptyState();
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
                    var r = ActiveReceivers.FirstOrDefault(d => d.Id == clientId);
                    if (r != null) ActiveReceivers.Remove(r);
                    var s = ActiveSenders.FirstOrDefault(d => d.Id == clientId);
                    if (s != null) ActiveSenders.Remove(s);
                    UpdateEmptyState();
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

        private void OnWebClientHeartbeat(string clientId)
        {
            Dispatcher.UIThread.Post(() => {
                var existing = Devices.FirstOrDefault(d => d.Id == clientId);
                if (existing != null)
                {
                    existing.LastSeen = DateTime.Now;
                }
            });
        }

        // ── Web File Sharing & Staging ────────────────────────────────────────

        private void OnWebFileShared(string clientId, string clientName, string filePath, long size)
        {
            Dispatcher.UIThread.Post(() => {
                var staged = new StagedWebFile
                {
                    ClientId = clientId,
                    ClientName = clientName,
                    FilePath = filePath,
                    Size = size
                };
                StagedWebFiles.Add(staged);
                UpdateWebSharedFilesList();
                UpdateWebSharedClientsList();
                ShowToast($"Received file '{Path.GetFileName(filePath)}' from {clientName}");
            });
        }

        private async Task<bool> OnWebFileSharedCallback(FileTransferState state)
        {
            // Prompt user using the standard dialog popup
            bool accepted = await OnTransferRequested(state);

            if (!accepted)
            {
                Dispatcher.UIThread.Post(() => {
                    ShowToast($"Declined incoming web transfer: {state.FileName}");
                });
            }

            return accepted;
        }

        private void UpdateWebSharedClientsList()
        {
            var webClients = Devices.Where(d => d.Type == "Web Client").ToList();
            if (WebClientsListBox != null) WebClientsListBox.ItemsSource = webClients;
            if (WebClientsRailCountText != null)
                WebClientsRailCountText.Text = $"{webClients.Count} device{(webClients.Count == 1 ? "" : "s")} connected";
            if (SidebarWebClientBadge != null)
                SidebarWebClientBadge.IsVisible = webClients.Count > 0;
            if (SidebarWebClientBadgeCount != null)
                SidebarWebClientBadgeCount.Text = webClients.Count.ToString();

            if (webClients.Count == 0)
            {
                _selectedWebClient = null;
                if (WebDeviceSessionView != null) WebDeviceSessionView.IsVisible = false;
                if (NoWebClientsCard != null) NoWebClientsCard.IsVisible = true;
            }
            else
            {
                if (WebDeviceSessionView != null) WebDeviceSessionView.IsVisible = true;
                if (NoWebClientsCard != null) NoWebClientsCard.IsVisible = false;

                if (_selectedWebClient == null || !webClients.Any(c => c.Id == _selectedWebClient.Id))
                {
                    _selectedWebClient = webClients.FirstOrDefault();
                    if (WebClientsListBox != null) WebClientsListBox.SelectedItem = _selectedWebClient;
                }
                UpdateSelectedWebClientUI();
            }
            UpdateWebSharedFilesList();
        }

        private void UpdateSelectedWebClientUI()
        {
            if (_selectedWebClient == null) return;
            if (SelectedWebClientName != null) SelectedWebClientName.Text = _selectedWebClient.DisplayName;
            if (SelectedWebClientDetails != null) 
                SelectedWebClientDetails.Text = $"IP: {_selectedWebClient.IpAddress} • Web Portal Client • Connected";
            if (SelectedWebClientIcon != null)
            {
                string lname = _selectedWebClient.Name.ToLower();
                string key = (lname.Contains("iphone") || lname.Contains("android") || lname.Contains("mobile") || lname.Contains("phone")) ? "IconMobile" : "IconDevice";
                if (this.TryFindResource(key, out var geom) && geom is Avalonia.Media.Geometry g)
                {
                    SelectedWebClientIcon.Data = g;
                }
            }
            UpdateWebSendQueueUI();
        }

        private void WebClientsListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (WebClientsListBox.SelectedItem is DeviceModel dev)
            {
                _selectedWebClient = dev;
                _sessionDevice = dev;
                UpdateSelectedWebClientUI();
                UpdateWebSharedFilesList();
            }
            else
            {
                UpdateWebSharedFilesList();
            }
        }

        private void UpdateWebSharedFilesList()
        {
            if (_selectedWebClient == null)
            {
                if (WebSharedFilesList != null) WebSharedFilesList.ItemsSource = null;
                if (WebFilesCountText != null) WebFilesCountText.Text = "0 files";
                if (WebFilesEmptyLabel != null) WebFilesEmptyLabel.IsVisible = true;
                if (WebAcceptAllBtn != null) WebAcceptAllBtn.IsEnabled = false;
                return;
            }

            var clientFiles = StagedWebFiles.Where(f => f.ClientId == _selectedWebClient.Id || f.ClientName == _selectedWebClient.Name).ToList();
            if (WebSharedFilesList != null) WebSharedFilesList.ItemsSource = clientFiles;
            if (WebFilesCountText != null) WebFilesCountText.Text = $"{clientFiles.Count} file{(clientFiles.Count == 1 ? "" : "s")}";
            if (WebFilesEmptyLabel != null) WebFilesEmptyLabel.IsVisible = clientFiles.Count == 0;
            if (WebAcceptAllBtn != null) WebAcceptAllBtn.IsEnabled = clientFiles.Count > 0;
        }

        private async void WebAddFiles_Click(object? sender, RoutedEventArgs e)
        {
            var files = await PickFilesAsync();
            foreach (var f in files)
            {
                if (!WebClientSendQueue.Any(q => q.Path == f.Path))
                    WebClientSendQueue.Add(f);
            }
            UpdateWebSendQueueUI();
        }

        private async void WebAddFolder_Click(object? sender, RoutedEventArgs e)
        {
            var files = await PickFolderAsync();
            foreach (var f in files)
            {
                if (!WebClientSendQueue.Any(q => q.Path == f.Path))
                    WebClientSendQueue.Add(f);
            }
            UpdateWebSendQueueUI();
        }

        private async void WebQuickSendFiles_Click(object? sender, RoutedEventArgs e)
        {
            if (_selectedWebClient == null)
            {
                ShowToast("Please select a device from the list above first");
                return;
            }

            var files = await PickFilesAsync();
            if (files == null || files.Count == 0) return;

            var paths = files.Select(f => f.Path).ToList();
            bool ok = paths.Count > 1
                ? (_webDashboardService?.ShareMultipleForWebClient(_selectedWebClient.Id, paths) ?? false)
                : (_webDashboardService?.ShareForWebClient(_selectedWebClient.Id, paths[0]) ?? false);

            if (ok)
            {
                ShowToast($"Sent {paths.Count} file(s) to '{_selectedWebClient.DisplayName}'. Browser receiving notification...");
                PlaySound("send");
            }
            else
            {
                ShowToast("Failed to send files. Web client may have disconnected.");
            }
        }

        private async void WebQuickSendFolder_Click(object? sender, RoutedEventArgs e)
        {
            if (_selectedWebClient == null)
            {
                ShowToast("Please select a device from the list above first");
                return;
            }

            var files = await PickFolderAsync();
            if (files == null || files.Count == 0) return;

            var paths = files.Select(f => f.Path).ToList();
            bool ok = paths.Count > 1
                ? (_webDashboardService?.ShareMultipleForWebClient(_selectedWebClient.Id, paths) ?? false)
                : (_webDashboardService?.ShareForWebClient(_selectedWebClient.Id, paths[0]) ?? false);

            if (ok)
            {
                ShowToast($"Sent {paths.Count} file(s) from folder to '{_selectedWebClient.DisplayName}'. Browser receiving notification...");
                PlaySound("send");
            }
            else
            {
                ShowToast("Failed to send files. Web client may have disconnected.");
            }
        }

        private void WebClearSendQueue_Click(object? sender, RoutedEventArgs e)
        {
            WebClientSendQueue.Clear();
            UpdateWebSendQueueUI();
        }

        private void WebRemoveStagedItem_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is QueueItem item)
            {
                WebClientSendQueue.Remove(item);
                UpdateWebSendQueueUI();
            }
        }

        private void UpdateWebSendQueueUI()
        {
            if (WebSendStagedCountText != null)
                WebSendStagedCountText.Text = $"{WebClientSendQueue.Count} file{(WebClientSendQueue.Count == 1 ? "" : "s")} ready";
            if (WebSendQueueEmptyLabel != null)
                WebSendQueueEmptyLabel.IsVisible = WebClientSendQueue.Count == 0;
            if (WebSendToDeviceBtn != null)
            {
                string targetName = _selectedWebClient != null ? _selectedWebClient.DisplayName : "Device";
                WebSendToDeviceBtn.Content = $"Send {WebClientSendQueue.Count} File(s) to {targetName}";
                WebSendToDeviceBtn.IsEnabled = WebClientSendQueue.Count > 0 && _selectedWebClient != null;
            }
        }

        private void WebSendToDevice_Click(object? sender, RoutedEventArgs e)
        {
            if (_selectedWebClient == null)
            {
                ShowToast("Please select a device from the rail above");
                return;
            }
            if (WebClientSendQueue.Count == 0)
            {
                ShowToast("Please add files to send first");
                return;
            }

            var paths = WebClientSendQueue.Select(q => q.Path).ToList();
            bool ok = paths.Count > 1
                ? (_webDashboardService?.ShareMultipleForWebClient(_selectedWebClient.Id, paths) ?? false)
                : (_webDashboardService?.ShareForWebClient(_selectedWebClient.Id, paths[0]) ?? false);

            if (ok)
            {
                ShowToast($"Pushed {paths.Count} file(s) to '{_selectedWebClient.DisplayName}'. Browser downloading...");
                WebClientSendQueue.Clear();
                UpdateWebSendQueueUI();
            }
            else
            {
                ShowToast($"Failed to push files. Web client may have disconnected.");
            }
        }

        private async void WebAcceptAllFiles_Click(object? sender, RoutedEventArgs e)
        {
            if (_selectedWebClient == null) return;
            var clientFiles = StagedWebFiles.Where(f => f.ClientId == _selectedWebClient.Id || f.ClientName == _selectedWebClient.Name).ToList();
            if (clientFiles.Count == 0)
            {
                ShowToast("No files to accept");
                return;
            }

            int count = 0;
            foreach (var staged in clientFiles)
            {
                try
                {
                    if (File.Exists(staged.FilePath))
                    {
                        string destPath = GetUniqueFilePath(_saveDirectory, staged.FileName);
                        File.Move(staged.FilePath, destPath);
                        var state = new FileTransferState
                        {
                            FileName = Path.GetFileName(destPath),
                            FilePath = destPath,
                            TotalBytes = staged.Size,
                            TransferredBytes = staged.Size,
                            Status = TransferStatus.Done,
                            Direction = TransferDirection.Received,
                            PeerName = staged.ClientName,
                            Timestamp = DateTime.UtcNow
                        };
                        await _dbHelper.SaveTransferAsync(state);
                        ReceivedFiles.Insert(0, state);
                        count++;
                    }
                }
                catch { }
                StagedWebFiles.Remove(staged);
            }
            UpdateWebSharedFilesList();
            ShowToast($"Accepted and saved {count} file(s) from {_selectedWebClient.DisplayName}");
            PlaySound("complete");
        }

        private void DisconnectCurrentWebClient_Click(object? sender, RoutedEventArgs e)
        {
            if (_selectedWebClient != null && !string.IsNullOrEmpty(_selectedWebClient.Id))
            {
                _webDashboardService?.DisconnectWebClient(_selectedWebClient.Id);
                Devices.Remove(_selectedWebClient);
                UpdateWebSharedClientsList();
                ShowToast($"Disconnected {_selectedWebClient.DisplayName}");
            }
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
                        // File was already automatically finalized into downloads by OnWebTransferCompleted
                        var existing = ReceivedFiles.FirstOrDefault(f => f.FileName == stagedFile.FileName);
                        if (existing != null)
                        {
                            ShowToast($"File already saved to Downloads: {existing.FileName}");
                        }
                        else
                        {
                            ShowToast($"File received: {stagedFile.FileName}");
                        }
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

        // ── Dedicated Device Session Support (Backwards Compatibility) ────────

        private DeviceModel? _sessionDevice;

        public void OpenDeviceSession(DeviceModel device)
        {
            _sessionDevice = device;
            _selectedWebClient = device;
            UpdateSelectedWebClientUI();
            UpdateWebSharedFilesList();
        }

        private void UpdateDeviceSessionFiles()
        {
            UpdateWebSharedFilesList();
        }

        private void SessionSendFiles_Click(object? sender, RoutedEventArgs e)
        {
            WebAddFiles_Click(sender, e);
        }

        private void SessionSendFolder_Click(object? sender, RoutedEventArgs e)
        {
            WebAddFolder_Click(sender, e);
        }

        private void SelectWebClient_Click(object? sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is DeviceModel dev)
            {
                OpenDeviceSession(dev);
            }
        }
    }
}
