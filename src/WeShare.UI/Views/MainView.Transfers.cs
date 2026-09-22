using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using WeShare.Core.Models;
using WeShare.Core.Network;
using WeShare.Core.Transfer;

namespace WeShare.UI.Views
{
    public partial class MainView : UserControl
    {
        private bool _isSending = false;
        private DeviceModel? _lastDeclinedDevice;

        // ── Target Selection & Dispatch ───────────────────────────────────────

        private void SendFile_Click(object sender, RoutedEventArgs e)
        {
            var device = (sender as Button)?.DataContext as DeviceModel
                      ?? ((sender as MenuItem)?.DataContext as DeviceModel)
                      ?? ((sender as Control)?.DataContext as DeviceModel);
            if (device == null) return;

            if (SendQueue.Count > 0)
            {
                _sendTarget = device;
                UpdateSendTargetUI();
                StartSendSession(device);
            }
            else
            {
                SelectSendTarget(device);
            }
        }

        public void SelectSendTarget(DeviceModel device)
        {
            _sendTarget = device;
            UpdateSendTargetUI();

            ShowPanel(SendFilesPanel, "SEND FILES", NavSendBtn);
            SendStepWizard.IsVisible = true;
            Step1Indicator.Foreground = SolidColorBrush.Parse("#7C3AED");
            Step2Indicator.Foreground = SolidColorBrush.Parse("#7C3AED");

            if (SendQueue.Count == 0)
            {
                ShowToast($"Selected '{device.DisplayName}'. Add or drop files to send.");
            }
            else
            {
                ShowToast($"Ready to send {SendQueue.Count} file(s) to '{device.DisplayName}'.");
            }
        }

        private void ClearSendTarget_Click(object? sender, RoutedEventArgs e)
        {
            _sendTarget = null;
            UpdateSendTargetUI();
            ShowToast("Cleared recipient selection");
        }

        private void SendToSelectedTarget_Click(object? sender, RoutedEventArgs e)
        {
            if (_sendTarget == null)
            {
                NavSendDiscovery_Click(sender ?? this, e);
                return;
            }

            if (SendQueue.Count == 0)
            {
                ShowToast("Please add files to send first");
                return;
            }

            if (!string.IsNullOrEmpty(_sendTarget.Ssid))
            {
                ShowToast($"Connecting to WeShare hotspot \"{_sendTarget.Ssid}\"...");
                var target = _sendTarget;
                _ = Task.Run(async () => {
                    bool ok = await _platformService.ConnectToWifiAsync(target.Ssid, target.Password ?? "");
                    if (ok)
                    {
                        await Task.Delay(1500); // let DHCP settle
                        Dispatcher.UIThread.Post(() => {
                            ShowToast("Connected to WeShare hotspot! Initiating transfer...");
                            StartSendSession(target);
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

            StartSendSession(_sendTarget);
        }

        private void UpdateSendTargetUI()
        {
            if (SendTargetCard == null) return;

            if (_sendTarget != null)
            {
                SendTargetCard.IsVisible = true;
                if (SendTargetName != null) SendTargetName.Text = _sendTarget.DisplayName;
                if (SendTargetDetails != null) SendTargetDetails.Text = $"{_sendTarget.Type} • {_sendTarget.IpAddress}";
                if (SendTargetIcon != null)
                {
                    string key = _sendTarget.Type?.ToLower() switch
                    {
                        "phone" or "android" or "ios" or "mobile" => "IconMobile",
                        "web client" or "web" => "IconGlobe",
                        _ => "IconDevice"
                    };
                    if (this.TryFindResource(key, out var geom) && geom is Avalonia.Media.Geometry g)
                    {
                        SendTargetIcon.Data = g;
                    }
                }

                if (ChooseRecipientBtn != null)
                {
                    ChooseRecipientBtn.Classes.Set("PrimaryBtn", false);
                    ChooseRecipientBtn.Classes.Set("GhostBtn", true);
                    ChooseRecipientBtn.IsVisible = true;
                }
                if (SendToTargetBtn != null)
                {
                    SendToTargetBtn.IsVisible = true;
                    SendToTargetBtn.Content = $"Send {SendQueue.Count} File(s) to {_sendTarget.DisplayName} →";
                }
            }
            else
            {
                SendTargetCard.IsVisible = false;
                if (ChooseRecipientBtn != null)
                {
                    ChooseRecipientBtn.Classes.Set("PrimaryBtn", true);
                    ChooseRecipientBtn.Classes.Set("GhostBtn", false);
                    ChooseRecipientBtn.IsVisible = true;
                }
                if (SendToTargetBtn != null)
                {
                    SendToTargetBtn.IsVisible = false;
                }
            }
        }

        private void StartSendSession(DeviceModel device)
        {
            var itemsToSend = SendQueue.ToList();
            if (itemsToSend.Count == 0 && _lastDeclinedItems.TryGetValue(device.Id ?? device.IpAddress, out var saved))
            {
                itemsToSend = saved.ToList();
            }

            if (itemsToSend.Count == 0)
            {
                ShowToast("Please add files to send first");
                ShowPanel(SendFilesPanel, "SEND FILES", NavSendBtn);
                return;
            }

            // Snapshot staged items for this device send session
            SendQueue.Clear();
            UpdateQueueUI();

            TransferDeclinedCard.IsVisible = false;
            _sendTarget = device;
            SendProgressBorder.IsVisible = true;
            _isSending = true;
            UpdateTransfersVisibility();

            _isTransferInProgress = true;
            _activeTransferIsSender = true;
            _activeTransferPeerName = device.DisplayName;
            _activeBatchTotalBytes = itemsToSend.Sum(x => x.Size);
            _activeBatchTransferredBytes = 0;
            _activeBatchTotalCount = itemsToSend.Count;
            _activeBatchCompletedCount = 0;
            _transferStartTime = DateTime.UtcNow;

            ActiveTransferBatchFiles.Clear();
            foreach (var item in itemsToSend)
            {
                ActiveTransferBatchFiles.Add(new ActiveBatchFileItem
                {
                    FileName = item.Name,
                    FileSize = item.Size,
                    RelativePath = item.RelativePath,
                    Status = TransferFileItemStatus.Waiting,
                    StatusText = "Waiting in queue"
                });
            }
            if (ActiveTransferBatchFiles.Count > 0)
            {
                ActiveTransferBatchFiles[0].Status = TransferFileItemStatus.Transferring;
                ActiveTransferBatchFiles[0].StatusText = "Connecting...";
            }

            UpdateActiveTransferViewInfo(true, device.DisplayName, device.IpAddress, itemsToSend.Count, _activeBatchTotalBytes);
            ShowPanel(ActiveTransferPanel, "ACTIVE TRANSFER");

            _ = Task.Run(() => ProcessSendSessionAsync(device, itemsToSend));
        }

        private async Task ProcessSendSessionAsync(DeviceModel device, System.Collections.Generic.List<QueueItem> items)
        {
            try
            {
                string batchId = Guid.NewGuid().ToString("N");

                // If sending to a desktop peer, negotiate batch manifest first so receiver gets checkbox manifest
                if (device.Type != "Web Client" && items.Count > 0)
                {
                    try
                    {
                        var manifestItems = items.Select((item, idx) => new BatchFileItem
                        {
                            FileId = "bf_" + idx + "_" + Guid.NewGuid().ToString("N")[..8],
                            FileName = item.Name,
                            RelativePath = string.IsNullOrEmpty(item.RelativePath) ? item.Name : item.RelativePath,
                            FileSize = item.Size
                        }).ToList();

                        var manifest = new BatchManifest
                        {
                            BatchId = batchId,
                            SenderName = _localDevice.DisplayName,
                            SenderIp = _localDevice.IpAddress,
                            Files = manifestItems,
                            TotalBytes = items.Sum(x => x.Size)
                        };

                        var resp = await _transferManager.SendBatchManifestAsync(device.IpAddress, device.Port, manifest);
                        if (!resp.AnyAccepted || resp.AcceptedFileIds.Count == 0)
                        {
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                _isTransferInProgress = false;
                                TransferDeclinedMessage.Text = $"'{device.DisplayName}' declined the transfer request.";
                                TransferDeclinedCard.IsVisible = true;
                                SendProgressBorder.IsVisible = false;
                                UpdateTransfersVisibility();
                                ShowTransferFailureModal($"'{device.DisplayName}' declined the transfer request.", device.DisplayName, canRetry: true);
                            });
                            return;
                        }

                        var acceptedSet = new HashSet<string>(resp.AcceptedFileIds);
                        var acceptedItems = new System.Collections.Generic.List<QueueItem>();
                        for (int j = 0; j < manifestItems.Count; j++)
                        {
                            if (acceptedSet.Contains(manifestItems[j].FileId))
                            {
                                acceptedItems.Add(items[j]);
                            }
                        }
                        items = acceptedItems;
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            _activeBatchTotalCount = items.Count;
                            _activeBatchTotalBytes = items.Sum(x => x.Size);
                            ActiveTransferBatchFiles.Clear();
                            foreach (var it in items)
                            {
                                ActiveTransferBatchFiles.Add(new ActiveBatchFileItem
                                {
                                    FileName = it.Name,
                                    FileSize = it.Size,
                                    RelativePath = it.RelativePath,
                                    Status = TransferFileItemStatus.Waiting,
                                    StatusText = "Waiting in queue"
                                });
                            }
                            if (ActiveTransferBatchFiles.Count > 0)
                            {
                                ActiveTransferBatchFiles[0].Status = TransferFileItemStatus.Transferring;
                            }
                            UpdateActiveTransferViewInfo(true, device.DisplayName, device.IpAddress, items.Count, _activeBatchTotalBytes);
                        });
                    }
                    catch (Exception ex)
                    {
                        DebugLog($"Batch manifest negotiation warning: {ex.Message}");
                    }
                }

                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    var state = new FileTransferState
                    {
                        FileId = Guid.NewGuid().ToString("N"),
                        FileName = item.Name,
                        FilePath = item.Path,
                        RelativePath = item.RelativePath,
                        BatchId = batchId,
                        TotalBytes = item.Size,
                        PeerName = device.DisplayName,
                        Direction = TransferDirection.Sent,
                        Status = TransferStatus.Sending,
                        Timestamp = DateTime.UtcNow
                    };

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (!ActiveSends.Any(s => s.FilePath == item.Path && s.PeerName == device.DisplayName))
                            ActiveSends.Add(state);

                        SendProgressFile.Text = item.Name;
                        SendProgressBar.Value = 0;
                        SendProgressSpeed.Text = "Initiating transfer...";
                        SendProgressBorder.IsVisible = true;
                        UpdateTransfersVisibility();
                    });

                    try
                    {
                        if (device.Type == "Web Client")
                        {
                            if (i == 0)
                            {
                                var allPaths = items.Select(x => x.Path).ToList();
                                bool sent = items.Count > 1 
                                    ? (_webDashboardService?.ShareMultipleForWebClient(device.Id, allPaths) ?? false)
                                    : (_webDashboardService?.ShareForWebClient(device.Id, item.Path) ?? false);

                                if (!sent)
                                {
                                    await Dispatcher.UIThread.InvokeAsync(() =>
                                    {
                                        ActiveSends.Remove(state);
                                        UpdateTransfersVisibility();
                                        ShowToast("Web client disconnected — cannot send files");
                                    });
                                    break;
                                }

                                await Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    SendProgressSpeed.Text = $"Transfer request sent to '{device.DisplayName}'. Waiting for client to accept in browser...";
                                    ShowToast($"Offer sent to '{device.DisplayName}' via Web Portal");
                                });
                            }
                            // Keep state in ActiveSends; completion will be handled by OnWebTransferCompleted when client downloads
                            continue;
                        }
                        else
                        {
                            using var stream = await item.OpenStream();
                            await _transferManager.SendFileAsync(device.IpAddress, device.Port, item.Name, stream, item.Size, item.Path, item.RelativePath, batchId);

                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                ActiveSends.Remove(state);
                                UpdateTransfersVisibility();
                            });
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        if (ex is TransferDeclinedException declinedEx)
                        {
                            _lastDeclinedDevice = device;
                            _lastDeclinedItems[device.Id ?? device.IpAddress] = items;
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                ActiveSends.Remove(state);
                                TransferDeclinedMessage.Text = $"'{device.DisplayName}' declined the file transfer request for '{declinedEx.FileName}'.";
                                TransferDeclinedCard.IsVisible = true;
                                SendProgressBorder.IsVisible = false;
                                UpdateTransfersVisibility();
                            });
                            return;
                        }
                        else
                        {
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                ActiveSends.Remove(state);
                                UpdateTransfersVisibility();
                                ShowToast($"Transfer failed: {ex.Message}");
                            });
                        }
                        break;
                    }
                }

                if (device.Type != "Web Client" && items.Count > 0)
                {
                    _transferManager.NotifyBatchCompleted(new BatchManifest
                    {
                        SenderName = _localDevice.DisplayName,
                        TotalBytes = items.Sum(x => x.Size)
                    }, items.Count, items.Sum(x => x.Size));

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        SendProgressBar.Value = 100;
                        SendProgressSpeed.Text = "All files delivered successfully!";
                        SendProgressPct.Text = "Done";
                        ShowTransferSuccessModal(true, device.DisplayName, items.Count, items.Sum(x => x.Size));
                    });
                }
            }
            catch (Exception ex)
            {
                ShowToast($"Transfer error: {ex.Message}");
            }
            finally
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (ActiveSends.Count == 0)
                    {
                        _isSending = false;
                        SendProgressBorder.IsVisible = false;
                    }
                    UpdateTransfersVisibility();
                });
            }
        }

        private void RetryTransfer_Click(object sender, RoutedEventArgs e)
        {
            TransferDeclinedCard.IsVisible = false;
            if (_lastDeclinedDevice != null)
            {
                StartSendSession(_lastDeclinedDevice);
            }
        }

        private void DismissDeclined_Click(object sender, RoutedEventArgs e)
        {
            TransferDeclinedCard.IsVisible = false;
        }

        private void OnWebOfferDeclined(string clientId, string fileNameOrId, string clientName)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var dev = Devices.FirstOrDefault(d => d.Id == clientId) 
                          ?? new DeviceModel { Id = clientId, Name = clientName, Type = "Web Client" };
                _lastDeclinedDevice = dev;
                string fname = string.IsNullOrEmpty(fileNameOrId) ? "the file" : fileNameOrId;
                TransferDeclinedMessage.Text = $"Web client '{clientName}' declined the transfer request for '{fname}'.";
                TransferDeclinedCard.IsVisible = true;
                SendProgressBorder.IsVisible = false;
            });
        }

        // ── File Picking, Dropping & Queue Management ─────────────────────────

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
            DeviceModel? targetDevice = (e.Source as Avalonia.Visual)?.DataContext as DeviceModel;

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
                    else if (Directory.Exists(path))
                    {
                        await AddDirectoryToQueueAsync(path);
                    }
                }
                UpdateQueueUI();

                if (targetDevice != null)
                {
                    _sendTarget = targetDevice;
                    UpdateSendTargetUI();
                    bool isRecv = targetDevice.IsReceiver || string.Equals(targetDevice.Role, "Receiver", StringComparison.OrdinalIgnoreCase);
                    if (isRecv)
                    {
                        ShowToast($"Sending directly to {targetDevice.DisplayName}...");
                        StartSendSession(targetDevice);
                    }
                    else
                    {
                        ShowPanel(SendFilesPanel, "SEND FILES", NavSendBtn);
                        ShowToast($"'{targetDevice.DisplayName}' is not in Receive mode. Ask recipient to tap 'Receive'.");
                    }
                }
                else
                {
                    NavSendFiles_Click(this, new RoutedEventArgs());
                }
            }
        }

        private async void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;
            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions { Title = "Select folder to send", AllowMultiple = false });
            if (folders.Count == 0) return;
            var folderPath = folders[0].Path.LocalPath;
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath)) return;

            await AddDirectoryToQueueAsync(folderPath);
        }

        private async Task AddDirectoryToQueueAsync(string folderPath)
        {
            try
            {
                string folderName = Path.GetFileName(folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (string.IsNullOrEmpty(folderName)) folderName = "Folder";

                ShowToast($"Compressing folder '{folderName}'...");
                string tempZip = Path.Combine(Path.GetTempPath(), $"WeShare_{folderName}_{DateTime.Now:yyyyMMddHHmmss}.zip");

                await Task.Run(() =>
                {
                    if (File.Exists(tempZip)) File.Delete(tempZip);
                    System.IO.Compression.ZipFile.CreateFromDirectory(folderPath, tempZip, System.IO.Compression.CompressionLevel.Fastest, true);
                });

                var fi = new FileInfo(tempZip);
                SendQueue.Add(new QueueItem
                {
                    Name = $"{folderName}.zip",
                    Path = tempZip,
                    Size = fi.Length,
                    OpenStream = () => Task.FromResult<Stream>(File.OpenRead(tempZip)),
                    Thumbnail = null
                });
                UpdateQueueUI();
                ShowToast($"Folder zipped and added: {folderName}.zip ({FileTransferState.FormatBytes(fi.Length)})");
            }
            catch (Exception ex)
            {
                ShowToast($"Failed to compress folder: {ex.Message}");
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

        private void ClearQueue_Click(object sender, RoutedEventArgs e)
        {
            SendQueue.Clear();
            UpdateQueueUI();
            ShowToast("File queue cleared");
        }

        private void UpdateQueueUI()
        {
            QueueEmptyLabel.IsVisible = SendQueue.Count == 0;
            SendFooter.IsVisible      = SendQueue.Count > 0 || _sendTarget != null;
            if (ZipSendBtn != null)
            {
                ZipSendBtn.IsVisible = SendQueue.Count > 1;
            }
            if (ClearQueueBtn != null)
            {
                ClearQueueBtn.IsVisible = SendQueue.Count > 0;
            }
            string targetInfo = _sendTarget != null ? $" → {_sendTarget.DisplayName}" : "";
            long totalSize = SendQueue.Sum(q => q.Size);
            SendSummaryText.Text = $"{SendQueue.Count} file(s) ({FileTransferState.FormatBytes(totalSize)}){targetInfo}";
            UpdateSendTargetUI();
        }

        private void SendNow_Click(object sender, RoutedEventArgs e)
        {
            if (SendQueue.Count == 0) return;
            if (_sendTarget == null)
            {
                NavSendDiscovery_Click(sender, e);
                return;
            }

            if (_isSending)
            {
                ShowToast("A transfer is already in progress");
                return;
            }

            StartSendSession(_sendTarget);
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

        private async Task<System.Collections.Generic.List<QueueItem>> PickFolderAsync()
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return new();
            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions { Title = "Select folder to send", AllowMultiple = false });

            if (folders.Count == 0) return new();
            var folder = folders[0];
            var folderPath = folder.Path.LocalPath;
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath)) return new();

            var list = new System.Collections.Generic.List<QueueItem>();
            var rootParent = Path.GetDirectoryName(folderPath) ?? folderPath;

            var allFiles = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
            foreach (var filePath in allFiles)
            {
                var rel = Path.GetRelativePath(rootParent, filePath);
                var fi = new FileInfo(filePath);
                list.Add(new QueueItem
                {
                    Name = Path.GetFileName(filePath),
                    Path = filePath,
                    RelativePath = rel.Replace('\\', '/'),
                    Size = fi.Length,
                    OpenStream = () => Task.FromResult<Stream>(File.OpenRead(filePath))
                });
            }
            return list;
        }

        // ── Thumbnails & Windows Shell Integration ────────────────────────────

        private async Task<Avalonia.Media.Imaging.Bitmap?> LoadWindowsShellThumbnailAsync(string path)
        {
#if WINDOWS
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
#endif
            await Task.CompletedTask;
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

        // ── Transfer Engine Callbacks ─────────────────────────────────────────

        private void OnTransferStarted(FileTransferState state)
        {
            Dispatcher.UIThread.Post(async () => {
                if (state.Direction == TransferDirection.Received) 
                { 
                    ActiveReceives.Add(state); 
                    RecvEmptyState.IsVisible = false; 
                }
                else 
                { 
                    SendProgressBorder.IsVisible = true; 
                    _currentSendingFileId = state.FileId;
                    SendSpeedGraph?.Clear();
                }

                // If not already in full transfer mode, transition now
                if (!_isTransferInProgress)
                {
                    _isTransferInProgress = true;
                    _activeTransferIsSender = state.Direction == TransferDirection.Sent;
                    _activeTransferPeerName = !string.IsNullOrEmpty(state.PeerName) ? state.PeerName : "Nearby Device";
                    _activeBatchTotalBytes = state.TotalBytes;
                    _activeBatchTransferredBytes = 0;
                    _activeBatchTotalCount = 1;
                    _activeBatchCompletedCount = 0;
                    _transferStartTime = DateTime.UtcNow;

                    ActiveTransferBatchFiles.Clear();
                    ActiveTransferBatchFiles.Add(new ActiveBatchFileItem
                    {
                        FileId = state.FileId,
                        FileName = state.FileName,
                        FileSize = state.TotalBytes,
                        RelativePath = state.RelativePath,
                        Status = TransferFileItemStatus.Transferring,
                        StatusText = "Transferring..."
                    });
                    UpdateActiveTransferViewInfo(_activeTransferIsSender, _activeTransferPeerName, state.RemoteIp, 1, state.TotalBytes);
                    ShowPanel(ActiveTransferPanel, "ACTIVE TRANSFER");
                }
                else
                {
                    var existing = ActiveTransferBatchFiles.FirstOrDefault(f => f.FileId == state.FileId || f.FileName.Equals(state.FileName, StringComparison.OrdinalIgnoreCase));
                    if (existing != null)
                    {
                        existing.FileId = state.FileId;
                        existing.Status = TransferFileItemStatus.Transferring;
                        existing.StatusText = "Transferring...";
                    }
                    else
                    {
                        ActiveTransferBatchFiles.Add(new ActiveBatchFileItem
                        {
                            FileId = state.FileId,
                            FileName = state.FileName,
                            FileSize = state.TotalBytes,
                            RelativePath = state.RelativePath,
                            Status = TransferFileItemStatus.Transferring,
                            StatusText = "Transferring..."
                        });
                        _activeBatchTotalCount = ActiveTransferBatchFiles.Count;
                    }
                }
                
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

                if (HomeSpeedBadge != null)
                {
                    string dir = state.Direction == TransferDirection.Sent ? "↑" : "↓";
                    HomeSpeedBadge.Text       = $"{dir} {state.SpeedMbPerSec:F1} MB/s";
                    HomeSpeedBadge.IsVisible  = true;
                }

                if (state.Direction == TransferDirection.Received)
                {
                    _lastAcceptedIp = state.RemoteIp;
                    _lastAcceptedTime = DateTime.Now;
                }

                var item = ActiveTransferBatchFiles.FirstOrDefault(f => f.FileId == state.FileId || f.FileName.Equals(state.FileName, StringComparison.OrdinalIgnoreCase));
                if (item != null)
                {
                    item.ProgressPercentage = state.ProgressPercentage;
                    item.Status = TransferFileItemStatus.Transferring;
                    item.StatusText = $"{state.ProgressPercentage:F0}% ({state.SpeedMbPerSec:F1} MB/s)";
                }

                long curFileBytes = (long)(state.TotalBytes * (state.ProgressPercentage / 100.0));
                long totalDone = _activeBatchTransferredBytes + curFileBytes;
                long totalVolume = Math.Max(_activeBatchTotalBytes, totalDone);
                double overallPct = totalVolume > 0 ? (totalDone * 100.0 / totalVolume) : state.ProgressPercentage;
                if (overallPct > 100) overallPct = 100;

                if (ActiveTransferMainProgressBar != null) ActiveTransferMainProgressBar.Value = overallPct;
                if (ActiveTransferBigPctText != null) ActiveTransferBigPctText.Text = $"{overallPct:F0}%";
                if (ActiveTransferSpeedText != null) ActiveTransferSpeedText.Text = $"{state.SpeedMbPerSec:F1} MB/s";
                if (ActiveTransferEtaText != null) ActiveTransferEtaText.Text = state.ETA.TotalSeconds > 0 ? state.ETA.ToString(@"mm\:ss") : "--:--";
                if (ActiveTransferBytesText != null) ActiveTransferBytesText.Text = $"{FileTransferState.FormatBytes(totalDone)} / {FileTransferState.FormatBytes(totalVolume)}";
                if (ActiveTransferSummaryFilesText != null) ActiveTransferSummaryFilesText.Text = $"Transferring {state.FileName} ({Math.Min(_activeBatchCompletedCount + 1, _activeBatchTotalCount)} of {_activeBatchTotalCount})...";
                if (ActiveTransferFilesCountText != null) ActiveTransferFilesCountText.Text = $"{_activeBatchCompletedCount} / {_activeBatchTotalCount}";
                ActiveTransferSpeedGraph?.AddSpeed(state.SpeedMbPerSec);
            });
        }

        private void OnTransferCompleted(FileTransferState state)
        {
            Dispatcher.UIThread.Post(async () => {
                SendProgressBorder.IsVisible   = false;
                GlobalActivityBorder.IsVisible = false;
                if (HomeSpeedBadge != null) HomeSpeedBadge.IsVisible = false;

                var batchItem = ActiveTransferBatchFiles.FirstOrDefault(f => f.FileId == state.FileId || f.FileName.Equals(state.FileName, StringComparison.OrdinalIgnoreCase));
                if (batchItem != null)
                {
                    batchItem.Status = TransferFileItemStatus.Completed;
                    batchItem.ProgressPercentage = 100;
                    batchItem.StatusText = "Completed";
                }

                _activeBatchCompletedCount++;
                _activeBatchTransferredBytes += state.TotalBytes;
                if (ActiveTransferFilesCountText != null) ActiveTransferFilesCountText.Text = $"{_activeBatchCompletedCount} / {_activeBatchTotalCount}";

                if (state.Direction == TransferDirection.Received)
                {
                    var ex = ActiveReceives.FirstOrDefault(s => s.FileId == state.FileId);
                    if (ex != null) ActiveReceives.Remove(ex);
                    await _dbHelper.SaveTransferAsync(state);
                    ReceivedFiles.Insert(0, state);
                    ShowToast($"📥 '{state.FileName}' received from {state.PeerName}!", 4500);
                    PlaySound("complete");
                    _platformService.ShowSystemToast("File Received", $"{state.FileName} from {state.PeerName}", state.FilePath);
                    _lastAcceptedIp = state.RemoteIp;
                    _lastAcceptedTime = DateTime.Now;
                    CheckAndShowIncomingNote(state);
                }
                else
                {
                    PlaySound("complete");
                    _currentSendingFileId = null;
                    await _dbHelper.SaveTransferAsync(state);
                    CleanTempZipFile(state.FilePath);
                }

                bool allDone = _activeBatchTotalCount > 0 && (_activeBatchCompletedCount >= _activeBatchTotalCount || ActiveTransferBatchFiles.All(f => f.Status == TransferFileItemStatus.Completed));
                if (allDone)
                {
                    _isTransferInProgress = false;
                    string peer = !string.IsNullOrEmpty(_activeTransferPeerName) ? _activeTransferPeerName : state.PeerName;
                    ShowTransferSuccessModal(_activeTransferIsSender, peer, _activeBatchCompletedCount, _activeBatchTotalBytes > 0 ? _activeBatchTotalBytes : _activeBatchTransferredBytes);
                }
                else
                {
                    var nextWaiting = ActiveTransferBatchFiles.FirstOrDefault(f => f.Status == TransferFileItemStatus.Waiting);
                    if (nextWaiting != null)
                    {
                        nextWaiting.Status = TransferFileItemStatus.Transferring;
                        nextWaiting.StatusText = "Starting...";
                    }
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

                var batchItem = ActiveTransferBatchFiles.FirstOrDefault(f => f.FileId == state.FileId || f.FileName.Equals(state.FileName, StringComparison.OrdinalIgnoreCase));
                if (batchItem != null)
                {
                    batchItem.Status = TransferFileItemStatus.Failed;
                    batchItem.StatusText = "Failed";
                }

                _isTransferInProgress = false;
                string rawReason = !string.IsNullOrEmpty(state.ErrorMessage) ? state.ErrorMessage : "Connection interrupted";
                string friendlyToast;
                if (rawReason.Contains("declined", StringComparison.OrdinalIgnoreCase) || rawReason.Contains("refused", StringComparison.OrdinalIgnoreCase))
                    friendlyToast = "Transfer was declined or stopped by recipient.";
                else if (rawReason.Contains("network", StringComparison.OrdinalIgnoreCase) || rawReason.Contains("socket", StringComparison.OrdinalIgnoreCase) || rawReason.Contains("timed out", StringComparison.OrdinalIgnoreCase))
                    friendlyToast = "Wi-Fi connection lost. Check connection and tap Retry.";
                else
                    friendlyToast = "Transfer stopped. You can tap Retry to continue.";

                ShowToast($"❌ '{state.FileName}': {friendlyToast}", 4000);
                _platformService.ShowSystemToast("Transfer Stopped", $"{state.FileName}: {friendlyToast}");

                ShowTransferFailureModal(rawReason, _activeTransferPeerName, canRetry: _activeTransferIsSender);
            });
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
                            ShowToast($"📥 '{state.FileName}' received from {state.PeerName}!", 4500);
                            PlaySound("complete");
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
                else if (state.Direction == TransferDirection.Sent)
                {
                    var exSend = ActiveSends.FirstOrDefault(s => s.FileId == state.FileId || s.FileName == state.FileName);
                    if (exSend != null) ActiveSends.Remove(exSend);
                    await _dbHelper.SaveTransferAsync(state);
                    ShowToast($"✅ Delivered '{state.FileName}' to '{state.PeerName}'", 3500);
                    _platformService.ShowSystemToast("File Delivered", $"{state.FileName} downloaded by {state.PeerName}", state.FilePath);
                    PlaySound("success");
                    UpdateTransfersVisibility();
                    RefreshHistory();
                }
            });
        }

        private void ConfigureTransferManager(TcpTransferManager manager)
        {
            manager.LocalName = _localDevice.DisplayName;
            manager.LocalType = _localDevice.Type;
            manager.TransferStarted   += OnTransferStarted;
            manager.TransferProgress  += OnTransferProgress;
            manager.TransferCompleted += OnTransferCompleted;
            manager.TransferRequestCallback = OnTransferRequested;
            manager.ConnectionRequestCallback = OnConnectionRequested;
            manager.BatchManifestCallback = OnBatchManifestRequested;
            manager.ResendRequestCallback = OnResendRequested;
            manager.BatchTransferCompleted += OnBatchTransferCompleted;
            manager.DeviceConnected += OnDeviceConnected;
            manager.DeviceDisconnected += OnDeviceDisconnected;
        }

        // ── Dock Telemetry, Progress Views & Cancel Management ────────────────

        private void UpdateActiveTransferDock(FileTransferState state)
        {
            if (ActiveTransferDock == null) return;
            ActiveTransferDock.IsVisible = true;
            if (BottomPlayerFileName != null) BottomPlayerFileName.Text = state.FileName;
            if (BottomPlayerPeerName != null) 
            {
                string dirLabel = state.Direction == TransferDirection.Sent ? "To: " : "From: ";
                string peer = !string.IsNullOrEmpty(state.PeerName) ? state.PeerName : "Nearby Device";
                string sizeStr = state.TotalBytes > 0 ? $" • {FileTransferState.FormatBytes(state.TotalBytes)}" : "";
                BottomPlayerPeerName.Text = $"{dirLabel}{peer}{sizeStr}";
            }
            if (BottomPlayerSpeed != null) BottomPlayerSpeed.Text = $"{state.SpeedMbPerSec:F1} MB/s";
            if (ActiveDockProgressPct != null) ActiveDockProgressPct.Text = $"{state.ProgressPercentage:F0}%";
            if (BottomPlayerProgress != null) BottomPlayerProgress.Value = state.ProgressPercentage;
            if (ActiveDockStatusLabel != null) 
                ActiveDockStatusLabel.Text = state.Direction == TransferDirection.Sent ? "SENDING" : "RECEIVING";
            
            try
            {
                if (ActiveDockIcon != null)
                {
                    string iconKey = state.Direction == TransferDirection.Sent ? "IconSend" : "IconReceive";
                    if (this.TryFindResource(iconKey, out var res) && res is StreamGeometry geom)
                    {
                        ActiveDockIcon.Data = geom;
                    }
                }
                if (ActiveDockBadge != null)
                {
                    ActiveDockBadge.Background = state.Direction == TransferDirection.Sent 
                        ? SolidColorBrush.Parse("#257C3AED") 
                        : SolidColorBrush.Parse("#2506B6D4");
                }
            }
            catch { }
        }

        private void HideActiveTransferDock()
        {
            if (ActiveTransferDock == null) return;
            if (!HasActiveTransfer())
            {
                ActiveTransferDock.IsVisible = false;
            }
        }

        private void CancelActiveSend_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_currentSendingFileId))
            {
                _transferManager.CancelTransfer(_currentSendingFileId);
                ShowToast("Sending cancelled");
            }
            else if (ActiveReceives.Count > 0)
            {
                var first = ActiveReceives.FirstOrDefault();
                if (first != null)
                {
                    _transferManager.CancelTransfer(first.FileId);
                    ShowToast("Receiving cancelled");
                }
            }
            HideActiveTransferDock();
        }

        private void CancelIncoming_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is FileTransferState state)
            {
                _transferManager.CancelTransfer(state.FileId);
                ShowToast("Receiving cancelled");
            }
        }

        private void CancelOutgoingItem_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is FileTransferState state)
            {
                _transferManager.CancelTransfer(state.FileId);
                ActiveSends.Remove(state);
                UpdateTransfersVisibility();
                ShowToast($"Transfer of {state.FileName} cancelled");
            }
        }

        private void CancelActiveTransferSession_Click(object? sender, RoutedEventArgs e)
        {
            _isTransferInProgress = false;
            try
            {
                _transferManager.CancelAll();
            }
            catch (Exception ex)
            {
                DebugLog($"CancelAll error: {ex.Message}");
            }

            ShowToast("Transfer cancelled by user.");
            ShowTransferFailureModal("Transfer cancelled by user.", _activeTransferPeerName, canRetry: _activeTransferIsSender);
        }

        private void UpdateActiveTransferViewInfo(bool isSender, string peerName, string peerIp, int totalFiles, long totalBytes)
        {
            if (ActiveTransferRoleBadge != null)
            {
                ActiveTransferRoleBadge.Background = isSender ? new SolidColorBrush(Color.Parse("#25173B")) : new SolidColorBrush(Color.Parse("#102A24"));
                ActiveTransferRoleBadge.BorderBrush = isSender ? new SolidColorBrush(Color.Parse("#7C3AED")) : new SolidColorBrush(Color.Parse("#10B981"));
            }
            if (ActiveTransferRoleText != null)
            {
                ActiveTransferRoleText.Text = isSender ? "SENDING IN PROGRESS" : "RECEIVING IN PROGRESS";
                ActiveTransferRoleText.Foreground = isSender ? new SolidColorBrush(Color.Parse("#A855F7")) : new SolidColorBrush(Color.Parse("#10B981"));
            }
            if (ActiveTransferPeerInfo != null)
            {
                string ipDisplay = !string.IsNullOrEmpty(peerIp) ? $" ({peerIp})" : "";
                ActiveTransferPeerInfo.Text = $"{(isSender ? "Sending to: " : "Receiving from: ")}{peerName}{ipDisplay}";
            }
            if (ActiveTransferQueueCount != null)
            {
                ActiveTransferQueueCount.Text = $"({totalFiles} file{(totalFiles == 1 ? "" : "s")})";
            }
            if (ActiveTransferFilesCountText != null)
            {
                ActiveTransferFilesCountText.Text = $"0 / {totalFiles}";
            }
            if (ActiveTransferBytesText != null)
            {
                ActiveTransferBytesText.Text = $"0 B / {FileTransferState.FormatBytes(totalBytes)}";
            }
            if (ActiveTransferSummaryFilesText != null)
            {
                ActiveTransferSummaryFilesText.Text = $"{totalFiles} file{(totalFiles == 1 ? "" : "s")} queued";
            }
            if (ActiveTransferMainProgressBar != null)
            {
                ActiveTransferMainProgressBar.Value = 0;
            }
            if (ActiveTransferBigPctText != null)
            {
                ActiveTransferBigPctText.Text = "0%";
            }
            if (ActiveTransferSpeedText != null)
            {
                ActiveTransferSpeedText.Text = "0.0 MB/s";
            }
            if (ActiveTransferEtaText != null)
            {
                ActiveTransferEtaText.Text = "--:--";
            }
        }

        private void UpdateTransfersVisibility()
        {
            bool hasSends = ActiveSends.Count > 0 || _isSending || (SendProgressBorder != null && SendProgressBorder.IsVisible);
            bool hasReceives = ActiveReceives.Count > 0;
            bool hasAny = hasSends || hasReceives;

            if (NoActiveTransfersCard != null)
                NoActiveTransfersCard.IsVisible = !hasAny;

            if (OutgoingTransfersBorder != null)
                OutgoingTransfersBorder.IsVisible = ActiveSends.Count > 1;

            if (IncomingBorder != null)
                IncomingBorder.IsVisible = hasReceives;
        }

        private bool HasActiveTransfer()
        {
            return _isSending || ActiveReceives.Count > 0;
        }

        // ── ZIP Multi-file Archives ───────────────────────────────────────────

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

        // ── Unique File Path Helper ───────────────────────────────────────────

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
}
