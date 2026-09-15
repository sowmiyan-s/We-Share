namespace WeShare.Core.Transfer
{
    public partial class WebDashboardService
    {
        private string GetDashboardHtml() => @"<!DOCTYPE html>
<html lang=""en"" data-theme=""dark"">
<head>
<meta charset=""UTF-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no, viewport-fit=cover"">
<meta name=""description"" content=""We Share Web Portal - High-speed, secure local peer-to-peer file transfer between mobile and PC."">
<meta name=""theme-color"" content=""#080A10"">
<title>We Share | Web Portal</title>

<link rel=""preconnect"" href=""https://fonts.googleapis.com"">
<link rel=""preconnect"" href=""https://fonts.gstatic.com"" crossorigin>
<link href=""https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@400;500;600;700;800&display=swap"" rel=""stylesheet"">

<style>
:root {
  --bg: #07090E;
  --bg-subtle: #0D101A;
  --panel: rgba(16, 19, 31, 0.75);
  --panel-solid: #111422;
  --card: rgba(22, 26, 42, 0.65);
  --card-hover: rgba(28, 33, 54, 0.85);
  --card-solid: #171B2C;
  --border: rgba(124, 58, 237, 0.22);
  --border-subtle: rgba(255, 255, 255, 0.08);
  --primary: #7C3AED;
  --primary-light: #9333EA;
  --primary-gradient: linear-gradient(135deg, #7C3AED 0%, #9333EA 50%, #4F46E5 100%);
  --primary-glow: rgba(124, 58, 237, 0.1);
  --cyan: #06B6D4;
  --emerald: #10B981;
  --amber: #A855F7;
  --rose: #F43F5E;
  --text: #F8FAFC;
  --text-dim: #94A3B8;
  --text-muted: #64748B;
  --input-bg: rgba(15, 18, 29, 0.8);
  --backdrop-blur: blur(20px);
  --radius-sm: 8px;
  --radius-md: 14px;
  --radius-lg: 20px;
  --radius-full: 9999px;
  --shadow-glow: 0 8px 24px rgba(0, 0, 0, 0.4);
}

html[data-theme=""light""] {
  --bg: #F8FAFC;
  --bg-subtle: #EDF2F7;
  --panel: rgba(255, 255, 255, 0.85);
  --panel-solid: #FFFFFF;
  --card: rgba(241, 245, 249, 0.8);
  --card-hover: rgba(226, 232, 240, 0.9);
  --card-solid: #F1F5F9;
  --border: rgba(124, 58, 237, 0.2);
  --border-subtle: rgba(0, 0, 0, 0.08);
  --primary: #7C3AED;
  --primary-light: #8B5CF6;
  --primary-glow: rgba(124, 58, 237, 0.2);
  --text: #0F172A;
  --text-dim: #475569;
  --text-muted: #94A3B8;
  --input-bg: rgba(241, 245, 249, 0.9);
  --shadow-glow: 0 8 24 0 rgba(124, 58, 237, 0.12);
}

* {
  box-sizing: border-box;
  margin: 0;
  padding: 0;
  -webkit-tap-highlight-color: transparent;
}

body {
  background: var(--bg);
  color: var(--text);
  font-family: 'Plus Jakarta Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  min-height: 100vh;
  padding-bottom: 120px;
  overflow-x: hidden;
  position: relative;
}

/* Ambient Background Glow */
body::before {
  content: '';
  position: fixed;
  top: -120px;
  left: 50%;
  transform: translateX(-50%);
  width: 600px;
  height: 400px;
  background: radial-gradient(circle, rgba(124, 58, 237, 0.18) 0%, rgba(6, 182, 212, 0.08) 50%, transparent 70%);
  filter: blur(80px);
  pointer-events: none;
  z-index: 0;
}

.app-wrapper {
  max-width: 760px;
  width: 100%;
  margin: 0 auto;
  padding: 20px 24px;
  position: relative;
  z-index: 1;
  display: flex;
  flex-direction: column;
  gap: 20px;
}
@media (max-width: 640px) {
  .app-wrapper {
    padding: 12px 14px;
    gap: 14px;
  }
}

/* ------------------------------------------------------------- */
/* TOP APP BAR / BRAND HEADER                                    */
/* ------------------------------------------------------------- */
.app-header {
  background: var(--panel);
  backdrop-filter: var(--backdrop-blur);
  -webkit-backdrop-filter: var(--backdrop-blur);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  padding: 14px 18px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  box-shadow: var(--shadow-glow);
}

.brand-section {
  display: flex;
  align-items: center;
  gap: 12px;
}

.brand-logo-disc {
  width: 38px;
  height: 38px;
  border-radius: 12px;
  background: var(--card);
  border: 1px solid var(--border);
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
}

.brand-logo-disc img {
  width: 26px;
  height: 26px;
  object-fit: contain;
}

.brand-title-wrap {
  display: flex;
  flex-direction: column;
}

.brand-title {
  font-family: inherit;
  font-size: 16px;
  font-weight: 800;
  letter-spacing: 0.5px;
  color: var(--text);
  line-height: 1.2;
}

.brand-status-badge {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-size: 11px;
  color: var(--emerald);
  font-weight: 600;
}

.status-dot {
  width: 7px;
  height: 7px;
  border-radius: 50%;
  background: var(--emerald);
  box-shadow: 0 0 10px var(--emerald);
  animation: pulse-dot 2s infinite;
}

@keyframes pulse-dot {
  0% { transform: scale(0.95); opacity: 0.8; }
  50% { transform: scale(1.25); opacity: 1; }
  100% { transform: scale(0.95); opacity: 0.8; }
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 8px;
}

.header-btn {
  background: var(--card);
  border: 1px solid var(--border-subtle);
  color: var(--text);
  width: 38px;
  height: 38px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s ease;
}

.header-btn:hover {
  background: var(--card-hover);
  border-color: var(--primary);
  transform: translateY(-1px);
}

.header-btn svg {
  width: 17px;
  height: 17px;
}

/* Nickname Pill */
.nickname-pill {
  background: var(--card);
  border: 1px solid var(--border-subtle);
  border-radius: 20px;
  padding: 4px 10px;
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 11px;
  font-weight: 600;
  cursor: pointer;
}

.nickname-pill:hover {
  border-color: var(--primary);
}

/* ------------------------------------------------------------- */
/* MARQUEE SIGNAL BAR                                            */
/* ------------------------------------------------------------- */
.signal-ticker {
  background: rgba(124, 58, 237, 0.08);
  border: 1px solid rgba(124, 58, 237, 0.18);
  border-radius: 10px;
  padding: 6px 12px;
  overflow: hidden;
  white-space: nowrap;
  display: flex;
  align-items: center;
}

.ticker-content {
  display: inline-block;
  padding-left: 100%;
  animation: ticker-anim 28s linear infinite;
  font-size: 10px;
  font-weight: 700;
  letter-spacing: 1.5px;
  text-transform: uppercase;
  color: var(--primary-light);
}

@keyframes ticker-anim {
  0% { transform: translateX(0); }
  100% { transform: translateX(-100%); }
}

/* ------------------------------------------------------------- */
/* NAVIGATION TAB BAR                                            */
/* ------------------------------------------------------------- */
.tab-bar {
  background: var(--panel);
  backdrop-filter: var(--backdrop-blur);
  -webkit-backdrop-filter: var(--backdrop-blur);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-md);
  padding: 5px;
  display: flex;
  gap: 6px;
}

.tab-btn {
  flex: 1;
  background: transparent;
  border: none;
  border-radius: 10px;
  padding: 10px 8px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
  cursor: pointer;
  color: var(--text-dim);
  font-family: inherit;
  font-size: 11px;
  font-weight: 600;
  transition: all 0.2s ease;
  position: relative;
}

.tab-btn svg {
  width: 18px;
  height: 18px;
  transition: transform 0.2s ease;
}

.tab-btn:hover {
  color: var(--text);
}

.tab-btn.active {
  background: var(--primary-gradient);
  color: #FFFFFF;
  box-shadow: 0 4px 14px rgba(124, 58, 237, 0.4);
}

.tab-btn.active svg {
  transform: translateY(-1px);
}

.tab-badge {
  position: absolute;
  top: 4px;
  right: 12px;
  background: var(--cyan);
  color: #080A10;
  font-size: 9px;
  font-weight: 800;
  border-radius: 8px;
  padding: 1px 5px;
  min-width: 14px;
  text-align: center;
}

/* ------------------------------------------------------------- */
/* TAB CONTENT PANELS                                            */
/* ------------------------------------------------------------- */
.tab-view {
  display: none;
  flex-direction: column;
  gap: 16px;
  animation: fadeIn 0.22s ease forwards;
}

.tab-view.active {
  display: flex;
}

@keyframes fadeIn {
  from { opacity: 0; transform: translateY(6px); }
  to { opacity: 1; transform: translateY(0); }
}

/* ------------------------------------------------------------- */

/* ------------------------------------------------------------- */
/* 3-STEP WORKFLOW WIZARD                                        */
/* ------------------------------------------------------------- */
.web-step-wizard {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  background: var(--panel);
  backdrop-filter: var(--backdrop-blur);
  -webkit-backdrop-filter: var(--backdrop-blur);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-md);
  padding: 10px 16px;
}
.wsw-step {
  display: flex;
  align-items: center;
  gap: 7px;
  opacity: 0.45;
  transition: all 0.25s ease;
}
.wsw-step.active {
  opacity: 1;
}
.wsw-step.completed {
  opacity: 0.9;
}
.wsw-badge {
  width: 22px;
  height: 22px;
  border-radius: 11px;
  background: var(--card-solid);
  border: 1px solid var(--border);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 11px;
  font-weight: 700;
  color: var(--text-dim);
}
.wsw-step.active .wsw-badge {
  background: var(--primary);
  border-color: var(--primary);
  color: #fff;
}
.wsw-step.completed .wsw-badge {
  background: var(--emerald);
  border-color: var(--emerald);
  color: #fff;
}
.wsw-label {
  font-size: 12px;
  font-weight: 600;
  color: var(--text);
}
.wsw-connector {
  width: 20px;
  height: 2px;
  background: var(--border-subtle);
  border-radius: 1px;
  transition: background 0.25s ease;
}
.wsw-connector.active {
  background: var(--primary);
}

/* TARGET DEVICE SELECTOR STRIP                                  */
/* ------------------------------------------------------------- */
.device-select-card {
  background: var(--panel);
  backdrop-filter: var(--backdrop-blur);
  -webkit-backdrop-filter: var(--backdrop-blur);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-md);
  padding: 12px 16px;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.dsc-left {
  display: flex;
  align-items: center;
  gap: 10px;
}

.dsc-icon {
  width: 32px;
  height: 32px;
  border-radius: 8px;
  background: rgba(124, 58, 237, 0.15);
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--primary-light);
}

.dsc-icon svg {
  width: 16px;
  height: 16px;
}

.dsc-label {
  font-size: 11px;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 0.8px;
  font-weight: 700;
}

.dsc-select-wrap select {
  background: var(--card-solid);
  border: 1px solid var(--border);
  color: var(--text);
  font-family: inherit;
  font-size: 12px;
  font-weight: 600;
  padding: 6px 12px;
  border-radius: 8px;
  outline: none;
  cursor: pointer;
}

/* ------------------------------------------------------------- */
/* SEND TAB: MODERN MULTI-FILE DROPZONE                          */
/* ------------------------------------------------------------- */
.dropzone-card {
  background: var(--panel);
  backdrop-filter: var(--backdrop-blur);
  -webkit-backdrop-filter: var(--backdrop-blur);
  border: 2px dashed var(--border);
  border-radius: var(--radius-lg);
  padding: 36px 20px;
  text-align: center;
  position: relative;
  cursor: pointer;
  transition: all 0.25s ease;
  overflow: hidden;
}

.dropzone-card.dragover {
  border-color: var(--cyan);
  background: rgba(6, 182, 212, 0.08);
  transform: scale(1.01);
}

.dropzone-card:hover {
  border-color: var(--primary-light);
  background: rgba(124, 58, 237, 0.06);
}

.dz-icon-circle {
  width: 64px;
  height: 64px;
  border-radius: 50%;
  background: var(--primary-gradient);
  display: inline-flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 16px;
  box-shadow: 0 6px 20px var(--primary-glow);
  color: #FFFFFF;
}

.dz-icon-circle svg {
  width: 28px;
  height: 28px;
}

.dz-title {
  font-family: inherit;
  font-size: 18px;
  font-weight: 800;
  margin-bottom: 6px;
  color: var(--text);
}

.dz-sub {
  font-size: 12px;
  color: var(--text-dim);
  max-width: 320px;
  margin: 0 auto 20px;
  line-height: 1.5;
}

.dz-btn-group {
  display: flex;
  gap: 10px;
  justify-content: center;
  flex-wrap: wrap;
}

.dz-action-btn {
  background: var(--card);
  border: 1px solid var(--border);
  color: var(--text);
  padding: 10px 18px;
  border-radius: 10px;
  font-family: inherit;
  font-size: 12px;
  font-weight: 700;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  transition: all 0.2s ease;
}

.dz-action-btn:hover {
  background: var(--primary-gradient);
  color: #FFFFFF;
  border-color: transparent;
  transform: translateY(-1px);
}

.dz-action-btn svg {
  width: 15px;
  height: 15px;
}

/* ------------------------------------------------------------- */
/* STAGING TRAY (MULTIPLE SELECTED FILES)                        */
/* ------------------------------------------------------------- */
.staging-card {
  background: var(--panel);
  backdrop-filter: var(--backdrop-blur);
  -webkit-backdrop-filter: var(--backdrop-blur);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  padding: 18px;
  display: flex;
  flex-direction: column;
  gap: 14px;
  box-shadow: var(--shadow-glow);
}

.staging-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.staging-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-family: inherit;
  font-size: 14px;
  font-weight: 800;
  color: var(--text);
}

.staging-badge {
  background: rgba(124, 58, 237, 0.2);
  color: var(--primary-light);
  font-size: 11px;
  font-weight: 700;
  padding: 2px 8px;
  border-radius: 12px;
}

.staging-clear-btn {
  background: transparent;
  border: none;
  color: var(--rose);
  font-size: 11px;
  font-weight: 700;
  cursor: pointer;
  padding: 4px 8px;
  border-radius: 6px;
  transition: background 0.15s;
}

.staging-clear-btn:hover {
  background: rgba(244, 63, 94, 0.1);
}

.staging-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  max-height: 240px;
  overflow-y: auto;
  padding-right: 4px;
}

.staging-list::-webkit-scrollbar {
  width: 4px;
}
.staging-list::-webkit-scrollbar-thumb {
  background: rgba(124, 58, 237, 0.3);
  border-radius: 4px;
}

.staging-item {
  background: var(--card);
  border: 1px solid var(--border-subtle);
  border-radius: 10px;
  padding: 10px 12px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  transition: all 0.2s ease;
}

.staging-item:hover {
  border-color: var(--border);
  background: var(--card-hover);
}

.si-left {
  display: flex;
  align-items: center;
  gap: 10px;
  overflow: hidden;
  flex: 1;
}

.si-icon {
  width: 32px;
  height: 32px;
  border-radius: 8px;
  background: rgba(124, 58, 237, 0.15);
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--cyan);
  flex-shrink: 0;
}

.si-icon svg {
  width: 16px;
  height: 16px;
}

.si-info {
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.si-name {
  font-size: 12px;
  font-weight: 600;
  color: var(--text);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.si-size {
  font-size: 10px;
  font-family: inherit;
  color: var(--text-muted);
}

.si-remove {
  background: transparent;
  border: none;
  color: var(--text-muted);
  width: 26px;
  height: 26px;
  border-radius: 6px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.15s ease;
  flex-shrink: 0;
}

.si-remove:hover {
  color: var(--rose);
  background: rgba(244, 63, 94, 0.1);
}

.staging-footer {
  border-top: 1px solid var(--border-subtle);
  padding-top: 12px;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.staging-summary-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 11px;
  color: var(--text-dim);
}

.staging-summary-val {
  font-family: inherit;
  font-weight: 700;
  color: var(--cyan);
}

.send-all-btn {
  background: var(--primary-gradient);
  color: #FFFFFF;
  border: none;
  padding: 13px 20px;
  border-radius: var(--radius-md);
  font-family: inherit;
  font-size: 13px;
  font-weight: 800;
  letter-spacing: 0.5px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  box-shadow: 0 4px 18px var(--primary-glow);
  transition: all 0.2s ease;
}

.send-all-btn:hover {
  opacity: 0.95;
  transform: translateY(-1px);
}

.send-all-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
  transform: none;
}

.send-all-btn svg {
  width: 16px;
  height: 16px;
}

/* ------------------------------------------------------------- */
/* DOWNLOADS TAB                                                 */
/* ------------------------------------------------------------- */
.card-section {
  background: var(--panel);
  backdrop-filter: var(--backdrop-blur);
  -webkit-backdrop-filter: var(--backdrop-blur);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-lg);
  padding: 18px;
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.section-title {
  font-family: inherit;
  font-size: 15px;
  font-weight: 800;
  color: var(--text);
  display: flex;
  align-items: center;
  gap: 8px;
}

.file-cards-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.file-card-item {
  background: var(--card);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-md);
  padding: 12px 14px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  transition: all 0.2s ease;
}

.file-card-item:hover {
  background: var(--card-hover);
  border-color: var(--border);
}

.fci-left {
  display: flex;
  align-items: center;
  gap: 10px;
  overflow: hidden;
  flex: 1;
}

.fci-icon {
  width: 36px;
  height: 36px;
  border-radius: 10px;
  background: rgba(124, 58, 237, 0.15);
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--primary-light);
  flex-shrink: 0;
}

.fci-icon svg {
  width: 18px;
  height: 18px;
}

.fci-name {
  font-size: 13px;
  font-weight: 600;
  color: var(--text);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.fci-meta {
  font-size: 11px;
  font-family: inherit;
  color: var(--text-muted);
}

.fci-dl-btn {
  background: rgba(124, 58, 237, 0.15);
  color: var(--primary-light);
  border: 1px solid var(--border);
  padding: 8px 14px;
  border-radius: 8px;
  font-family: inherit;
  font-size: 11px;
  font-weight: 700;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  text-decoration: none;
  transition: all 0.2s ease;
  flex-shrink: 0;
}

.fci-dl-btn:hover {
  background: var(--primary-gradient);
  color: #FFFFFF;
  border-color: transparent;
}

.fci-dl-btn svg {
  width: 13px;
  height: 13px;
}

.empty-state-box {
  text-align: center;
  padding: 32px 16px;
  color: var(--text-muted);
}

.empty-state-box svg {
  width: 42px;
  height: 42px;
  margin-bottom: 10px;
  color: var(--border);
}

.empty-state-title {
  font-weight: 700;
  font-size: 13px;
  color: var(--text-dim);
  margin-bottom: 4px;
}

.empty-state-text {
  font-size: 11px;
  line-height: 1.5;
  max-width: 280px;
  margin: 0 auto;
}

/* ------------------------------------------------------------- */
/* ACTIVITY TAB                                                  */
/* ------------------------------------------------------------- */
.history-item {
  background: var(--card);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-md);
  padding: 10px 14px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.hi-left {
  display: flex;
  align-items: center;
  gap: 10px;
  overflow: hidden;
  flex: 1;
}

.hi-dir-badge {
  width: 28px;
  height: 28px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.hi-dir-badge.received {
  background: rgba(16, 185, 129, 0.15);
  color: var(--emerald);
}

.hi-dir-badge.sent {
  background: rgba(6, 182, 212, 0.15);
  color: var(--cyan);
}

.hi-dir-badge svg {
  width: 14px;
  height: 14px;
}

.hi-name {
  font-size: 12px;
  font-weight: 600;
  color: var(--text);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.hi-sub {
  font-size: 10px;
  color: var(--text-muted);
}

.hi-right {
  text-align: right;
  flex-shrink: 0;
}

.hi-status {
  font-size: 10px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.hi-status.completed { color: var(--emerald); }
.hi-status.failed { color: var(--rose); }

/* ------------------------------------------------------------- */
/* SHARE TAB                                                     */
/* ------------------------------------------------------------- */
.qr-container {
  display: flex;
  align-items: center;
  gap: 20px;
  padding: 8px 0;
}

.qr-box {
  background: #FFFFFF;
  border: 1px solid var(--border);
  border-radius: var(--radius-md);
  padding: 8px;
  width: 130px;
  height: 130px;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
}

.qr-box img {
  width: 100%;
  height: 100%;
  object-fit: contain;
}

.qr-info {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.qr-title {
  font-family: inherit;
  font-weight: 800;
  font-size: 15px;
  color: var(--text);
}

.qr-desc {
  font-size: 11px;
  color: var(--text-dim);
  line-height: 1.5;
}

.url-copy-box {
  display: flex;
  align-items: center;
  gap: 8px;
  background: var(--input-bg);
  border: 1px solid var(--border-subtle);
  border-radius: 8px;
  padding: 6px 10px;
}

.url-text {
  font-family: inherit;
  font-size: 11px;
  color: var(--cyan);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  flex: 1;
}

.copy-btn {
  background: rgba(124, 58, 237, 0.2);
  border: none;
  color: var(--primary-light);
  padding: 4px 10px;
  border-radius: 6px;
  font-size: 11px;
  font-weight: 700;
  cursor: pointer;
}

.copy-btn:hover {
  background: var(--primary);
  color: #FFFFFF;
}

/* ------------------------------------------------------------- */
/* CAPTIVE PORTAL NOTICE                                         */
/* ------------------------------------------------------------- */
.captive-banner {
  background: rgba(245, 158, 11, 0.1);
  border: 1px solid rgba(245, 158, 11, 0.3);
  border-radius: var(--radius-md);
  padding: 12px 14px;
  display: none;
  align-items: flex-start;
  gap: 12px;
}

.cb-icon {
  color: var(--amber);
  margin-top: 2px;
}

.cb-content {
  flex: 1;
}

.cb-title {
  font-size: 12px;
  font-weight: 700;
  color: var(--amber);
  margin-bottom: 2px;
}

.cb-text {
  font-size: 11px;
  color: var(--text-dim);
  line-height: 1.4;
  margin-bottom: 8px;
}

.cb-btn {
  background: var(--amber);
  color: #080A10;
  border: none;
  padding: 6px 12px;
  border-radius: 6px;
  font-family: inherit;
  font-size: 11px;
  font-weight: 800;
  cursor: pointer;
}

/* ------------------------------------------------------------- */
/* FLOATING LIVE TRANSFER HUD (BOTTOM SHEET / HUD)               */
/* ------------------------------------------------------------- */
.transfer-hud {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  background: rgba(12, 15, 26, 0.94);
  backdrop-filter: blur(24px);
  -webkit-backdrop-filter: blur(24px);
  border-top: 1px solid var(--border);
  box-shadow: 0 -10px 40px rgba(0, 0, 0, 0.6);
  padding: 16px 20px 24px;
  z-index: 2000;
  transform: translateY(110%);
  transition: transform 0.35s cubic-bezier(0.16, 1, 0.3, 1);
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.transfer-hud.active {
  transform: translateY(0);
}

.hud-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.hud-file-tag {
  display: flex;
  align-items: center;
  gap: 10px;
  overflow: hidden;
  flex: 1;
}

.hud-pulse-ring {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background: var(--cyan);
  box-shadow: 0 0 12px var(--cyan);
  animation: pulse-dot 1.5s infinite;
  flex-shrink: 0;
}

.hud-file-title {
  font-family: inherit;
  font-size: 13px;
  font-weight: 800;
  color: var(--text);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.hud-cancel-btn {
  background: rgba(244, 63, 94, 0.15);
  border: 1px solid rgba(244, 63, 94, 0.3);
  color: var(--rose);
  padding: 5px 12px;
  border-radius: 8px;
  font-size: 11px;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.15s ease;
}

.hud-cancel-btn:hover {
  background: var(--rose);
  color: #FFFFFF;
}

/* Progress bar */
.hud-bar-container {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.hud-bar-track {
  width: 100%;
  height: 8px;
  border-radius: 4px;
  background: rgba(255, 255, 255, 0.08);
  overflow: hidden;
  position: relative;
}

.hud-bar-fill {
  width: 0%;
  height: 100%;
  background: var(--primary-gradient);
  border-radius: 4px;
  transition: width 0.2s linear;
  box-shadow: 0 0 14px var(--primary-glow);
}

.hud-stats-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 11px;
  font-family: inherit;
}

.hud-speed {
  color: var(--cyan);
  font-weight: 700;
}

.hud-bytes {
  color: var(--text-dim);
}

.hud-eta {
  color: var(--primary-light);
  font-weight: 700;
}

/* Sparkline Wave Canvas */
.hud-sparkline-wrap {
  width: 100%;
  height: 38px;
  border-radius: 8px;
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid var(--border-subtle);
  overflow: hidden;
}

#hudSpeedGraph {
  width: 100%;
  height: 100%;
  display: block;
}

/* ------------------------------------------------------------- */
/* INCOMING FILE MODAL DIALOG                                    */
/* ------------------------------------------------------------- */
.modal-overlay {
  position: fixed;
  top: 0; left: 0; right: 0; bottom: 0;
  background: rgba(5, 7, 12, 0.85);
  backdrop-filter: blur(12px);
  -webkit-backdrop-filter: blur(12px);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20px;
  z-index: 3000;
  opacity: 0;
  pointer-events: none;
  transition: opacity 0.2s ease;
}

.modal-overlay.active {
  opacity: 1;
  pointer-events: auto;
}

.modal-sheet {
  background: var(--panel-solid);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  padding: 28px 24px;
  width: 100%;
  max-width: 400px;
  text-align: center;
  box-shadow: 0 20px 50px rgba(0, 0, 0, 0.7);
  transform: scale(0.95);
  transition: transform 0.2s ease;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.modal-overlay.active .modal-sheet {
  transform: scale(1);
}

.modal-icon-disc {
  width: 60px;
  height: 60px;
  border-radius: 30px;
  background: rgba(124, 58, 237, 0.15);
  color: var(--primary-light);
  display: inline-flex;
  align-items: center;
  justify-content: center;
  margin: 0 auto;
}

.modal-icon-disc svg {
  width: 28px;
  height: 28px;
}

.modal-title {
  font-family: inherit;
  font-size: 18px;
  font-weight: 800;
  color: var(--text);
}

.modal-desc {
  font-size: 13px;
  color: var(--text-dim);
  line-height: 1.5;
}

.modal-actions {
  display: flex;
  gap: 10px;
}

.modal-btn {
  flex: 1;
  padding: 12px;
  border-radius: 10px;
  font-family: inherit;
  font-size: 12px;
  font-weight: 800;
  cursor: pointer;
  border: none;
  transition: all 0.2s ease;
}

.modal-btn-decline {
  background: rgba(244, 63, 94, 0.12);
  color: var(--rose);
  border: 1px solid rgba(244, 63, 94, 0.25);
}

.modal-btn-accept {
  background: var(--primary-gradient);
  color: #FFFFFF;
  box-shadow: 0 4px 14px var(--primary-glow);
}

/* ------------------------------------------------------------- */
/* TOAST NOTIFICATION                                            */
/* ------------------------------------------------------------- */
.toast-pill {
  position: fixed;
  top: 20px;
  left: 50%;
  transform: translateX(-50%) translateY(-60px);
  background: var(--primary-gradient);
  color: #FFFFFF;
  font-size: 12px;
  font-weight: 700;
  padding: 10px 20px;
  border-radius: 20px;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  gap: 8px;
  z-index: 4000;
  opacity: 0;
  transition: all 0.3s cubic-bezier(0.16, 1, 0.3, 1);
  pointer-events: none;
}

.toast-pill.show {
  transform: translateX(-50%) translateY(0);
  opacity: 1;
}

.toast-pill svg {
  width: 16px;
  height: 16px;
}

/* Hidden Inputs */
input[type=""file""] {
  display: none;
}
</style>
</head>
<body>

<div class=""app-wrapper"">

  <!-- TOP HEADER -->
  <header class=""app-header"">
    <div class=""brand-section"">
      <div class=""brand-logo-disc"">
        <img src=""/api/logo"" alt=""WeShare Logo"">
      </div>
      <div class=""brand-title-wrap"">
        <span class=""brand-title"">WE SHARE</span>
        <span class=""brand-status-badge"">
          <span class=""status-dot""></span>
          <span id=""hostStatusText"">CONNECTING...</span>
        </span>
      </div>
    </div>

    <div class=""header-actions"">
      <div class=""nickname-pill"" onclick=""promptEditNickname()"" title=""Edit Nickname"">
        <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round"" style=""width:12px; height:12px; color:var(--primary-light);""><path d=""M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2""/><circle cx=""12"" cy=""7"" r=""4""/></svg>
        <span id=""nicknameDisplay"">Mobile Web</span>
      </div>
      <button class=""header-btn"" id=""themeToggleBtn"" onclick=""toggleTheme()"" title=""Toggle Theme"">
        <svg id=""themeIcon"" xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2.2"" stroke-linecap=""round"" stroke-linejoin=""round""><circle cx=""12"" cy=""12"" r=""4""/><path d=""M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M6.34 17.66l-1.41 1.41M19.07 4.93l-1.41 1.41""/></svg>
      </button>
    </div>
  </header>

  <!-- CAPTIVE PORTAL WARNING -->
  <div class=""captive-banner"" id=""captiveWarning"">
    <div class=""cb-icon"">
      <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round"" style=""width:20px;height:20px;""><path d=""M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z""/><line x1=""12"" y1=""9"" x2=""12"" y2=""13""/><line x1=""12"" y1=""17"" x2=""12.01"" y2=""17""/></svg>
    </div>
    <div class=""cb-content"">
      <div class=""cb-title"">Wi-Fi Sign-In Notice</div>
      <div class=""cb-text"">Authorize this connection to prevent mobile data fallback while transferring files.</div>
      <button class=""cb-btn"" onclick=""authorizeWifi()"">AUTHORIZE WI-FI</button>
    </div>
    <button onclick=""document.getElementById('captiveWarning').style.display='none'"" style=""background:none;border:none;color:var(--text-muted);cursor:pointer;font-size:16px;"">&times;</button>
  </div>

  <!-- MARQUEE TICKER -->
  <div class=""signal-ticker"">
    <div class=""ticker-content"">
      ULTRA-FAST LOCAL DIRECT TRANSFER • ZERO CLOUD STORAGE • P2P HIGH SPEED ACTIVE • WE SHARE PORTAL READY
    </div>
  </div>

  <!-- NAVIGATION TAB BAR -->
  <nav class=""tab-bar"">
    <button class=""tab-btn active"" id=""tabBtnSend"" onclick=""switchTab('send')"">
      <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M22 2L11 13""/><path d=""M22 2l-7 20-4-9-9-4 20-7z""/></svg>
      <span>Send</span>
      <span class=""tab-badge"" id=""stagingBadge"" style=""display:none;"">0</span>
    </button>
    <button class=""tab-btn"" id=""tabBtnDownloads"" onclick=""switchTab('downloads')"">
      <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4""/><polyline points=""7 10 12 15 17 10""/><line x1=""12"" y1=""15"" x2=""12"" y2=""3""/></svg>
      <span>Downloads</span>
      <span class=""tab-badge"" id=""downloadsBadge"" style=""display:none;"">0</span>
    </button>
    <button class=""tab-btn"" id=""tabBtnActivity"" onclick=""switchTab('activity')"">
      <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><circle cx=""12"" cy=""12"" r=""10""/><polyline points=""12 6 12 12 16 14""/></svg>
      <span>Activity</span>
    </button>
    <button class=""tab-btn"" id=""tabBtnShare"" onclick=""switchTab('share')"">
      <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><rect x=""3"" y=""3"" width=""7"" height=""7""/><rect x=""14"" y=""3"" width=""7"" height=""7""/><rect x=""14"" y=""14"" width=""7"" height=""7""/><rect x=""3"" y=""14"" width=""7"" height=""7""/></svg>
      <span>Share</span>
    </button>
  </nav>

  <!-- ========================================================= -->
  <!-- 1. SEND TAB VIEW (3-STEP INTUITIVE SEQUENCE)                 -->
  <!-- ========================================================= -->
  <div class=""tab-view active"" id=""viewSend"">

    <!-- 3-Step Sequence Wizard -->
    <div class=""web-step-wizard"" id=""webStepWizard"">
      <div class=""wsw-step active"" id=""wswStep1"">
        <div class=""wsw-badge"">1</div>
        <div class=""wsw-label"">Select Files</div>
      </div>
      <div class=""wsw-connector"" id=""wswConn1""></div>
      <div class=""wsw-step"" id=""wswStep2"">
        <div class=""wsw-badge"">2</div>
        <div class=""wsw-label"">Choose Device</div>
      </div>
      <div class=""wsw-connector"" id=""wswConn2""></div>
      <div class=""wsw-step"" id=""wswStep3"">
        <div class=""wsw-badge"">3</div>
        <div class=""wsw-label"">Transfer</div>
      </div>
    </div>

    <!-- Hidden Multi-File and Camera Inputs -->
    <input type=""file"" id=""multiFileInput"" multiple onchange=""onFilesSelected(this.files)"">
    <input type=""file"" id=""cameraInput"" accept=""image/*,video/*"" capture=""environment"" onchange=""onFilesSelected(this.files)"">

    <!-- Step 1: Interactive Dropzone -->
    <div class=""dropzone-card"" id=""dropZone"" onclick=""document.getElementById('multiFileInput').click()"">
      <div class=""dz-icon-circle"">
        <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2.2"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4""/><polyline points=""17 8 12 3 7 8""/><line x1=""12"" y1=""3"" x2=""12"" y2=""15""/></svg>
      </div>
      <div class=""dz-title"">Step 1: Select Files to Transfer</div>
      <div class=""dz-sub"">Tap or drop photos, 4K videos, documents, or archives of any size.</div>
      <div class=""dz-btn-group"" onclick=""event.stopPropagation()"">
        <button class=""dz-action-btn"" onclick=""document.getElementById('multiFileInput').click()"">
          <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z""/><polyline points=""14 2 14 8 20 8""/><line x1=""12"" y1=""18"" x2=""12"" y2=""12""/><line x1=""9"" y1=""15"" x2=""15"" y2=""15""/></svg>
          Browse Files
        </button>
        <button class=""dz-action-btn"" onclick=""document.getElementById('cameraInput').click()"">
          <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M23 19a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4l2-3h6l2 3h4a2 2 0 0 1 2 2z""/><circle cx=""12"" cy=""13"" r=""4""/></svg>
          Camera Roll
        </button>
      </div>
    </div>

    <!-- Step 2: Destination Device Selector -->
    <div class=""device-select-card"" id=""deviceSelectCard"">
      <div class=""dsc-left"">
        <div class=""dsc-icon"">
          <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><rect x=""2"" y=""3"" width=""20"" height=""14"" rx=""2"" ry=""2""/><line x1=""8"" y1=""21"" x2=""16"" y2=""21""/><line x1=""12"" y1=""17"" x2=""12"" y2=""21""/></svg>
        </div>
        <div>
          <div class=""dsc-label"">Step 2: Choose Destination Device</div>
          <div style=""font-size:12px; font-weight:700; color:var(--text);"" id=""targetNameLabel"">Host PC</div>
        </div>
      </div>
      <div class=""dsc-select-wrap"">
        <select id=""targetDeviceSelect"" onchange=""onTargetDeviceChanged()"">
          <option value=""pc"">Host PC</option>
        </select>
      </div>
    </div>

    <!-- Step 3: Staging Tray & Transfer Action -->
    <div class=""staging-card"" id=""stagingTray"" style=""display: none;"">
      <div class=""staging-header"">
        <div class=""staging-title"">
          <span>Step 3: Ready to Transfer</span>
          <span class=""staging-badge"" id=""stagingCountBadge"">0 files</span>
        </div>
        <button class=""staging-clear-btn"" onclick=""clearStagingTray()"">Clear All</button>
      </div>

      <div class=""staging-list"" id=""stagingList"">
        <!-- Rendered dynamically -->
      </div>

      <div class=""staging-footer"">
        <div class=""staging-summary-row"">
          <span>Total Batch Payload:</span>
          <span class=""staging-summary-val"" id=""stagingTotalSize"">0 MB</span>
        </div>
        <button class=""send-all-btn"" id=""sendAllBtn"" onclick=""startBatchSend()"">
          <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2.5"" stroke-linecap=""round"" stroke-linejoin=""round""><line x1=""22"" y1=""2"" x2=""11"" y2=""13""/><polygon points=""22 2 15 22 11 13 2 9 22 2""/></svg>
          <span id=""sendAllBtnText"">Start Transfer to Device →</span>
        </button>
      </div>
    </div>

  </div>

  <!-- ========================================================= -->
  <!-- 2. DOWNLOADS TAB VIEW                                     -->
  <!-- ========================================================= -->
  <div class=""tab-view"" id=""viewDownloads"">
    <div class=""card-section"">
      <div class=""section-header"">
        <div class=""section-title"">
          <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round"" style=""width:16px; height:16px; color:var(--primary-light);""><path d=""M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4""/><polyline points=""7 10 12 15 17 10""/><line x1=""12"" y1=""15"" x2=""12"" y2=""3""/></svg>
          <span>Available from PC</span>
        </div>
        <button onclick=""loadAvailableFiles()"" style=""background:none;border:none;color:var(--primary-light);font-size:11px;font-weight:700;cursor:pointer;"">REFRESH</button>
      </div>

      <div class=""file-cards-list"" id=""downloadFilesList"">
        <div class=""empty-state-box"">
          <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""1.8"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M13 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V9z""/><polyline points=""13 2 13 9 20 9""/></svg>
          <div class=""empty-state-title"">No Files Available</div>
          <div class=""empty-state-text"">Files shared from the desktop app will appear here for instant 1-click download.</div>
        </div>
      </div>
    </div>
  </div>

  <!-- ========================================================= -->
  <!-- 3. ACTIVITY TAB VIEW                                      -->
  <!-- ========================================================= -->
  <div class=""tab-view"" id=""viewActivity"">
    <div class=""card-section"">
      <div class=""section-header"">
        <div class=""section-title"">
          <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round"" style=""width:16px; height:16px; color:var(--primary-light);""><circle cx=""12"" cy=""12"" r=""10""/><polyline points=""12 6 12 12 16 14""/></svg>
          <span>Recent Transfers</span>
        </div>
        <button onclick=""loadHistory()"" style=""background:none;border:none;color:var(--primary-light);font-size:11px;font-weight:700;cursor:pointer;"">REFRESH</button>
      </div>

      <div class=""file-cards-list"" id=""historyItemsList"">
        <div class=""empty-state-box"">
          <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""1.8"" stroke-linecap=""round"" stroke-linejoin=""round""><polyline points=""22 12 18 12 15 21 9 3 6 12 2 12""/></svg>
          <div class=""empty-state-title"">No Transfers Yet</div>
          <div class=""empty-state-text"">Completed uploads and downloads between this device and the host will be recorded here.</div>
        </div>
      </div>
    </div>
  </div>

  <!-- ========================================================= -->
  <!-- 4. SHARE TAB VIEW                                         -->
  <!-- ========================================================= -->
  <div class=""tab-view"" id=""viewShare"">
    <div class=""card-section"">
      <div class=""section-header"">
        <div class=""section-title"">
          <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round"" style=""width:16px; height:16px; color:var(--primary-light);""><circle cx=""18"" cy=""5"" r=""3""/><circle cx=""6"" cy=""12"" r=""3""/><circle cx=""18"" cy=""19"" r=""3""/><line x1=""8.59"" y1=""13.51"" x2=""15.42"" y2=""17.49""/><line x1=""15.41"" y1=""6.51"" x2=""8.59"" y2=""10.49""/></svg>
          <span>Connect Another Device</span>
        </div>
      </div>

      <div class=""qr-container"">
        <div class=""qr-box"">
          <img src=""/api/qr"" alt=""Portal QR Code"">
        </div>
        <div class=""qr-info"">
          <div class=""qr-title"">Scan from Camera</div>
          <div class=""qr-desc"">Point your phone's camera at this QR code to instantly open the We Share Web Portal on any local device.</div>
        </div>
      </div>

      <div class=""url-copy-box"">
        <span class=""url-text"" id=""portalUrlDisplay"">http://...</span>
        <button class=""copy-btn"" onclick=""copyPortalUrl()"">COPY</button>
      </div>
    </div>
  </div>

</div>

<!-- =========================================================== -->
<!-- FLOATING LIVE TRANSFER HUD                                  -->
<!-- =========================================================== -->
<div class=""transfer-hud"" id=""transferHud"">
  <div class=""hud-header"">
    <div class=""hud-file-tag"">
      <div class=""hud-pulse-ring""></div>
      <div class=""hud-file-title"" id=""hudFileName"">Uploading file...</div>
    </div>
    <button class=""hud-cancel-btn"" onclick=""cancelActiveUpload()"">Abort</button>
  </div>

  <div class=""hud-bar-container"">
    <div class=""hud-bar-track"">
      <div class=""hud-bar-fill"" id=""hudBarFill""></div>
    </div>
    <div class=""hud-stats-row"">
      <span class=""hud-speed"" id=""hudSpeedText"">0.0 MB/s</span>
      <span class=""hud-bytes"" id=""hudBytesText"">0 MB / 0 MB</span>
      <span class=""hud-eta"" id=""hudEtaText"">ETA: --</span>
    </div>
  </div>

  <div class=""hud-sparkline-wrap"">
    <canvas id=""hudSpeedGraph""></canvas>
  </div>
</div>

<!-- =========================================================== -->
<!-- INCOMING TRANSFER BOTTOM MODAL (SINGLE FILE)                -->
<!-- =========================================================== -->
<div class=""modal-overlay"" id=""singleOfferModal"">
  <div class=""modal-sheet"">
    <div class=""modal-icon-disc"">
      <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4""/><polyline points=""7 10 12 15 17 10""/><line x1=""12"" y1=""15"" x2=""12"" y2=""3""/></svg>
    </div>
    <div class=""modal-title"">Incoming File</div>
    <div class=""modal-desc"" id=""singleOfferDesc"">The host PC is offering to send you a file.</div>
    <div class=""modal-actions"">
      <button class=""modal-btn modal-btn-decline"" onclick=""declineSingleOffer()"">Decline</button>
      <button class=""modal-btn modal-btn-accept"" onclick=""acceptSingleOffer()"">Accept & Download</button>
    </div>
  </div>
</div>

<!-- =========================================================== -->
<!-- INCOMING TRANSFER BOTTOM MODAL (BATCH FILES)                -->
<!-- =========================================================== -->
<div class=""modal-overlay"" id=""batchOfferModal"">
  <div class=""modal-sheet"">
    <div class=""modal-icon-disc"">
      <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z""/></svg>
    </div>
    <div class=""modal-title"" id=""batchOfferTitle"">Incoming Batch</div>
    <div class=""modal-desc"" id=""batchOfferDesc"">Multiple files incoming from PC.</div>
    <div class=""modal-actions"">
      <button class=""modal-btn modal-btn-decline"" onclick=""declineBatchOffer()"">Decline All</button>
      <button class=""modal-btn modal-btn-accept"" onclick=""acceptBatchOffer()"">Accept All</button>
    </div>
  </div>
</div>

<!-- TOAST PILL -->
<div class=""toast-pill"" id=""toastPill"">
  <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2.5"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M20 6L9 17l-5-5""/></svg>
  <span id=""toastText"">Action completed</span>
</div>

<script>
/* ------------------------------------------------------------- */
/* CLIENT STATE & IDENTITY                                       */
/* ------------------------------------------------------------- */
let currentTargetId = 'pc';
let stagedFiles = [];
let isUploading = false;
let currentXhr = null;
let speedHistory = [];
const MAX_SPEED_POINTS = 36;
let pendingSingleOffer = null;
let pendingBatchOffer = null;

function getClientId() {
  try {
    let id = localStorage.getItem('weshare_client_id');
    if (!id) {
      id = 'wc_' + Math.random().toString(36).substring(2, 10) + '_' + Date.now().toString(36);
      localStorage.setItem('weshare_client_id', id);
    }
    return id;
  } catch (e) {
    if (!window.weshare_mem_client_id) {
      window.weshare_mem_client_id = 'wc_mem_' + Math.random().toString(36).substring(2, 10);
    }
    return window.weshare_mem_client_id;
  }
}

function getSavedNickname() {
  try {
    return localStorage.getItem('weshare_nickname') || 'Mobile Web';
  } catch(e) {
    return 'Mobile Web';
  }
}

function saveNickname(name) {
  name = (name || '').trim();
  if (!name) return;
  try {
    localStorage.setItem('weshare_nickname', name);
  } catch(e) {}
  document.getElementById('nicknameDisplay').textContent = name;
  fetch('/api/heartbeat?clientId=' + getClientId() + '&name=' + encodeURIComponent(name), { method: 'POST' }).catch(() => {});
}

function promptEditNickname() {
  const cur = getSavedNickname();
  const res = prompt('Enter your device nickname:', cur);
  if (res && res.trim() && res.trim() !== cur) {
    saveNickname(res.trim());
    showToast('Nickname updated: ' + res.trim());
  }
}

/* ------------------------------------------------------------- */
/* THEME CONTROLLER                                              */
/* ------------------------------------------------------------- */
function initTheme() {
  let theme = 'dark';
  try {
    theme = localStorage.getItem('weshare_theme') || 'dark';
  } catch(e) {}
  document.documentElement.setAttribute('data-theme', theme);
  updateThemeIcon(theme);
}

function toggleTheme() {
  const cur = document.documentElement.getAttribute('data-theme') || 'dark';
  const nxt = cur === 'dark' ? 'light' : 'dark';
  document.documentElement.setAttribute('data-theme', nxt);
  try { localStorage.setItem('weshare_theme', nxt); } catch(e) {}
  updateThemeIcon(nxt);
}

function updateThemeIcon(theme) {
  const icon = document.getElementById('themeIcon');
  if (theme === 'light') {
    icon.innerHTML = '<path d=""M12 3a6 6 0 0 0 9 9 9 9 0 1 1-9-9Z""/>';
  } else {
    icon.innerHTML = '<circle cx=""12"" cy=""12"" r=""4""/><path d=""M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M6.34 17.66l-1.41 1.41M19.07 4.93l-1.41 1.41""/>';
  }
}

/* ------------------------------------------------------------- */
/* TAB SWITCHING                                                 */
/* ------------------------------------------------------------- */
function switchTab(name) {
  document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
  document.querySelectorAll('.tab-view').forEach(v => v.classList.remove('active'));

  if (name === 'send') {
    document.getElementById('tabBtnSend').classList.add('active');
    document.getElementById('viewSend').classList.add('active');
  } else if (name === 'downloads') {
    document.getElementById('tabBtnDownloads').classList.add('active');
    document.getElementById('viewDownloads').classList.add('active');
    loadAvailableFiles();
  } else if (name === 'activity') {
    document.getElementById('tabBtnActivity').classList.add('active');
    document.getElementById('viewActivity').classList.add('active');
    loadHistory();
  } else if (name === 'share') {
    document.getElementById('tabBtnShare').classList.add('active');
    document.getElementById('viewShare').classList.add('active');
  }
}

/* ------------------------------------------------------------- */
/* TOAST                                                         */
/* ------------------------------------------------------------- */
let toastTimeout = null;
function showToast(msg) {
  const t = document.getElementById('toastPill');
  document.getElementById('toastText').textContent = msg;
  t.classList.add('show');
  if (toastTimeout) clearTimeout(toastTimeout);
  toastTimeout = setTimeout(() => {
    t.classList.remove('show');
  }, 3200);
}

/* ------------------------------------------------------------- */
/* FORMATTING UTILS                                              */
/* ------------------------------------------------------------- */
function formatBytes(bytes) {
  if (!bytes || bytes === 0) return '0 B';
  const k = 1024;
  const dm = 1;
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
}

function getFileCategorySvg(name) {
  const ext = (name.split('.').pop() || '').toLowerCase();
  // Video
  if (['mp4', 'mkv', 'mov', 'avi', 'wmv', 'flv', 'webm', 'm4v', '3gp'].includes(ext)) {
    return '<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><polygon points=""23 7 16 12 23 17 23 7""/><rect x=""1"" y=""5"" width=""15"" height=""14"" rx=""2"" ry=""2""/></svg>';
  }
  // Image
  if (['jpg', 'jpeg', 'png', 'gif', 'webp', 'svg', 'bmp', 'heic', 'heif', 'ico', 'avif'].includes(ext)) {
    return '<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><rect x=""3"" y=""3"" width=""18"" height=""18"" rx=""2"" ry=""2""/><circle cx=""8.5"" cy=""8.5"" r=""1.5""/><polyline points=""21 15 16 10 5 21""/></svg>';
  }
  // Audio
  if (['mp3', 'wav', 'flac', 'aac', 'ogg', 'm4a', 'wma'].includes(ext)) {
    return '<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M9 18V5l12-2v13""/><circle cx=""6"" cy=""18"" r=""3""/><circle cx=""18"" cy=""16"" r=""3""/></svg>';
  }
  // Archive
  if (['zip', 'rar', '7z', 'tar', 'gz', 'bz2', 'iso'].includes(ext)) {
    return '<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M21 8v13H3V8""/><path d=""M1 3h22v5H1z""/><path d=""M10 12h4""/></svg>';
  }
  // Document
  if (['pdf', 'doc', 'docx', 'txt', 'rtf', 'odt', 'xls', 'xlsx', 'ppt', 'pptx'].includes(ext)) {
    return '<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z""/><polyline points=""14 2 14 8 20 8""/><line x1=""16"" y1=""13"" x2=""8"" y2=""13""/><line x1=""16"" y1=""17"" x2=""8"" y2=""17""/></svg>';
  }
  // Generic
  return '<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M13 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V9z""/><polyline points=""13 2 13 9 20 9""/></svg>';
}

/* ------------------------------------------------------------- */
/* STAGING TRAY HANDLER (MULTIPLE FILES)                         */
/* ------------------------------------------------------------- */
function onFilesSelected(fileList) {
  if (!fileList || fileList.length === 0) return;
  for (let i = 0; i < fileList.length; i++) {
    stagedFiles.push(fileList[i]);
  }
  // Reset input values so picking the same file again triggers change event
  document.getElementById('multiFileInput').value = '';
  document.getElementById('cameraInput').value = '';
  renderStagingTray();
  showToast('Added ' + fileList.length + ' file(s) to queue');
}

function removeStagedFile(idx) {
  if (isUploading) return;
  stagedFiles.splice(idx, 1);
  renderStagingTray();
}

function clearStagingTray() {
  if (isUploading) return;
  stagedFiles = [];
  renderStagingTray();
}

function renderStagingTray() {
  const tray = document.getElementById('stagingTray');
  const list = document.getElementById('stagingList');
  const countBadge = document.getElementById('stagingCountBadge');
  const tabBadge = document.getElementById('stagingBadge');
  const totalSize = document.getElementById('stagingTotalSize');
  const btnText = document.getElementById('sendAllBtnText');

  if (stagedFiles.length === 0) {
    tray.style.display = 'none';
    tabBadge.style.display = 'none';
    const s1 = document.getElementById('wswStep1'), s2 = document.getElementById('wswStep2'), s3 = document.getElementById('wswStep3');
    const c1 = document.getElementById('wswConn1'), c2 = document.getElementById('wswConn2');
    if (s1) s1.className = 'wsw-step active';
    if (s2) s2.className = 'wsw-step';
    if (s3) s3.className = 'wsw-step';
    if (c1) c1.className = 'wsw-connector';
    if (c2) c2.className = 'wsw-connector';
    return;
  }
  const s1 = document.getElementById('wswStep1'), s2 = document.getElementById('wswStep2'), s3 = document.getElementById('wswStep3');
  const c1 = document.getElementById('wswConn1'), c2 = document.getElementById('wswConn2');
  if (s1) s1.className = 'wsw-step completed';
  if (c1) c1.className = 'wsw-connector active';
  if (s2) s2.className = 'wsw-step active';
  if (c2) c2.className = 'wsw-connector';
  if (s3) s3.className = 'wsw-step';

  tray.style.display = 'flex';
  tabBadge.style.display = 'inline-block';
  tabBadge.textContent = stagedFiles.length;
  countBadge.textContent = stagedFiles.length + (stagedFiles.length === 1 ? ' file' : ' files');

  let combinedBytes = 0;
  list.innerHTML = '';

  stagedFiles.forEach((f, idx) => {
    combinedBytes += f.size;
    const item = document.createElement('div');
    item.className = 'staging-item';
    item.id = 'stagedItem_' + idx;
    item.innerHTML = `
      <div class=""si-left"">
        <div class=""si-icon"">
          ${getFileCategorySvg(f.name)}
        </div>
        <div class=""si-info"">
          <span class=""si-name"" title=""${f.name}"">${f.name}</span>
          <span class=""si-size"">${formatBytes(f.size)}</span>
        </div>
      </div>
      <button class=""si-remove"" onclick=""removeStagedFile(${idx})"" title=""Remove file"" ${isUploading ? 'disabled style=""opacity:0.3""' : ''}>
        <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2.5"" stroke-linecap=""round"" stroke-linejoin=""round"" style=""width:14px; height:14px;""><line x1=""18"" y1=""6"" x2=""6"" y2=""18""/><line x1=""6"" y1=""6"" x2=""18"" y2=""18""/></svg>
      </button>
    `;
    list.appendChild(item);
  });

  totalSize.textContent = formatBytes(combinedBytes);
  btnText.textContent = `Send ${stagedFiles.length} File${stagedFiles.length > 1 ? 's' : ''} (${formatBytes(combinedBytes)})`;
}

/* ------------------------------------------------------------- */
/* BATCH FILE STREAMING UPLOADER                                 */
/* ------------------------------------------------------------- */
async function startBatchSend() {
  if (stagedFiles.length === 0 || isUploading) return;
  isUploading = true;
  document.getElementById('sendAllBtn').disabled = true;
  const s2 = document.getElementById('wswStep2'), s3 = document.getElementById('wswStep3'), c2 = document.getElementById('wswConn2');
  if (s2) s2.className = 'wsw-step completed';
  if (c2) c2.className = 'wsw-connector active';
  if (s3) s3.className = 'wsw-step active';

  const total = stagedFiles.length;
  const hud = document.getElementById('transferHud');
  hud.classList.add('active');

  const clientId = getClientId();
  const targetId = document.getElementById('targetDeviceSelect').value || 'pc';

  let successCount = 0;
  let failMessage = '';

  for (let i = 0; i < stagedFiles.length; i++) {
    const file = stagedFiles[i];
    
    // Highlight staged item in list
    const itemEl = document.getElementById('stagedItem_' + i);
    if (itemEl) itemEl.style.borderColor = 'var(--cyan)';

    // Update HUD titles
    document.getElementById('hudFileName').textContent = `[${i + 1}/${total}] ${file.name}`;
    updateHudProgress(0, file.size, 0, '--');
    speedHistory = [];
    drawSpeedGraph();

    try {
      // 1. Request permission
      const askUrl = '/api/ask-receive?clientId=' + clientId + '&targetId=' + targetId + '&name=' + encodeURIComponent(file.name) + '&size=' + file.size;
      const askRes = await fetch(askUrl, { method: 'POST' }).then(r => r.json());

      if (!askRes.accepted) {
        throw new Error(askRes.error || 'Transfer declined by recipient');
      }

      // 2. Stream raw file body chunk by chunk without memory bloat
      await streamFileUpload(file, askRes.id, clientId);
      successCount++;

      if (itemEl) {
        itemEl.style.borderColor = 'var(--emerald)';
        itemEl.style.opacity = '0.7';
      }
    } catch (err) {
      console.error('File upload failed:', err);
      failMessage = err.message || 'Upload error';
      if (itemEl) itemEl.style.borderColor = 'var(--rose)';
      if (failMessage.includes('declined') || failMessage.includes('aborted')) {
        break;
      }
    }
  }

  isUploading = false;
  document.getElementById('sendAllBtn').disabled = false;
  hud.classList.remove('active');

  if (successCount === total) {
    showToast(`Successfully transferred all ${total} files!`);
    stagedFiles = [];
    renderStagingTray();
  } else if (successCount > 0) {
    showToast(`Transferred ${successCount} of ${total} files. (${failMessage})`);
    stagedFiles.splice(0, successCount);
    renderStagingTray();
  } else {
    showToast(`Upload failed: ${failMessage}`);
  }

  loadHistory();
}

let currentUploadResolve = null;

function streamFileUpload(file, uploadId, clientId) {
  return new Promise((resolve, reject) => {
    let isFinished = false;

    const safeResolve = () => {
      if (isFinished) return;
      isFinished = true;
      currentXhr = null;
      currentUploadResolve = null;
      updateHudProgress(100, file.size, file.size, rollingSpeed, 'Done');
      resolve();
    };

    const safeReject = (err) => {
      if (isFinished) return;
      isFinished = true;
      currentXhr = null;
      currentUploadResolve = null;
      reject(err);
    };

    currentUploadResolve = safeResolve;

    const xhr = new XMLHttpRequest();
    currentXhr = xhr;

    xhr.open('POST', '/upload?clientId=' + encodeURIComponent(clientId) + '&id=' + encodeURIComponent(uploadId));
    xhr.setRequestHeader('X-File-Name', encodeURIComponent(file.name));

    let lastTime = performance.now();
    let lastLoaded = 0;
    let rollingSpeed = 0;

    xhr.upload.onprogress = (e) => {
      if (!e.lengthComputable) return;
      const now = performance.now();
      const elapsed = (now - lastTime) / 1000;

      if (elapsed >= 0.2 || e.loaded === e.total) {
        const bytesDiff = e.loaded - lastLoaded;
        const currentSpeed = elapsed > 0 ? ((bytesDiff / elapsed) / 1000000) : 0; // MB/s
        rollingSpeed = rollingSpeed === 0 ? currentSpeed : (rollingSpeed * 0.7 + currentSpeed * 0.3);

        const percent = Math.min(100, Math.round((e.loaded / e.total) * 100));
        let etaStr = '--';
        if (rollingSpeed > 0.05) {
          const remSeconds = Math.max(0, Math.round((e.total - e.loaded) / (rollingSpeed * 1000000)));
          etaStr = remSeconds < 60 ? `${remSeconds}s` : `${Math.floor(remSeconds / 60)}m ${remSeconds % 60}s`;
        }

        updateHudProgress(percent, e.total, e.loaded, rollingSpeed, etaStr);
        speedHistory.push(rollingSpeed);
        if (speedHistory.length > MAX_SPEED_POINTS) speedHistory.shift();
        drawSpeedGraph();

        lastTime = now;
        lastLoaded = e.loaded;
      }
    };

    xhr.upload.onload = () => {
      updateHudProgress(100, file.size, file.size, rollingSpeed, 'Finalizing...');
      // Fallback: If 100% of payload was accepted by socket, ensure we don't stall
      setTimeout(() => {
        if (!isFinished && isUploading) {
          safeResolve();
        }
      }, 2500);
    };

    xhr.onload = () => {
      if (xhr.status >= 200 && xhr.status < 300) {
        safeResolve();
      } else {
        safeReject(new Error(`Server response ${xhr.status}`));
      }
    };

    xhr.onerror = () => {
      // If socket teardown happened after bytes arrived, check if we can safely resolve
      setTimeout(() => {
        if (!isFinished) safeReject(new Error('Network connection error'));
      }, 500);
    };

    xhr.onabort = () => {
      safeReject(new Error('Upload aborted by user'));
    };

    xhr.send(file);
  });
}

function cancelActiveUpload() {
  if (currentXhr) {
    currentXhr.abort();
    currentXhr = null;
  }
  isUploading = false;
  document.getElementById('transferHud').classList.remove('active');
  document.getElementById('sendAllBtn').disabled = false;
  showToast('Transfer canceled');
}

function updateHudProgress(percent, totalBytes, loadedBytes, speedMb, etaStr) {
  document.getElementById('hudBarFill').style.width = percent + '%';
  document.getElementById('hudSpeedText').textContent = (speedMb || 0).toFixed(1) + ' MB/s';
  document.getElementById('hudBytesText').textContent = `${formatBytes(loadedBytes)} / ${formatBytes(totalBytes)} (${percent}%)`;
  document.getElementById('hudEtaText').textContent = etaStr ? `ETA: ${etaStr}` : 'ETA: --';
}

function drawSpeedGraph() {
  const canvas = document.getElementById('hudSpeedGraph');
  if (!canvas) return;
  const ctx = canvas.getContext('2d');
  const w = canvas.width = canvas.offsetWidth;
  const h = canvas.height = canvas.offsetHeight;

  ctx.clearRect(0, 0, w, h);
  if (speedHistory.length < 2) return;

  const maxVal = Math.max(...speedHistory, 1.0);
  const step = w / (MAX_SPEED_POINTS - 1);

  ctx.beginPath();
  const startIdx = MAX_SPEED_POINTS - speedHistory.length;
  for (let i = 0; i < speedHistory.length; i++) {
    const x = (startIdx + i) * step;
    const y = h - (speedHistory[i] / maxVal) * (h - 8) - 4;
    if (i === 0) ctx.moveTo(x, y);
    else ctx.lineTo(x, y);
  }

  ctx.strokeStyle = '#06B6D4';
  ctx.lineWidth = 2;
  ctx.stroke();

  // Fill gradient
  ctx.lineTo((startIdx + speedHistory.length - 1) * step, h);
  ctx.lineTo(startIdx * step, h);
  ctx.closePath();
  const grad = ctx.createLinearGradient(0, 0, 0, h);
  grad.addColorStop(0, 'rgba(6, 182, 212, 0.25)');
  grad.addColorStop(1, 'rgba(6, 182, 212, 0.0)');
  ctx.fillStyle = grad;
  ctx.fill();
}

/* ------------------------------------------------------------- */
/* DRAG AND DROP HANDLERS                                        */
/* ------------------------------------------------------------- */
const dz = document.getElementById('dropZone');
['dragenter', 'dragover'].forEach(name => {
  dz.addEventListener(name, (e) => {
    e.preventDefault();
    e.stopPropagation();
    dz.classList.add('dragover');
  });
});
['dragleave', 'drop'].forEach(name => {
  dz.addEventListener(name, (e) => {
    e.preventDefault();
    e.stopPropagation();
    dz.classList.remove('dragover');
  });
});
dz.addEventListener('drop', (e) => {
  if (e.dataTransfer && e.dataTransfer.files) {
    onFilesSelected(e.dataTransfer.files);
  }
});

/* ------------------------------------------------------------- */
/* SERVER API CALLS & SSE                                        */
/* ------------------------------------------------------------- */
async function loadHostInfo() {
  try {
    const res = await fetch('/api/me').then(r => r.json());
    if (res && res.name) {
      document.getElementById('hostStatusText').textContent = 'CONNECTED: ' + res.name.toUpperCase();
      document.getElementById('targetNameLabel').textContent = res.name;
    }
  } catch(e) {
    document.getElementById('hostStatusText').textContent = 'CONNECTING...';
  }
}

async function loadDiscoveredDevices() {
  try {
    const devices = await fetch('/api/devices').then(r => r.json());
    const sel = document.getElementById('targetDeviceSelect');
    const curVal = sel.value;
    sel.innerHTML = '<option value=""pc"">Host PC</option>';
    const myId = getClientId();
    if (Array.isArray(devices)) {
      devices.forEach(d => {
        if (!d) return;
        if (d.id === myId || d.id === 'wc_' + myId || (myId && d.id && d.id.includes(myId))) return;
        const opt = document.createElement('option');
        opt.value = d.id;
        opt.textContent = (d.isFavorite ? '• ' : '') + d.name;
        sel.appendChild(opt);
      });
    }
    sel.value = curVal || 'pc';
  } catch(e) {}
}

function onTargetDeviceChanged() {
  const sel = document.getElementById('targetDeviceSelect');
  document.getElementById('targetNameLabel').textContent = sel.options[sel.selectedIndex].text;
}

async function loadAvailableFiles() {
  try {
    const files = await fetch('/api/files').then(r => r.json());
    const list = document.getElementById('downloadFilesList');
    const badge = document.getElementById('downloadsBadge');

    if (!Array.isArray(files) || files.length === 0) {
      badge.style.display = 'none';
      list.innerHTML = `
        <div class=""empty-state-box"">
          <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""1.8"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M13 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V9z""/><polyline points=""13 2 13 9 20 9""/></svg>
          <div class=""empty-state-title"">No Files Available</div>
          <div class=""empty-state-text"">Files shared from the desktop app will appear here for instant 1-click download.</div>
        </div>`;
      return;
    }

    badge.style.display = 'inline-block';
    badge.textContent = files.length;
    list.innerHTML = '';

    files.forEach(f => {
      const item = document.createElement('div');
      item.className = 'file-card-item';
      item.innerHTML = `
        <div class=""fci-left"">
          <div class=""fci-icon"">${getFileCategorySvg(f.name)}</div>
          <div style=""display:flex; flex-direction:column; overflow:hidden;"">
            <span class=""fci-name"" title=""${f.name}"">${f.name}</span>
            <span class=""fci-meta"">${formatBytes(f.size)}</span>
          </div>
        </div>
        <a class=""fci-dl-btn"" href=""/download?file=${encodeURIComponent(f.name)}"" download>
          <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2.5"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4""/><polyline points=""7 10 12 15 17 10""/><line x1=""12"" y1=""15"" x2=""12"" y2=""3""/></svg>
          Download
        </a>
      `;
      list.appendChild(item);
    });
  } catch(e) {}
}

async function loadHistory() {
  try {
    const list = document.getElementById('historyItemsList');
    const items = await fetch('/api/history').then(r => r.json());

    if (!Array.isArray(items) || items.length === 0) {
      list.innerHTML = `
        <div class=""empty-state-box"">
          <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""1.8"" stroke-linecap=""round"" stroke-linejoin=""round""><polyline points=""22 12 18 12 15 21 9 3 6 12 2 12""/></svg>
          <div class=""empty-state-title"">No Transfers Yet</div>
          <div class=""empty-state-text"">Completed uploads and downloads between this device and the host will be recorded here.</div>
        </div>`;
      return;
    }

    list.innerHTML = '';
    items.forEach(h => {
      const isRecv = (h.direction === 0 || h.direction === 'Received');
      const isDone = (h.status === 2 || h.status === 'Completed');
      const dateStr = h.timestamp ? new Date(h.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '';

      const item = document.createElement('div');
      item.className = 'history-item';
      item.innerHTML = `
        <div class=""hi-left"">
          <div class=""hi-dir-badge ${isRecv ? 'received' : 'sent'}"">
            ${isRecv
              ? '<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2.5"" stroke-linecap=""round"" stroke-linejoin=""round""><line x1=""12"" y1=""5"" x2=""12"" y2=""19""/><polyline points=""19 12 12 19 5 12""/></svg>'
              : '<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2.5"" stroke-linecap=""round"" stroke-linejoin=""round""><line x1=""12"" y1=""19"" x2=""12"" y2=""5""/><polyline points=""5 12 12 5 19 12""/></svg>'}
          </div>
          <div style=""display:flex; flex-direction:column; overflow:hidden;"">
            <span class=""hi-name"" title=""${h.fileName}"">${h.fileName}</span>
            <span class=""hi-sub"">${formatBytes(h.totalBytes)} • ${isRecv ? 'From PC' : 'Sent to PC'} • ${dateStr}</span>
          </div>
        </div>
        <div class=""hi-right"">
          <span class=""hi-status ${isDone ? 'completed' : 'failed'}"">${isDone ? 'COMPLETED' : 'FAILED'}</span>
        </div>
      `;
      list.appendChild(item);
    });
  } catch(e) {}
}

function initSSE() {
  const cid = getClientId();
  const sse = new EventSource('/api/events?clientId=' + cid);

  sse.addEventListener('offer', (e) => {
    try {
      pendingSingleOffer = JSON.parse(e.data);
      document.getElementById('singleOfferDesc').textContent = `Incoming file from ${pendingSingleOffer.from || 'Host PC'}: ""${pendingSingleOffer.name}"" (${formatBytes(pendingSingleOffer.size)})`;
      document.getElementById('singleOfferModal').classList.add('active');
    } catch(err) {}
  });

  sse.addEventListener('batch-offer', (e) => {
    try {
      pendingBatchOffer = JSON.parse(e.data);
      document.getElementById('batchOfferTitle').textContent = `Incoming Batch (${pendingBatchOffer.files?.length || 0} Files)`;
      document.getElementById('batchOfferDesc').textContent = `Total size: ${formatBytes(pendingBatchOffer.totalSize)} from ${pendingBatchOffer.from || 'Host PC'}`;
      document.getElementById('batchOfferModal').classList.add('active');
    } catch(err) {}
  });

  sse.addEventListener('progress', (e) => {
    try {
      const p = JSON.parse(e.data);
      if (isUploading && p.percent) {
        // Can be used to sync server-side progress
      }
    } catch(err) {}
  });

  sse.addEventListener('upload-complete', (e) => {
    loadHistory();
    loadAvailableFiles();
    if (typeof currentUploadResolve === 'function') {
      currentUploadResolve();
    }
  });

  sse.addEventListener('refresh', () => {
    loadHostInfo();
    loadAvailableFiles();
    loadDiscoveredDevices();
  });

  sse.onerror = () => {
    document.getElementById('hostStatusText').textContent = 'RECONNECTING...';
  };

  sse.onopen = () => {
    loadHostInfo();
  };
}

function acceptSingleOffer() {
  const modal = document.getElementById('singleOfferModal');
  modal.classList.remove('active');
  if (pendingSingleOffer) {
    window.location.href = `/download?file=${encodeURIComponent(pendingSingleOffer.name)}&fileId=${pendingSingleOffer.fileId}`;
    showToast('Download started');
  }
}

function declineSingleOffer() {
  const modal = document.getElementById('singleOfferModal');
  modal.classList.remove('active');
  if (pendingSingleOffer) {
    fetch('/api/decline?id=' + pendingSingleOffer.fileId, { method: 'POST' }).catch(() => {});
  }
}

function acceptBatchOffer() {
  const modal = document.getElementById('batchOfferModal');
  modal.classList.remove('active');
  if (pendingBatchOffer && Array.isArray(pendingBatchOffer.files)) {
    pendingBatchOffer.files.forEach((f, idx) => {
      setTimeout(() => {
        const link = document.createElement('a');
        link.href = `/download?file=${encodeURIComponent(f.name)}&fileId=${f.fileId || ''}`;
        link.download = f.name;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
      }, idx * 600);
    });
    showToast(`Downloading ${pendingBatchOffer.files.length} files...`);
  }
}

function declineBatchOffer() {
  const modal = document.getElementById('batchOfferModal');
  modal.classList.remove('active');
  if (pendingBatchOffer) {
    fetch('/api/decline?id=' + (pendingBatchOffer.batchId || ''), { method: 'POST' }).catch(() => {});
  }
}

function authorizeWifi() {
  fetch('/api/portal-login', { method: 'POST' }).then(() => {
    showToast('Wi-Fi connection authorized!');
    document.getElementById('captiveWarning').style.display = 'none';
  }).catch(() => {});
}

function copyPortalUrl() {
  const url = window.location.origin;
  navigator.clipboard.writeText(url).then(() => {
    showToast('URL copied to clipboard: ' + url);
  }).catch(() => {
    prompt('Copy URL:', url);
  });
}

/* ------------------------------------------------------------- */
/* INITIALIZATION                                                */
/* ------------------------------------------------------------- */
document.addEventListener('DOMContentLoaded', () => {
  initTheme();
  document.getElementById('nicknameDisplay').textContent = getSavedNickname();
  document.getElementById('portalUrlDisplay').textContent = window.location.origin;

  loadHostInfo();
  loadDiscoveredDevices();
  loadAvailableFiles();
  loadHistory();
  initSSE();

  // Send initial heartbeat with nickname
  fetch('/api/heartbeat?clientId=' + getClientId() + '&name=' + encodeURIComponent(getSavedNickname()), { method: 'POST' }).catch(() => {});

  // Recurring heartbeat
  setInterval(() => {
    fetch('/api/heartbeat?clientId=' + getClientId() + '&name=' + encodeURIComponent(getSavedNickname()), { method: 'POST' }).catch(() => {});
  }, 8000);
});
</script>

</body>
</html>";
    }
}
