using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using WeShare.Core.Discovery;

namespace WeShare.UI.Views
{
    public partial class MainView
    {
        private void TitleBar_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (this.VisualRoot is Window window)
            {
                window.BeginMoveDrag(e);
            }
        }

        private void UpdateEmptyState()
        {
            if (RadarEmptyHint != null)
                RadarEmptyHint.IsVisible = ActiveReceivers.Count == 0;

            if (RadarNoDevicesCard != null)
                RadarNoDevicesCard.IsVisible = ActiveReceivers.Count == 0;

            if (SidebarDevicesEmpty != null)
                SidebarDevicesEmpty.IsVisible = Devices.Count == 0;
        }

        private void ShowPanel(Control panel, string title, Button? navBtn = null)
        {
            if (_isTransferInProgress && panel != ActiveTransferPanel)
            {
                ShowToast("A file transfer is in progress. Please wait for it to complete.");
                return;
            }

            if (SendDiscoveryPanel != null && SendDiscoveryPanel.IsVisible && panel != SendDiscoveryPanel)
            {
                try { _platformService.StopBluetoothDiscovery(); } catch { }
            }

            if (ActiveTransferPanel != null) ActiveTransferPanel.IsVisible = false;
            if (HomePanel != null) HomePanel.IsVisible = false;
            if (SettingsPanel != null) SettingsPanel.IsVisible = false;
            if (AboutPanel != null) AboutPanel.IsVisible = false;
            if (ReceiveModePanel != null) ReceiveModePanel.IsVisible = false;
            if (FilesPanel != null) FilesPanel.IsVisible = false;
            if (SendFilesPanel != null) SendFilesPanel.IsVisible = false;
            if (SendDiscoveryPanel != null) SendDiscoveryPanel.IsVisible = false;
            if (TransfersPanel != null) TransfersPanel.IsVisible = false;
            if (WebSharedPanel != null) WebSharedPanel.IsVisible = false;
            if (DeviceSessionPanel != null) DeviceSessionPanel.IsVisible = false;
            if (SendStepWizard != null) SendStepWizard.IsVisible = false;

            string targetRole = "Idle";
            if (panel == ReceiveModePanel)
            {
                targetRole = "Receiver";
            }
            else if (panel == SendFilesPanel || panel == SendDiscoveryPanel)
            {
                targetRole = "Sender";
            }

            string oldRole = _localDevice.Role;
            _localDevice.Role = targetRole;
            _localDevice.IsReceiver = (targetRole == "Receiver");

            panel.IsVisible = true;

            // Normalize technical titles to human-friendly labels
            string friendlyTitle = title switch
            {
                "HOME" or "COMMAND CENTER" => "Home",
                "RADAR DISCOVERY" or "CHOOSE RECIPIENT" => "Find a Device",
                "RECEIVE MODE" => "Ready to Receive",
                "SEND FILES" => "Pick Files",
                "TRANSFER HISTORY" => "Recent Transfers",
                "WEB PORTAL" or "WEB TRANSFER" => "Connect Phone",
                "ACTIVE TRANSFER" => "Transferring Files",
                "SETTINGS" => "Settings",
                "ABOUT" => "About",
                _ => title
            };
            PageTitle.Text = friendlyTitle;
            SetActiveNav(navBtn);

            if (!string.Equals(oldRole, targetRole, StringComparison.OrdinalIgnoreCase))
            {
                if (_discoveryService != null)
                {
                    _ = _discoveryService.BroadcastRoleAsync(targetRole);
                }
                _webDashboardService?.NotifyAllClients("refresh");
            }

            if (targetRole == "Receiver")
            {
                try { _platformService.StartBluetoothAdvertising(_localDevice); } catch { }
            }
            else
            {
                try { _platformService.StopBluetoothAdvertising(); } catch { }
            }
        }

        private void SetActiveNav(Button? activeBtn)
        {
            var buttons = new[] { NavHomeBtn, NavSendBtn, NavReceiveBtn, NavTransfersBtn, NavFilesBtn, NavWebSharedBtn, NavSettBtn, NavAboutBtn };
            foreach (var b in buttons)
            {
                if (b != null)
                {
                    b.Classes.Set("Active", b == activeBtn);
                }
            }
        }

        private void NavHome_Click(object? sender, RoutedEventArgs e)
        {
            ShowPanel(HomePanel, "Home", NavHomeBtn);
        }

        private void NavFiles_Click(object? sender, RoutedEventArgs e)
        {
            NavTransfers_Click(sender, e);
        }

        private void NavTransfers_Click(object? sender, RoutedEventArgs e) => ShowPanel(TransfersPanel, "Recent Transfers", NavTransfersBtn);

        private void NavWebShared_Click(object? sender, RoutedEventArgs e)
        {
            ShowPanel(WebSharedPanel, "Connect Phone", NavWebSharedBtn);
            UpdateWebSharedClientsList();
        }

        private void NavSettings_Click(object? sender, RoutedEventArgs e)
        {
            ShowPanel(SettingsPanel, "Settings", NavSettBtn);
        }

        private void NavAbout_Click(object? sender, RoutedEventArgs e)
        {
            ShowPanel(AboutPanel, "About", NavAboutBtn);
        }

        private void HomeSend_Click(object sender, RoutedEventArgs e)
        {
            _sendTarget = null;
            UpdateSendTargetUI();
            ShowPanel(SendFilesPanel, "Pick Files", NavSendBtn);
            SendStepWizard.IsVisible = true;
            Step1Indicator.Foreground = SolidColorBrush.Parse("#7C3AED");
            Step2Indicator.Foreground = SolidColorBrush.Parse("#64748B");
            UpdateQueueUI();
        }

        private void HomeReceive_Click(object sender, RoutedEventArgs e) => NavReceiveMode_Click(sender, e);

        private void NavSendFiles_Click(object sender, RoutedEventArgs e)
        {
            _sendTarget = null;
            UpdateSendTargetUI();
            ShowPanel(SendFilesPanel, "Pick Files", NavSendBtn);
            SendStepWizard.IsVisible = true;
            Step1Indicator.Foreground = SolidColorBrush.Parse("#7C3AED");
            Step2Indicator.Foreground = SolidColorBrush.Parse("#64748B");
            UpdateQueueUI();
        }

        private void NavSendFilesStage_Click(object? sender, RoutedEventArgs e)
        {
            UpdateSendTargetUI();
            ShowPanel(SendFilesPanel, "Pick Files", NavSendBtn);
            SendStepWizard.IsVisible = true;
            Step1Indicator.Foreground = SolidColorBrush.Parse("#7C3AED");
            Step2Indicator.Foreground = SolidColorBrush.Parse(_sendTarget != null ? "#7C3AED" : "#64748B");
            UpdateQueueUI();
        }

        private void NavSendDiscovery_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel(SendDiscoveryPanel, "Find a Device", NavSendBtn);
            SendStepWizard.IsVisible = true;
            Step1Indicator.Foreground = SolidColorBrush.Parse("#7C3AED");
            Step2Indicator.Foreground = SolidColorBrush.Parse("#7C3AED");

            UpdateEmptyState();
            _ = _discoveryService.BroadcastPresenceAsync();
            try { _platformService.StartBluetoothDiscovery(OnDeviceDiscovered); } catch { }
        }

        private void NavReceiveMode_Click(object sender, RoutedEventArgs e)
        {
            if (ReceiveDeviceNameText != null) ReceiveDeviceNameText.Text = _localDevice.Name;
            if (ReceiveDeviceIpText != null) ReceiveDeviceIpText.Text = $"{_localDevice.Type} • {UdpDiscoveryService.GetLocalIp()}:{_localDevice.Port}";
            ShowPanel(ReceiveModePanel, "Ready to Receive", NavReceiveBtn);
            ShowToast("✅ You're ready to receive! Nearby senders can now find you.", 3500);
            _ = _discoveryService.BroadcastPresenceAsync();
        }

        private void ShowToast(string message, int durationMs = 2800)
        {
            if (_isCapturingScreenshots) return;
            Dispatcher.UIThread.Post(async () =>
            {
                _toastCts?.Cancel();
                _toastCts = new CancellationTokenSource();
                var token = _toastCts.Token;

                ToastMessage.Text = message;
                ToastBorder.Opacity = 0;
                ToastBorder.IsVisible = true;
                ToastBorder.Classes.Add("ToastVisible");

                try
                {
                    await Task.Delay(100, token);
                    ToastBorder.Opacity = 1;
                    await Task.Delay(durationMs, token);

                    for (int i = 10; i >= 0; i--)
                    {
                        token.ThrowIfCancellationRequested();
                        ToastBorder.Opacity = i / 10.0;
                        await Task.Delay(20, token);
                    }
                    ToastBorder.IsVisible = false;
                    ToastBorder.Classes.Remove("ToastVisible");
                }
                catch (OperationCanceledException) { }
            });
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
    }
}
