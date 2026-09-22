using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using WeShare.Core.Models;
using WeShare.Core.Network;
using WeShare.Core.Transfer;

namespace WeShare.UI.Views
{
    public partial class MainView : UserControl
    {
        // ── First-Run Onboarding Tour & Device Renaming ───────────────────────────

        private int _currentWelcomeStep = 1;

        private void ShowWelcomeStep(int step)
        {
            _currentWelcomeStep = step;
            if (WelcomeStepIndicatorText != null) WelcomeStepIndicatorText.Text = $"Step {step} of 3";
            if (WelcomeSlide1 != null) WelcomeSlide1.IsVisible = (step == 1);
            if (WelcomeSlide2 != null) WelcomeSlide2.IsVisible = (step == 2);
            if (WelcomeSlide3 != null) WelcomeSlide3.IsVisible = (step == 3);
        }

        private void WelcomeNext_Click(object? sender, RoutedEventArgs e)
        {
            if (_currentWelcomeStep < 3)
                ShowWelcomeStep(_currentWelcomeStep + 1);
        }

        private void WelcomeBack_Click(object? sender, RoutedEventArgs e)
        {
            if (_currentWelcomeStep > 1)
                ShowWelcomeStep(_currentWelcomeStep - 1);
        }

        private void WelcomeSkip_Click(object? sender, RoutedEventArgs e)
        {
            ShowWelcomeStep(2);
        }

        public void ShowWelcomeTour_Click(object? sender, RoutedEventArgs e)
        {
            if (WelcomeDeviceNameInput != null) WelcomeDeviceNameInput.Text = _localDevice.Name;
            ShowWelcomeStep(1);
            if (FirstRunWelcomeModal != null) FirstRunWelcomeModal.IsVisible = true;
        }

        private void CompleteOnboarding_Click(object? sender, RoutedEventArgs e)
        {
            var chosenName = WelcomeDeviceNameInput?.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(chosenName))
            {
                _localDevice.Name = chosenName;
                if (SidebarDeviceName != null) SidebarDeviceName.Text = chosenName;
                if (HomeDeviceNameText != null) HomeDeviceNameText.Text = chosenName;
                if (SettingsDeviceName != null) SettingsDeviceName.Text = chosenName;
                _dbHelper.SetSetting("DeviceName", chosenName);
            }
            _dbHelper.SetSetting("DeviceNameInitialized", "true");
            _dbHelper.SetSetting("OnboardingCompleted", "true");
            if (FirstRunWelcomeModal != null) FirstRunWelcomeModal.IsVisible = false;
            _ = _discoveryService?.BroadcastPresenceAsync();
            ShowToast($"Ready to share! Device name: '{_localDevice.Name}'", 3500);
        }

        private void SidebarDeviceName_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            ShowPanel(SettingsPanel, "SETTINGS", NavSettBtn);
        }

        private void SettingsDeviceName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SettingsDeviceName != null && !string.IsNullOrEmpty(SettingsDeviceName.Text))
            {
                _localDevice.Name = SettingsDeviceName.Text;
                if (SidebarDeviceName != null) SidebarDeviceName.Text = _localDevice.Name;
                if (HomeDeviceNameText != null) HomeDeviceNameText.Text = _localDevice.Name;
                _ = _dbHelper.SetSettingAsync("DeviceName", _localDevice.Name);
                _dbHelper.SetSetting("DeviceNameInitialized", "true");
            }
        }

        // ── Cancel Sending Confirmation Modal ─────────────────────────────────────

        private void CancelSending_Click(object sender, RoutedEventArgs e)
        {
            if (SendQueue.Count > 0)
            {
                if (CancelConfirmationMessage != null)
                    CancelConfirmationMessage.Text = $"You have {SendQueue.Count} file(s) selected. Are you sure you want to cancel?";
                if (CancelConfirmationModal != null)
                    CancelConfirmationModal.IsVisible = true;
                return;
            }

            _sendTarget = null;
            UpdateSendTargetUI();
            _isSending = false;
            NavHome_Click(sender, e);
        }

        private void DismissCancelModal_Click(object? sender, RoutedEventArgs e)
        {
            if (CancelConfirmationModal != null)
                CancelConfirmationModal.IsVisible = false;
        }

        private void ConfirmCancel_Click(object? sender, RoutedEventArgs e)
        {
            if (CancelConfirmationModal != null)
                CancelConfirmationModal.IsVisible = false;
            SendQueue.Clear();
            _sendTarget = null;
            UpdateSendTargetUI();
            _isSending = false;
            ShowToast("Transfer cancelled");
            NavHome_Click(sender, e);
        }

        // ── Batch Manifest Review Checklist Modal ─────────────────────────────────

        private async Task<System.Collections.Generic.List<string>> OnBatchManifestRequested(BatchManifest manifest)
        {
            _currentPendingBatchManifest = manifest;
            _currentBatchSenderIp = manifest.SenderIp;
            var tcs = new TaskCompletionSource<System.Collections.Generic.List<string>>(TaskCreationOptions.RunContinuationsAsynchronously);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _batchManifestTcs?.TrySetCanceled();
                _batchManifestTcs = tcs;

                BatchManifestItems.Clear();
                foreach (var f in manifest.Files)
                {
                    BatchManifestItems.Add(new BatchCheckItem
                    {
                        FileId = f.FileId,
                        FileName = f.FileName,
                        RelativePath = f.RelativePath != f.FileName ? f.RelativePath : "",
                        Size = f.FileSize,
                        IsSelected = true
                    });
                }

                if (BatchManifestList != null) BatchManifestList.ItemsSource = BatchManifestItems;
                if (BatchManifestTitle != null) BatchManifestTitle.Text = $"Incoming Batch ({manifest.Files.Count} Files)";
                if (BatchManifestSubtitle != null) BatchManifestSubtitle.Text = $"From: {manifest.SenderName} · Total: {FileTransferState.FormatBytes(manifest.TotalBytes)}";
                if (BatchSelectAllCheckBox != null) BatchSelectAllCheckBox.IsChecked = true;
                UpdateBatchManifestSummary();

                if (BatchManifestModal != null) BatchManifestModal.IsVisible = true;
                PlaySound("request");
                ShowToast($"Incoming file batch ({manifest.Files.Count} files) from {manifest.SenderName}");
            });

            return await tcs.Task;
        }

        private void BatchSelectAll_Click(object? sender, RoutedEventArgs e)
        {
            bool isChecked = BatchSelectAllCheckBox?.IsChecked ?? true;
            foreach (var item in BatchManifestItems)
            {
                item.IsSelected = isChecked;
            }
            UpdateBatchManifestSummary();
        }

        private void BatchItemCheck_Click(object? sender, RoutedEventArgs e)
        {
            UpdateBatchManifestSummary();
        }

        private void UpdateBatchManifestSummary()
        {
            int selectedCount = BatchManifestItems.Count(x => x.IsSelected);
            long selectedBytes = BatchManifestItems.Where(x => x.IsSelected).Sum(x => x.Size);
            if (BatchManifestSelectedSummary != null)
            {
                BatchManifestSelectedSummary.Text = $"{selectedCount} of {BatchManifestItems.Count} selected ({FileTransferState.FormatBytes(selectedBytes)})";
            }
            if (BatchAcceptBtn != null)
            {
                BatchAcceptBtn.Content = selectedCount > 0 ? $"Accept Selected ({selectedCount})" : "Accept Selected";
                BatchAcceptBtn.IsEnabled = selectedCount > 0;
            }
            if (BatchSelectAllCheckBox != null)
            {
                BatchSelectAllCheckBox.IsChecked = selectedCount == BatchManifestItems.Count;
            }
        }

        private void AcceptBatchManifest_Click(object? sender, RoutedEventArgs e)
        {
            if (BatchManifestModal != null) BatchManifestModal.IsVisible = false;
            if (_currentPendingBatchManifest != null)
            {
                _activeAcceptedBatchId = _currentPendingBatchManifest.BatchId;
                _lastAcceptedIp = _currentPendingBatchManifest.SenderIp;
                _lastAcceptedTime = DateTime.Now;

                var selectedFiles = BatchManifestItems.Where(x => x.IsSelected).ToList();
                _activeBatchTotalCount = selectedFiles.Count;
                _activeBatchTotalBytes = selectedFiles.Sum(x => x.Size);
                _activeBatchCompletedCount = 0;
                _activeBatchTransferredBytes = 0;
                _activeTransferIsSender = false;
                _activeTransferPeerName = _currentPendingBatchManifest.SenderName;
                _isTransferInProgress = true;
                _transferStartTime = DateTime.UtcNow;

                ActiveTransferBatchFiles.Clear();
                foreach (var f in selectedFiles)
                {
                    ActiveTransferBatchFiles.Add(new ActiveBatchFileItem
                    {
                        FileId = f.FileId,
                        FileName = f.FileName,
                        FileSize = f.Size,
                        RelativePath = f.RelativePath,
                        Status = TransferFileItemStatus.Waiting,
                        StatusText = "Waiting in queue"
                    });
                }
                if (ActiveTransferBatchFiles.Count > 0)
                {
                    ActiveTransferBatchFiles[0].Status = TransferFileItemStatus.Transferring;
                    ActiveTransferBatchFiles[0].StatusText = "Receiving...";
                }

                UpdateActiveTransferViewInfo(false, _currentPendingBatchManifest.SenderName, _currentPendingBatchManifest.SenderIp, selectedFiles.Count, _activeBatchTotalBytes);
                ShowPanel(ActiveTransferPanel, "ACTIVE TRANSFER");
            }
            var acceptedIds = BatchManifestItems.Where(x => x.IsSelected).Select(x => x.FileId).ToList();
            _batchManifestTcs?.TrySetResult(acceptedIds);
            ShowToast($"Accepted {acceptedIds.Count} files for transfer.");
        }

        private void DeclineBatchManifest_Click(object? sender, RoutedEventArgs e)
        {
            if (BatchManifestModal != null) BatchManifestModal.IsVisible = false;
            _batchManifestTcs?.TrySetResult(new System.Collections.Generic.List<string>());
            ShowToast("Batch transfer declined.");
        }

        // ── Celebratory Transfer Success & Failure Modals ─────────────────────────

        private void OnBatchTransferCompleted(BatchManifest manifest, int fileCount, long totalBytes)
        {
            Dispatcher.UIThread.Post(() =>
            {
                ShowTransferSuccessModal(false, manifest.SenderName, fileCount, totalBytes);
            });
        }

        public void ShowTransferSuccessModal(bool isSender, string peerName, int fileCount, long totalBytes)
        {
            if (TransferSuccessModal != null)
            {
                if (TransferSuccessTitle != null)
                    TransferSuccessTitle.Text = isSender ? "Files Sent Successfully" : "Files Received Successfully";
                if (TransferSuccessSubtitle != null)
                    TransferSuccessSubtitle.Text = isSender 
                        ? $"All {fileCount} files were delivered to {peerName}." 
                        : $"All {fileCount} files from {peerName} are saved in your Downloads.";
                if (TransferSuccessFileCount != null)
                    TransferSuccessFileCount.Text = $"{fileCount} file{(fileCount == 1 ? "" : "s")}";
                if (TransferSuccessTotalSize != null)
                    TransferSuccessTotalSize.Text = FileTransferState.FormatBytes(totalBytes);
                if (TransferSuccessPeerName != null)
                    TransferSuccessPeerName.Text = string.IsNullOrEmpty(peerName) ? "Nearby Device" : peerName;

                TransferSuccessModal.IsVisible = true;
                PlaySound("success");
                ShowToast("Transfer completed successfully!");
            }
        }

        public void ShowTransferFailureModal(string reason, string peerName, bool canRetry = false)
        {
            if (TransferFailureModal != null)
            {
                string friendlyReason;
                if (string.IsNullOrWhiteSpace(reason) || reason.Contains("interrupted", StringComparison.OrdinalIgnoreCase))
                    friendlyReason = "Connection was interrupted. Please ensure both devices stay on the same Wi-Fi network.";
                else if (reason.Contains("refused", StringComparison.OrdinalIgnoreCase) || reason.Contains("declined", StringComparison.OrdinalIgnoreCase))
                    friendlyReason = "The other device declined or stopped the transfer.";
                else if (reason.Contains("socket", StringComparison.OrdinalIgnoreCase) || reason.Contains("network", StringComparison.OrdinalIgnoreCase) || reason.Contains("timed out", StringComparison.OrdinalIgnoreCase))
                    friendlyReason = "Network connection dropped. Check Wi-Fi and tap Retry.";
                else
                    friendlyReason = reason;

                if (TransferFailureTitle != null) TransferFailureTitle.Text = "Transfer Stopped";
                if (TransferFailureSubtitle != null) TransferFailureSubtitle.Text = "We couldn't finish transferring this file.";
                if (TransferFailureReason != null) TransferFailureReason.Text = friendlyReason;
                if (TransferFailurePeerName != null) TransferFailurePeerName.Text = string.IsNullOrEmpty(peerName) ? "Nearby Device" : peerName;
                if (TransferFailureRetryBtn != null) TransferFailureRetryBtn.IsVisible = canRetry;

                TransferFailureModal.IsVisible = true;
                PlaySound("failed");
            }
        }

        private void CloseTransferSuccessModal_Click(object? sender, RoutedEventArgs e)
        {
            if (TransferSuccessModal != null) TransferSuccessModal.IsVisible = false;
            _isTransferInProgress = false;
            ShowPanel(HomePanel, "HOME", NavHomeBtn);
        }

        private void CloseTransferFailureModal_Click(object? sender, RoutedEventArgs e)
        {
            if (TransferFailureModal != null) TransferFailureModal.IsVisible = false;
            _isTransferInProgress = false;
            ShowPanel(HomePanel, "HOME", NavHomeBtn);
        }

        private void RetryFailedTransfer_Click(object? sender, RoutedEventArgs e)
        {
            if (TransferFailureModal != null) TransferFailureModal.IsVisible = false;
            _isTransferInProgress = false;

            if (_sendTarget != null && SendQueue.Count > 0)
            {
                StartSendSession(_sendTarget);
            }
            else
            {
                ShowPanel(SendFilesPanel, "SEND FILES", NavSendBtn);
            }
        }

        // ── Media Preview Modal ───────────────────────────────────────────────────

        private void PreviewFile_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is FileTransferState s && !string.IsNullOrEmpty(s.FilePath) && File.Exists(s.FilePath))
            {
                var ext = Path.GetExtension(s.FilePath).ToLowerInvariant();
                if (ext is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".webp")
                {
                    ShowImagePreview(s);
                }
                else if (ext == ".txt")
                {
                    CheckAndShowIncomingNote(s);
                }
                else
                {
                    _platformService.OpenFile(s.FilePath);
                }
            }
        }

        private void ShowImagePreview(FileTransferState s)
        {
            try
            {
                using var stream = File.OpenRead(s.FilePath);
                var bmp = new Avalonia.Media.Imaging.Bitmap(stream);
                PreviewImageControl.Source = bmp;
                PreviewFileName.Text = s.FileName;
                PreviewFileSpecs.Text = $"{bmp.PixelSize.Width} × {bmp.PixelSize.Height} · {FileTransferState.FormatBytes(s.TotalBytes)}";
                _activePreviewFilePath = s.FilePath;
                MediaPreviewModal.IsVisible = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Preview] Image decode failed: {ex.Message}");
                _platformService.OpenFile(s.FilePath);
            }
        }

        private async void CopyPreviewImage_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_activePreviewFilePath) && File.Exists(_activePreviewFilePath))
            {
                var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                if (clipboard != null)
                {
                    var dataObject = new DataObject();
                    dataObject.Set(DataFormats.Files, new[] { _activePreviewFilePath });
                    dataObject.Set(DataFormats.Text, _activePreviewFilePath);
                    await clipboard.SetDataObjectAsync(dataObject);
                    ShowToast("Image copied to clipboard!");
                }
            }
        }

        private void OpenPreviewInApp_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_activePreviewFilePath) && File.Exists(_activePreviewFilePath))
            {
                _platformService.OpenFile(_activePreviewFilePath);
            }
        }

        private void ClosePreviewModal_Click(object sender, RoutedEventArgs e)
        {
            MediaPreviewModal.IsVisible = false;
            PreviewImageControl.Source = null;
            _activePreviewFilePath = null;
        }

        // ── Nickname Management Modal ─────────────────────────────────────────────

        private void EditDeviceNickname_Click(object? sender, RoutedEventArgs e)
        {
            var device = (sender as MenuItem)?.Tag as DeviceModel 
                      ?? ((sender as MenuItem)?.DataContext as DeviceModel) 
                      ?? ((sender as Control)?.DataContext as DeviceModel);
            if (device != null)
            {
                _editingNicknameDevice = device;
                NicknameInput.Text = device.CustomNickname ?? device.Name;
                EditNicknameModal.IsVisible = true;
                NicknameInput.Focus();
            }
        }

        private void SaveNickname_Click(object? sender, RoutedEventArgs e)
        {
            if (_editingNicknameDevice != null)
            {
                string newName = NicknameInput.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(newName) || newName == _editingNicknameDevice.Name)
                {
                    _editingNicknameDevice.CustomNickname = null;
                    _deviceNicknames.Remove(_editingNicknameDevice.Id);
                }
                else
                {
                    _editingNicknameDevice.CustomNickname = newName;
                    _deviceNicknames[_editingNicknameDevice.Id] = newName;
                }

                SaveDevicePreferences();
                SortDevices();
                ShowToast($"Device renamed to '{_editingNicknameDevice.DisplayName}'");
            }
            EditNicknameModal.IsVisible = false;
            _editingNicknameDevice = null;
        }

        private void CloseNicknameModal_Click(object? sender, RoutedEventArgs e)
        {
            EditNicknameModal.IsVisible = false;
            _editingNicknameDevice = null;
        }

        // ── Quick Note & Clipboard Sharing Modal ──────────────────────────────────

        private void OpenQuickText_Click(object? sender, RoutedEventArgs e)
        {
            QuickTextInput.Text = string.Empty;
            QuickTextModal.IsVisible = true;
            QuickTextInput.Focus();
        }

        private void CloseQuickText_Click(object? sender, RoutedEventArgs e)
        {
            QuickTextModal.IsVisible = false;
        }

        private async void PasteClipboardToText_Click(object? sender, RoutedEventArgs e)
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                string? text = await clipboard.GetTextAsync();
                if (!string.IsNullOrEmpty(text))
                {
                    QuickTextInput.Text = text;
                    ShowToast("Pasted from clipboard");
                }
                else
                {
                    ShowToast("Clipboard is empty or contains non-text data");
                }
            }
        }

        private void SendQuickText_Click(object? sender, RoutedEventArgs e)
        {
            string text = QuickTextInput.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(text))
            {
                ShowToast("Please enter or paste text to send");
                return;
            }

            try
            {
                string notesDir = Path.Combine(Path.GetTempPath(), "WeShare_Notes");
                if (!Directory.Exists(notesDir)) Directory.CreateDirectory(notesDir);

                string fileName = $"Note_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string filePath = Path.Combine(notesDir, fileName);
                File.WriteAllText(filePath, $"// WE-SHARE-NOTE\n{text}");

                var fi = new FileInfo(filePath);
                SendQueue.Clear();
                SendQueue.Add(new QueueItem
                {
                    Name = fileName,
                    Path = filePath,
                    Size = fi.Length,
                    OpenStream = () => Task.FromResult<Stream>(File.OpenRead(filePath)),
                    Thumbnail = null
                });

                UpdateQueueUI();
                QuickTextModal.IsVisible = false;

                if (_sendTarget != null)
                {
                    ShowToast($"Sending note to {_sendTarget.DisplayName}...");
                    StartSendSession(_sendTarget);
                }
                else
                {
                    ShowToast("Note ready! Select a recipient to send to.");
                    ShowPanel(SendDiscoveryPanel, "CHOOSE RECIPIENT", null);
                }
            }
            catch (Exception ex)
            {
                ShowToast($"Failed to create note: {ex.Message}");
            }
        }

        private void CheckAndShowIncomingNote(FileTransferState state)
        {
            if (string.IsNullOrEmpty(state.FilePath) || !File.Exists(state.FilePath)) return;
            var ext = Path.GetExtension(state.FilePath).ToLowerInvariant();
            if (ext != ".txt") return;

            try
            {
                var fi = new FileInfo(state.FilePath);
                if (fi.Length > 128 * 1024) return;
                string content = File.ReadAllText(state.FilePath);
                if (content.StartsWith("// WE-SHARE-NOTE\n") || state.FileName.StartsWith("Note_"))
                {
                    if (content.StartsWith("// WE-SHARE-NOTE\n"))
                    {
                        content = content.Substring("// WE-SHARE-NOTE\n".Length);
                    }
                    IncomingNoteSenderText.Text = $"From: {state.PeerName}";
                    IncomingNoteContentText.Text = content.Trim();
                    bool hasLink = content.Contains("http://") || content.Contains("https://");
                    IncomingNoteOpenLinkBtn.IsVisible = hasLink;
                    IncomingNoteModal.IsVisible = true;
                }
            }
            catch { }
        }

        private async void CopyIncomingNote_Click(object? sender, RoutedEventArgs e)
        {
            string text = IncomingNoteContentText.Text ?? string.Empty;
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null && !string.IsNullOrEmpty(text))
            {
                await clipboard.SetTextAsync(text);
                ShowToast("Copied note to clipboard!");
            }
        }

        private void OpenIncomingNoteLink_Click(object? sender, RoutedEventArgs e)
        {
            string text = IncomingNoteContentText.Text ?? string.Empty;
            var match = System.Text.RegularExpressions.Regex.Match(text, @"https?://[^\s]+");
            if (match.Success)
            {
                _platformService.OpenUrl(match.Value);
            }
            else
            {
                ShowToast("No valid URL found in note");
            }
        }

        private void CloseIncomingNote_Click(object? sender, RoutedEventArgs e)
        {
            IncomingNoteModal.IsVisible = false;
        }

        // ── Resend Request Handlers ───────────────────────────────────────────────

        private async Task<bool> OnResendRequested(ResendRequest req)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _resendRequestTcs?.TrySetCanceled();
                _resendRequestTcs = tcs;
                _currentResendRequest = req;

                if (ResendRequestMessage != null)
                {
                    ResendRequestMessage.Text = $"'{req.RequesterName}' requested you to resend '{req.FileName}'.";
                }
                if (ResendRequestModal != null) ResendRequestModal.IsVisible = true;
                PlaySound("request");
                ShowToast($"Resend requested for '{req.FileName}' by {req.RequesterName}");
            });

            bool accepted = await tcs.Task;
            if (accepted)
            {
                _ = Task.Run(async () =>
                {
                    var matched = LibraryFiles.FirstOrDefault(f => f.FileName.Equals(req.FileName, StringComparison.OrdinalIgnoreCase))
                               ?? ReceivedFiles.FirstOrDefault(f => f.FileName.Equals(req.FileName, StringComparison.OrdinalIgnoreCase));
                    string? filePath = matched?.FilePath;
                    if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                    {
                        string candidate = Path.Combine(_saveDirectory, req.FileName);
                        if (File.Exists(candidate)) filePath = candidate;
                    }

                    if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath) && _sendTarget != null)
                    {
                        var fi = new FileInfo(filePath);
                        var queueItem = new QueueItem
                        {
                            Name = Path.GetFileName(filePath),
                            Path = filePath,
                            Size = fi.Length,
                            OpenStream = () => Task.FromResult<Stream>(File.OpenRead(filePath))
                        };
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            SendQueue.Add(queueItem);
                            UpdateQueueUI();
                            StartSendSession(_sendTarget);
                        });
                    }
                });
            }
            return accepted;
        }

        private void AcceptResendRequest_Click(object? sender, RoutedEventArgs e)
        {
            if (ResendRequestModal != null) ResendRequestModal.IsVisible = false;
            _resendRequestTcs?.TrySetResult(true);
            ShowToast("Accepted resend request. Resending file...");
        }

        private void DeclineResendRequest_Click(object? sender, RoutedEventArgs e)
        {
            if (ResendRequestModal != null) ResendRequestModal.IsVisible = false;
            _resendRequestTcs?.TrySetResult(false);
            ShowToast("Declined resend request.");
        }

        private async void RequestResendForFile_Click(object? sender, RoutedEventArgs e)
        {
            var transfer = (sender as Button)?.Tag as FileTransferState;
            if (transfer == null || _sendTarget == null)
            {
                ShowToast("Cannot request resend: no active peer connection.");
                return;
            }

            ShowToast($"Requesting resend of '{transfer.FileName}'...");
            if (_sendTarget.Type == "Web Client")
            {
                _webDashboardService?.PushResendRequestToWeb(_sendTarget.Id, new ResendRequest
                {
                    FileId = transfer.FileId,
                    FileName = transfer.FileName,
                    RequesterName = _localDevice.DisplayName
                });
            }
            else
            {
                try
                {
                    var res = await _transferManager.SendResendRequestAsync(_sendTarget.IpAddress, _sendTarget.Port, new ResendRequest
                    {
                        FileId = transfer.FileId,
                        FileName = transfer.FileName,
                        RequesterName = _localDevice.DisplayName
                    });
                    if (res)
                    {
                        ShowToast($"{_sendTarget.DisplayName} accepted resend request for '{transfer.FileName}'!");
                    }
                    else
                    {
                        ShowToast($"{_sendTarget.DisplayName} declined or could not resend file.");
                    }
                }
                catch (Exception ex)
                {
                    ShowToast($"Resend request failed: {ex.Message}");
                }
            }
        }
    }
}
