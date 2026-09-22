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
<meta name=""theme-color"" content=""#07090E"">
<title>We Share | Web Portal</title>

<style>
:root {
  --bg: #07090E;
  --bg-subtle: #0D101A;
  --panel: rgba(16, 19, 31, 0.85);
  --panel-solid: #111422;
  --card: rgba(22, 26, 42, 0.75);
  --card-hover: rgba(28, 33, 54, 0.9);
  --card-solid: #171B2C;
  --border: rgba(255, 255, 255, 0.12);
  --border-subtle: rgba(255, 255, 255, 0.08);
  --primary: #4F46E5;
  --primary-light: #6366F1;
  --primary-gradient: linear-gradient(135deg, #4F46E5 0%, #6366F1 100%);
  --primary-glow: rgba(0, 0, 0, 0.2);
  --cyan: #06B6D4;
  --emerald: #10B981;
  --amber: #F59E0B;
  --rose: #F43F5E;
  --text: #F8FAFC;
  --text-dim: #94A3B8;
  --text-muted: #64748B;
  --input-bg: rgba(15, 18, 29, 0.85);
  --backdrop-blur: blur(20px);
  --radius-sm: 8px;
  --radius-md: 14px;
  --radius-lg: 20px;
  --radius-full: 9999px;
  --shadow-glow: 0 2px 10px rgba(0, 0, 0, 0.25);
}

html[data-theme=""light""] {
  --bg: #F8FAFC;
  --bg-subtle: #EDF2F7;
  --panel: rgba(255, 255, 255, 0.9);
  --panel-solid: #FFFFFF;
  --card: rgba(241, 245, 249, 0.85);
  --card-hover: rgba(226, 232, 240, 0.95);
  --card-solid: #F1F5F9;
  --border: rgba(79, 70, 229, 0.2);
  --border-subtle: rgba(0, 0, 0, 0.08);
  --primary: #4F46E5;
  --primary-light: #6366F1;
  --primary-glow: rgba(0, 0, 0, 0.1);
  --text: #0F172A;
  --text-dim: #475569;
  --text-muted: #94A3B8;
  --input-bg: rgba(241, 245, 249, 0.95);
  --shadow-glow: 0 2px 10px rgba(0, 0, 0, 0.08);
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
  font-family: 'Segoe UI', system-ui, -apple-system, Roboto, BlinkMacSystemFont, 'Helvetica Neue', Arial, sans-serif;
  min-height: 100vh;
  padding-bottom: 90px;
  overflow-x: hidden;
  position: relative;
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
  gap: 18px;
}
@media (max-width: 640px) {
  .app-wrapper {
    padding: 12px 14px;
    gap: 14px;
  }
}

/* TOP APP BAR */
.app-header {
  background: var(--panel);
  backdrop-filter: var(--backdrop-blur);
  -webkit-backdrop-filter: var(--backdrop-blur);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  padding: 12px 18px;
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
  width: 40px;
  height: 40px;
  border-radius: 12px;
  background: var(--card);
  border: 1px solid var(--border);
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
}

.brand-logo-disc img {
  width: 28px;
  height: 28px;
  object-fit: contain;
}

.brand-title-wrap {
  display: flex;
  flex-direction: column;
}

.brand-title {
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
  font-weight: 700;
}

.status-dot {
  width: 7px;
  height: 7px;
  border-radius: 50%;
  background: var(--emerald);
}


  50% { transform: scale(1.25); opacity: 1; }
  100% { transform: scale(0.95); opacity: 0.8; }
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 8px;
}

.nickname-pill {
  display: flex;
  align-items: center;
  gap: 6px;
  background: var(--card);
  border: 1px solid var(--border);
  border-radius: 20px;
  padding: 6px 12px;
  font-size: 12px;
  font-weight: 700;
  color: var(--text);
  cursor: pointer;
  transition: all 0.2s ease;
}
.nickname-pill:hover {
  background: var(--card-hover);
  border-color: var(--primary);
}

.header-btn {
  background: var(--card);
  border: 1px solid var(--border);
  color: var(--text-dim);
  width: 36px;
  height: 36px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s ease;
}
.header-btn:hover {
  background: var(--card-hover);
  color: var(--text);
}
.header-btn svg {
  width: 17px;
  height: 17px;
}


  100% { transform: translateX(-100%); }
}

/* NAVIGATION TAB BAR */
.tab-bar {
  display: flex;
  align-items: center;
  background: var(--panel);
  backdrop-filter: var(--backdrop-blur);
  -webkit-backdrop-filter: var(--backdrop-blur);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  padding: 6px;
  gap: 6px;
}

.tab-btn {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 11px 14px;
  border-radius: var(--radius-md);
  background: transparent;
  border: none;
  color: var(--text-dim);
  font-size: 13px;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s ease;
  position: relative;
}

.tab-btn:hover {
  color: var(--text);
  background: rgba(255, 255, 255, 0.04);
}

.tab-btn.active {
  background: var(--primary-gradient);
  color: #FFFFFF;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.25);
}

.tab-btn svg {
  width: 17px;
  height: 17px;
}

.tab-badge {
  background: var(--rose);
  color: #FFFFFF;
  font-size: 10px;
  font-weight: 800;
  padding: 2px 6px;
  border-radius: 10px;
  margin-left: 2px;
}

/* TAB VIEWS */
.tab-view {
  display: none;
  flex-direction: column;
  gap: 16px;
  animation: fadeIn 0.2s ease forwards;
}

.tab-view.active {
  display: flex;
}

@keyframes fadeIn {
  from { opacity: 0; transform: translateY(5px); }
  to { opacity: 1; transform: translateY(0); }
}

/* CARDS */
.card-section {
  background: var(--panel);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  padding: 20px;
  display: flex;
  flex-direction: column;
  gap: 16px;
  box-shadow: var(--shadow-glow);
}

.section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.section-title {
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 14px;
  font-weight: 800;
  color: var(--text);
  letter-spacing: 0.3px;
}

/* DROPZONE */
.dropzone-card {
  background: var(--card);
  border: 2px dashed rgba(79, 70, 229, 0.35);
  border-radius: var(--radius-lg);
  padding: 34px 20px;
  text-align: center;
  cursor: pointer;
  transition: all 0.25s ease;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 14px;
}

.dropzone-card:hover {
  border-color: var(--primary-light);
  background: var(--card-hover);
  transform: translateY(-2px);
}

.dz-icon-circle {
  width: 72px;
  height: 72px;
  border-radius: 50%;
  background: rgba(79, 70, 229, 0.15);
  border: 1px solid rgba(79, 70, 229, 0.3);
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--primary-light);
}

.dz-icon-circle svg {
  width: 32px;
  height: 32px;
}

.dz-title {
  font-size: 16px;
  font-weight: 800;
  color: var(--text);
}

.dz-sub {
  font-size: 12px;
  color: var(--text-muted);
  max-width: 360px;
  line-height: 1.5;
}

.dz-btn-group {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  justify-content: center;
  margin-top: 6px;
}

.dz-action-btn {
  background: rgba(255, 255, 255, 0.06);
  border: 1px solid var(--border);
  border-radius: var(--radius-md);
  padding: 9px 16px;
  color: var(--text);
  font-size: 12px;
  font-weight: 700;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  transition: all 0.2s ease;
}

.dz-action-btn:hover {
  background: var(--primary);
  border-color: var(--primary);
  color: #FFFFFF;
}

.dz-action-btn svg {
  width: 15px;
  height: 15px;
}

/* STAGING TRAY */
.staging-card {
  background: var(--card-solid);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  padding: 18px;
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.staging-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.staging-title {
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 14px;
  font-weight: 800;
  color: var(--text);
}

.staging-badge {
  background: rgba(79, 70, 229, 0.15);
  border: 1px solid rgba(79, 70, 229, 0.3);
  color: var(--primary-light);
  font-size: 11px;
  font-weight: 800;
  padding: 3px 8px;
  border-radius: 12px;
}

.staging-clear-btn {
  background: transparent;
  border: none;
  color: var(--rose);
  font-size: 11px;
  font-weight: 700;
  cursor: pointer;
}

.staging-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  max-height: 220px;
  overflow-y: auto;
  padding-right: 4px;
}

.staging-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 10px 14px;
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-md);
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
  background: rgba(79, 70, 229, 0.12);
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--primary-light);
  flex-shrink: 0;
}
.si-icon svg { width: 16px; height: 16px; }

.si-info {
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.si-name {
  font-size: 12px;
  font-weight: 700;
  color: var(--text);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.si-size {
  font-size: 10px;
  color: var(--text-dim);
}

.si-remove-btn {
  background: transparent;
  border: none;
  color: var(--text-muted);
  cursor: pointer;
  padding: 4px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 6px;
  transition: all 0.15s ease;
}
.si-remove-btn:hover {
  color: var(--rose);
  background: rgba(244, 63, 94, 0.1);
}
.si-remove-btn svg { width: 14px; height: 14px; }

.staging-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 14px;
  padding-top: 6px;
  border-top: 1px solid var(--border-subtle);
}

.staging-summary-row {
  display: flex;
  flex-direction: column;
}

.staging-summary-lbl {
  font-size: 10px;
  color: var(--text-muted);
  text-transform: uppercase;
  font-weight: 700;
}

.staging-summary-val {
  font-size: 14px;
  font-weight: 800;
  color: var(--text);
}

.send-all-btn {
  background: var(--primary-gradient);
  border: none;
  border-radius: var(--radius-md);
  padding: 12px 24px;
  color: #FFFFFF;
  font-size: 13px;
  font-weight: 800;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.25);
  transition: all 0.2s ease;
}
.send-all-btn:hover {
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
}
.send-all-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
  transform: none;
}
.send-all-btn svg { width: 16px; height: 16px; }

/* ------------------------------------------------------------- */
/* RADAR STAGE (SENDER DISCOVERY)                                */
/* ------------------------------------------------------------- */
.radar-stage-card {
  background: var(--panel);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  padding: 24px 20px;
  text-align: center;
  box-shadow: var(--shadow-glow);
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 16px;
}

.radar-top-bar {
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.radar-back-btn {
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid var(--border);
  border-radius: var(--radius-sm);
  padding: 7px 12px;
  color: var(--text);
  font-size: 11px;
  font-weight: 700;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  cursor: pointer;
  transition: all 0.2s ease;
}
.radar-back-btn:hover {
  background: rgba(255, 255, 255, 0.1);
}
.radar-back-btn svg { width: 13px; height: 13px; }

.radar-batch-pill {
  background: rgba(79, 70, 229, 0.12);
  border: 1px solid rgba(79, 70, 229, 0.25);
  padding: 6px 12px;
  border-radius: 20px;
  font-size: 11px;
  font-weight: 700;
  color: var(--primary-light);
}

.web-radar-box {
  position: relative;
  width: 280px;
  height: 280px;
  margin: 10px auto;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: visible;
}

.radar-circle {
  position: absolute;
  border-radius: 50%;
  border: 1px solid rgba(255, 255, 255, 0.08);
  pointer-events: none;
}
.radar-circle.c1 { width: 80px; height: 80px; }
.radar-circle.c2 { width: 150px; height: 150px; border-style: dashed; border-color: rgba(255, 255, 255, 0.06); }
.radar-circle.c3 { width: 220px; height: 220px; }
.radar-circle.c4 { width: 280px; height: 280px; border-color: rgba(255, 255, 255, 0.05); }

.radar-sweep-beam {
  position: absolute;
  width: 100%;
  height: 100%;
  border-radius: 50%;
  background: conic-gradient(from 0deg at 50% 50%, rgba(99, 102, 241, 0.14) 0deg, transparent 55deg, transparent 360deg);
  animation: radarRotate 3s linear infinite;
  pointer-events: none;
}

@keyframes radarRotate {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

.radar-wave {
  position: absolute;
  width: 100%;
  height: 100%;
  border-radius: 50%;
  border: 1px solid rgba(99, 102, 241, 0.25);
  animation: radarPulseWave 2.6s ease-out infinite;
  pointer-events: none;
}

@keyframes radarPulseWave {
  0% { transform: scale(0.25); opacity: 0.6; }
  80% { transform: scale(1.0); opacity: 0; }
  100% { transform: scale(1.0); opacity: 0; }
}

.radar-center-sender {
  position: relative;
  z-index: 3;
  width: 52px;
  height: 52px;
  border-radius: 50%;
  background: var(--card-solid);
  border: 1.5px solid var(--border);
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--text);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.3);
}
.radar-center-sender svg { width: 24px; height: 24px; }

/* DYNAMIC RECEIVER RADAR NODES */
.web-radar-node {
  position: absolute;
  z-index: 5;
  display: flex;
  flex-direction: column;
  align-items: center;
  cursor: pointer;
  transition: transform 0.2s ease;
  transform: translate(-50%, -50%);
}

.web-radar-node:hover {
  transform: translate(-50%, -50%) scale(1.08);
}

.web-radar-node-disc {
  width: 44px;
  height: 44px;
  border-radius: 50%;
  background: #181B26;
  border: 1.5px solid #10B981;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.3);
  color: #10B981;
}
.web-radar-node-disc svg { width: 22px; height: 22px; }

.web-radar-node-label {
  font-size: 11px;
  font-weight: 800;
  color: #FFFFFF;
  background: rgba(15, 23, 42, 0.85);
  border: 1px solid var(--border);
  padding: 2px 8px;
  border-radius: 10px;
  margin-top: 4px;
  white-space: nowrap;
  max-width: 90px;
  overflow: hidden;
  text-overflow: ellipsis;
}

.radar-empty-hint {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6px;
  padding: 12px;
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-md);
  max-width: 420px;
}

.radar-empty-title {
  font-size: 12px;
  font-weight: 700;
  color: var(--text);
  display: flex;
  align-items: center;
  gap: 6px;
}

.radar-empty-text {
  font-size: 11px;
  color: var(--text-muted);
  line-height: 1.5;
}

/* ------------------------------------------------------------- */
/* RECEIVE MODE VIEW                                             */
/* ------------------------------------------------------------- */
.receive-hero-card {
  background: var(--panel);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  padding: 36px 20px;
  text-align: center;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 16px;
  box-shadow: var(--shadow-glow);
}

.receive-beacon-wrap {
  position: relative;
  width: 170px;
  height: 170px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin: 8px 0;
}

.beacon-ring-1 {
  position: absolute;
  inset: 0;
  border-radius: 50%;
  border: 1px solid rgba(16, 185, 129, 0.2);
  animation: beaconPulse 2.4s cubic-bezier(0.2, 0.6, 0.4, 1) infinite;
}

.beacon-ring-2 {
  position: absolute;
  inset: 18px;
  border-radius: 50%;
  border: 1px solid rgba(16, 185, 129, 0.15);
  animation: beaconPulse 2.4s cubic-bezier(0.2, 0.6, 0.4, 1) infinite 0.7s;
}

@keyframes beaconPulse {
  0% { transform: scale(0.6); opacity: 0.6; }
  80% { transform: scale(1.1); opacity: 0; }
  100% { transform: scale(1.1); opacity: 0; }
}

.beacon-center-disc {
  width: 86px;
  height: 86px;
  border-radius: 50%;
  background: var(--card-solid);
  border: 2px solid #10B981;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #10B981;
  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.3);
  z-index: 2;
}
.beacon-center-disc svg { width: 40px; height: 40px; }

.receive-status-pill {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 6px 18px;
  background: rgba(16, 185, 129, 0.12);
  border: 1px solid rgba(16, 185, 129, 0.3);
  border-radius: 20px;
  color: var(--emerald);
  font-size: 12px;
  font-weight: 800;
  letter-spacing: 0.6px;
}

.receive-device-title {
  font-size: 20px;
  font-weight: 800;
  color: var(--text);
}

.receive-device-sub {
  font-size: 12px;
  color: var(--text-muted);
}

.receive-guide-box {
  width: 100%;
  max-width: 440px;
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-md);
  padding: 14px 16px;
  text-align: left;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.rgb-row {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  font-size: 11px;
  color: var(--text-dim);
  line-height: 1.5;
}

.rgb-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: var(--cyan);
  margin-top: 5px;
  flex-shrink: 0;
}

/* ------------------------------------------------------------- */
/* DEDICATED FULL ACTIVE TRANSFER VIEW                           */
/* ------------------------------------------------------------- */
.transfer-view-card {
  background: var(--panel);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  padding: 24px 20px;
  display: flex;
  flex-direction: column;
  gap: 18px;
  box-shadow: var(--shadow-glow);
}

.tv-top-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.tv-peer-info {
  display: flex;
  align-items: center;
  gap: 12px;
}

.tv-peer-avatar {
  width: 44px;
  height: 44px;
  border-radius: 12px;
  background: rgba(79, 70, 229, 0.15);
  border: 1px solid var(--border);
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--primary-light);
}
.tv-peer-avatar svg { width: 22px; height: 22px; }

.tv-peer-text {
  display: flex;
  flex-direction: column;
}

.tv-role-badge {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: 10px;
  font-weight: 800;
  letter-spacing: 0.5px;
  color: var(--primary-light);
}

.tv-peer-name {
  font-size: 16px;
  font-weight: 800;
  color: var(--text);
}

.tv-abort-btn {
  background: rgba(244, 63, 94, 0.12);
  border: 1px solid rgba(244, 63, 94, 0.3);
  color: var(--rose);
  padding: 8px 14px;
  border-radius: var(--radius-sm);
  font-size: 11px;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s ease;
}
.tv-abort-btn:hover {
  background: var(--rose);
  color: #FFFFFF;
}

.tv-progress-box {
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-md);
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.tv-pb-top {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
}

.tv-current-filename {
  font-size: 14px;
  font-weight: 800;
  color: var(--text);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 80%;
}

.tv-pct-display {
  font-size: 26px;
  font-weight: 900;
  color: var(--primary-light);
}

.tv-bar-track {
  width: 100%;
  height: 8px;
  background: rgba(255, 255, 255, 0.08);
  border-radius: 4px;
  overflow: hidden;
}

.tv-bar-fill {
  width: 0%;
  height: 100%;
  background: var(--primary-gradient);
  border-radius: 4px;
  transition: width 0.15s linear;
}

.tv-stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(110px, 1fr));
  gap: 8px;
}

.tv-stat-card {
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-sm);
  padding: 8px 12px;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.tv-stat-lbl {
  font-size: 10px;
  font-weight: 700;
  color: var(--text-muted);
  text-transform: uppercase;
}

.tv-stat-val {
  font-size: 13px;
  font-weight: 800;
  color: var(--text);
}

.tv-queue-container {
  display: flex;
  flex-direction: column;
  gap: 6px;
  max-height: 220px;
  overflow-y: auto;
  padding-right: 4px;
}

.transfer-queue-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 10px 14px;
  border-radius: 8px;
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid var(--border-subtle);
}

/* ------------------------------------------------------------- */
/* DOWNLOADS & ACTIVITY LISTS                                    */
/* ------------------------------------------------------------- */
.file-cards-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.file-card-item {
  background: var(--card);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-md);
  padding: 12px 16px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.fci-left {
  display: flex;
  align-items: center;
  gap: 12px;
  overflow: hidden;
  flex: 1;
}

.fci-icon {
  width: 36px;
  height: 36px;
  border-radius: 10px;
  background: rgba(79, 70, 229, 0.12);
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--primary-light);
  flex-shrink: 0;
}
.fci-icon svg { width: 18px; height: 18px; }

.fci-name {
  font-size: 13px;
  font-weight: 700;
  color: var(--text);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.fci-meta {
  font-size: 11px;
  color: var(--text-dim);
}

.fci-dl-btn {
  background: var(--primary-gradient);
  border: none;
  color: #FFFFFF;
  padding: 8px 14px;
  border-radius: var(--radius-sm);
  font-size: 11px;
  font-weight: 700;
  text-decoration: none;
  display: inline-flex;
  align-items: center;
  gap: 6px;
}
.fci-dl-btn svg { width: 13px; height: 13px; }

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
  width: 30px;
  height: 30px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}
.hi-dir-badge.received { background: rgba(16, 185, 129, 0.15); color: var(--emerald); }
.hi-dir-badge.sent { background: rgba(6, 182, 212, 0.15); color: var(--cyan); }
.hi-dir-badge svg { width: 14px; height: 14px; }

.hi-name {
  font-size: 12px;
  font-weight: 700;
  color: var(--text);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.hi-sub {
  font-size: 10px;
  color: var(--text-muted);
}

.hi-status {
  font-size: 10px;
  font-weight: 800;
  text-transform: uppercase;
}
.hi-status.completed { color: var(--emerald); }
.hi-status.failed { color: var(--rose); }

.empty-state-box {
  text-align: center;
  padding: 36px 16px;
  color: var(--text-muted);
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
}
.empty-state-box svg {
  width: 44px;
  height: 44px;
  color: var(--border);
}
.empty-state-title {
  font-weight: 800;
  font-size: 14px;
  color: var(--text-dim);
}
.empty-state-text {
  font-size: 12px;
  line-height: 1.5;
  max-width: 300px;
}

/* SHARE TAB */
.qr-container {
  display: flex;
  align-items: center;
  gap: 20px;
  padding: 8px 0;
}
@media (max-width: 480px) {
  .qr-container { flex-direction: column; text-align: center; }
}

.qr-box {
  background: #FFFFFF;
  border-radius: var(--radius-md);
  padding: 10px;
  width: 140px;
  height: 140px;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
}
.qr-box img { width: 100%; height: 100%; object-fit: contain; }

.url-copy-box {
  display: flex;
  align-items: center;
  gap: 8px;
  background: var(--input-bg);
  border: 1px solid var(--border-subtle);
  border-radius: 8px;
  padding: 8px 12px;
}

.url-text {
  font-size: 12px;
  color: var(--cyan);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  flex: 1;
}

.copy-btn {
  background: rgba(79, 70, 229, 0.2);
  border: 1px solid rgba(79, 70, 229, 0.35);
  color: var(--primary-light);
  padding: 5px 12px;
  border-radius: 6px;
  font-size: 11px;
  font-weight: 700;
  cursor: pointer;
}
.copy-btn:hover { background: var(--primary); color: #FFFFFF; }

/* MODALS */
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
  padding: 24px 22px;
  width: 100%;
  max-width: 400px;
  text-align: center;
  box-shadow: 0 20px 50px rgba(0, 0, 0, 0.7);
  transform: scale(0.95);
  transition: transform 0.2s ease;
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.modal-overlay.active .modal-sheet {
  transform: scale(1);
}

.modal-icon-disc {
  width: 56px;
  height: 56px;
  border-radius: 50%;
  background: rgba(79, 70, 229, 0.15);
  color: var(--primary-light);
  display: inline-flex;
  align-items: center;
  justify-content: center;
  margin: 0 auto;
}
.modal-icon-disc svg { width: 28px; height: 28px; }

.modal-title {
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
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.25);
}

.batch-chk-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 10px;
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid var(--border-subtle);
  border-radius: 8px;
  text-align: left;
}
.batch-chk-item input[type=""checkbox""] {
  accent-color: var(--primary);
  width: 16px;
  height: 16px;
  cursor: pointer;
}
.batch-chk-name {
  font-size: 12px;
  font-weight: 700;
  color: var(--text);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

/* TOAST */
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
.toast-pill svg { width: 16px; height: 16px; }

input[type=""file""] { display: none; }

/* HERO ACTION BUTTONS (MATCHING APP HOME VIEW) */
.hero-action-grid {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 32px;
  margin: 18px 0 24px 0;
  flex-wrap: wrap;
}
.hero-action-col {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 10px;
  cursor: pointer;
  background: transparent;
  border: none;
  padding: 0;
  outline: none;
  transition: transform 0.2s ease;
}
.hero-action-col:hover {
  transform: translateY(-2px);
}
.hero-circle-btn {
  width: 140px;
  height: 140px;
  border-radius: 50%;
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: transform 0.28s cubic-bezier(0.34, 1.56, 0.64, 1), box-shadow 0.28s ease;
  cursor: pointer;
}
@media (min-width: 600px) {
  .hero-circle-btn {
    width: 154px;
    height: 154px;
  }
}
.hero-circle-btn.send-disc {
  background: var(--card-solid);
  border: 1.5px solid rgba(124, 58, 237, 0.45);
  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.3);
}
.hero-circle-btn.send-disc:hover, .hero-circle-btn.send-disc:active {
  background: var(--card-hover);
  border-color: #7C3AED;
  transform: scale(1.04);
  box-shadow: 0 6px 18px rgba(0, 0, 0, 0.4);
}
.hero-circle-btn.receive-disc {
  background: var(--card-solid);
  border: 1.5px solid rgba(16, 185, 129, 0.45);
  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.3);
}
.hero-circle-btn.receive-disc:hover, .hero-circle-btn.receive-disc:active {
  background: var(--card-hover);
  border-color: #10B981;
  transform: scale(1.04);
  box-shadow: 0 6px 18px rgba(0, 0, 0, 0.4);
}
.hero-circle-img {
  width: 78%;
  height: 78%;
  object-fit: contain;
  pointer-events: none;
  filter: drop-shadow(0 4px 14px rgba(0, 0, 0, 0.45));
}
.hero-title {
  font-size: 15px;
  font-weight: 800;
  letter-spacing: 1.5px;
  color: #FFFFFF;
}
.hero-subtitle {
  font-size: 11px;
  font-weight: 500;
  color: var(--text-muted);
}
.radar-center-img {
  width: 32px;
  height: 32px;
  object-fit: contain;
  pointer-events: none;
}
.beacon-core-img {
  width: 48px;
  height: 48px;
  object-fit: contain;
  pointer-events: none;
}




/* SMART RADAR ASSISTANT CARD */
.radar-smart-assistant {
  margin-top: 14px;
  padding: 12px 14px;
  border-radius: 12px;
  background: rgba(245, 158, 11, 0.1);
  border: 1px solid rgba(245, 158, 11, 0.3);
  display: none;
  flex-direction: column;
  gap: 8px;
  text-align: left;
}
.rsa-title {
  font-size: 11px;
  font-weight: 800;
  color: var(--amber);
  letter-spacing: 0.6px;
  display: flex;
  align-items: center;
  gap: 6px;
}
.rsa-text {
  font-size: 11px;
  color: var(--text-dim);
  line-height: 1.4;
}

/* SUGGESTION CHIPS */
.name-chips-row {
  display: flex;
  gap: 6px;
  flex-wrap: wrap;
  margin-top: 8px;
  margin-bottom: 14px;
}
.name-chip {
  padding: 5px 10px;
  border-radius: 8px;
  background: rgba(255, 255, 255, 0.08);
  border: 1px solid var(--border-subtle);
  color: var(--text-dim);
  font-size: 11px;
  font-weight: 600;
  cursor: pointer;
}
.name-chip:hover {
  background: rgba(79, 70, 229, 0.2);
  color: #FFFFFF;
  border-color: var(--primary);
}

</style>
</head>
<body>




<div class=""app-wrapper"">

  <!-- TOP HEADER -->
  <header class=""app-header"">
    <div class=""brand-section"">
      <div class=""brand-logo-disc"">
        <img src=""/api/logo"" alt=""WeShare"" onerror=""this.style.display='none';"">
      </div>
      <div class=""brand-title-wrap"">
        <span class=""brand-title"">WE SHARE</span>
        <span class=""brand-status-badge"">
          <span class=""status-dot""></span>
          <span id=""hostStatusText"">ONLINE</span>
        </span>
      </div>
    </div>

    <div class=""header-actions"">
      <div class=""nickname-pill"" onclick=""promptEditNickname()"" title=""Click to rename device"">
        <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round"" style=""width:13px; height:13px; color:var(--primary-light);""><rect x=""5"" y=""2"" width=""14"" height=""20"" rx=""2"" ry=""2""/><line x1=""12"" y1=""18"" x2=""12.01"" y2=""18""/></svg>
        <span id=""nicknameDisplay"">Device</span>
        <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round"" style=""width:11px; height:11px; color:var(--text-muted); margin-left:2px;""><path d=""M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7""/><path d=""M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z""/></svg>
      </div>
      <button class=""header-btn"" id=""themeToggleBtn"" onclick=""toggleTheme()"" title=""Toggle Theme"">
        <svg id=""themeIcon"" xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2.2"" stroke-linecap=""round"" stroke-linejoin=""round""><circle cx=""12"" cy=""12"" r=""4""/><path d=""M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M6.34 17.66l-1.41 1.41M19.07 4.93l-1.41 1.41""/></svg>
      </button>
    </div>
  </header>

  

  <!-- NAVIGATION TAB BAR -->
  <nav class=""tab-bar"">
    <button class=""tab-btn active"" id=""tabBtnSend"" onclick=""switchTab('send')"">
      <img src=""/assets/send.png"" style=""width:16px; height:16px; object-fit:contain;"" onerror=""this.style.display='none'; this.nextElementSibling.style.display='block';"" alt=""Send"">
      <svg style=""display:none;"" xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M22 2L11 13""/><path d=""M22 2l-7 20-4-9-9-4 20-7z""/></svg>
      <span>Send</span>
      <span class=""tab-badge"" id=""stagingBadge"" style=""display:none;"">0</span>
    </button>
    <button class=""tab-btn"" id=""tabBtnReceive"" onclick=""switchTab('receive')"">
      <img src=""/assets/receive.png"" style=""width:16px; height:16px; object-fit:contain;"" onerror=""this.style.display='none'; this.nextElementSibling.style.display='block';"" alt=""Receive"">
      <svg style=""display:none;"" xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4""/><polyline points=""7 10 12 15 17 10""/><line x1=""12"" y1=""15"" x2=""12"" y2=""3""/></svg>
      <span>Receive</span>
    </button>
    <button class=""tab-btn"" id=""tabBtnDownloads"" onclick=""switchTab('downloads')"">
      <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z""/></svg>
      <span>Library</span>
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
  <!-- 1. SEND TAB VIEW (SHAREit STAGED FLOW)                     -->
  <!-- ========================================================= -->
  <div class=""tab-view active"" id=""viewSend"">

    <!-- HERO BIG CIRCULAR ACTION BUTTONS (MATCHING DESKTOP APP) -->
    <div class=""hero-action-grid"">
      <!-- BIG CIRCULAR SEND BUTTON -->
      <button type=""button"" class=""hero-action-col"" onclick=""startSenderRadar()"" title=""Search for receivers and send files"">
        <div class=""hero-circle-btn send-disc"">
          <img src=""/assets/send.png"" class=""hero-circle-img"" onerror=""this.style.display='none'; this.nextElementSibling.style.display='block';"" alt=""Send"">
          <svg style=""display:none; width:60px; height:60px; color:#FFFFFF;"" xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M22 2L11 13""/><path d=""M22 2l-7 20-4-9-9-4 20-7z""/></svg>
        </div>
        <span class=""hero-title"">SEND</span>
        <span class=""hero-subtitle"">Tap to scan &amp; send files</span>
      </button>

      <!-- BIG CIRCULAR RECEIVE BUTTON -->
      <button type=""button"" class=""hero-action-col"" onclick=""switchTab('receive')"" title=""Enter receive mode to accept incoming files"">
        <div class=""hero-circle-btn receive-disc"">
          <img src=""/assets/receive.png"" class=""hero-circle-img"" onerror=""this.style.display='none'; this.nextElementSibling.style.display='block';"" alt=""Receive"">
          <svg style=""display:none; width:60px; height:60px; color:#FFFFFF;"" xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4""/><polyline points=""7 10 12 15 17 10""/><line x1=""12"" y1=""15"" x2=""12"" y2=""3""/></svg>
        </div>
        <span class=""hero-title"">RECEIVE</span>
        <span class=""hero-subtitle"">Ready to accept transfers</span>
      </button>
    </div>


    <!-- STAGE 1: RADAR RECEIVER DISCOVERY (SHOWN FIRST) -->
    <div id=""sendRadarStage"" style=""display:block;"">
      <div class=""radar-stage-card"">
        <div class=""radar-top-bar"">
          <div class=""receive-status-pill"" style=""margin:0;"">
            <span class=""status-dot""></span>
            <span>RADAR SCANNING</span>
          </div>

        </div>

        <div style=""display:flex; flex-direction:column; align-items:center; gap:4px; margin-top:8px;"">
          <div style=""font-size:17px; font-weight:800; color:var(--text);"">Nearby Receivers</div>
          <div style=""font-size:12px; color:var(--text-muted); text-align:center;"">Tap an active receiver node to connect and send files</div>
        </div>

        <!-- Animated Sweeping Radar Circle -->
        <div class=""web-radar-box"" id=""webRadarBox"">
          <div class=""radar-circle c1""></div>
          <div class=""radar-circle c2""></div>
          <div class=""radar-circle c3""></div>
          <div class=""radar-circle c4""></div>
          <div class=""radar-sweep-beam""></div>
          <div class=""radar-wave""></div>

          <!-- Center Node: You (Sender) with find.png -->
          <div class=""radar-center-sender"" title=""You (Sender)"">
            <img src=""/assets/find.png"" class=""radar-center-img"" onerror=""this.style.display='none'; this.nextElementSibling.style.display='block';"" alt=""Search"">
            <svg style=""display:none;"" xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><rect x=""5"" y=""2"" width=""14"" height=""20"" rx=""2"" ry=""2""/><line x1=""12"" y1=""18"" x2=""12.01"" y2=""18""/></svg>
          </div>

          <!-- Dynamic Receiver Nodes Container -->
          <div id=""radarNodesContainer""></div>
        </div>

        <!-- Available Receivers List -->
        <div id=""radarReceiversList"" style=""width:100%; max-width:380px; display:flex; flex-direction:column; gap:8px; margin-top:8px;""></div>

        <!-- Empty Radar Status -->
        <div class=""radar-empty-hint"" id=""radarEmptyHint"">
          <div class=""radar-empty-title"">
            <span class=""status-dot""></span>
            <span>Scanning for Nearby Receivers...</span>
          </div>
          <div class=""radar-empty-text"">
            Make sure the other device is in <b>Receive Mode</b> and connected to the same Wi-Fi network.
          </div>
        </div>

        <!-- SMART RADAR TROUBLESHOOTING ASSISTANT -->
        <div class=""radar-smart-assistant"" id=""radarSmartAssistant"">
          <div class=""rsa-title"">
            <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round"" width=""14"" height=""14""><circle cx=""12"" cy=""12"" r=""10""/><line x1=""12"" y1=""8"" x2=""12"" y2=""12""/><line x1=""12"" y1=""16"" x2=""12.01"" y2=""16""/></svg>
            <span>Can't see each other?</span>
          </div>
          <div class=""rsa-text"">
            Some college, office, or public Wi-Fi networks block devices from seeing each other.<br>
            On your PC, click <b>Direct Hotspot</b> to connect directly without a router!
          </div>
        </div>
      </div>
    </div>

    <!-- STAGE 2: FILE SELECTION & STAGING (SHOWN AFTER CONNECTING) -->
    <div id=""sendSelectionStage"" style=""display:none;"">

      <!-- CONNECTED TARGET BANNER -->
      <div id=""connectedTargetBanner"" style=""margin-bottom:14px; padding:12px 16px; border-radius:14px; background:linear-gradient(135deg, rgba(79,70,229,0.15), rgba(16,185,129,0.12)); border:1.5px solid rgba(16,185,129,0.3); display:flex; justify-content:space-between; align-items:center;"">
        <div style=""display:flex; align-items:center; gap:10px;"">
          <div style=""width:10px; height:10px; border-radius:5px; background:var(--emerald); box-shadow:0 0 10px var(--emerald);""></div>
          <div style=""display:flex; flex-direction:column;"">
            <div style=""font-size:10px; font-weight:800; color:var(--emerald); letter-spacing:0.8px;"">CONNECTED RECIPIENT</div>
            <div id=""connectedTargetName"" style=""font-size:14px; font-weight:800; color:var(--text);"">This Computer</div>
          </div>
        </div>
        <button type=""button"" class=""staging-clear-btn"" style=""padding:6px 12px; font-size:11px; background:rgba(255,255,255,0.08); border-radius:8px; border:1px solid var(--border);"" onclick=""startSenderRadar()"">
          Change (Radar)
        </button>
      </div>
      <!-- Hidden Multi-File, Folder and Camera Inputs -->
      <input type=""file"" id=""multiFileInput"" multiple onchange=""onFilesSelected(this.files)"">
      <input type=""file"" id=""folderInput"" webkitdirectory directory multiple onchange=""onFolderSelected(this.files)"">
      <input type=""file"" id=""cameraInput"" accept=""image/*,video/*"" capture=""environment"" onchange=""onFilesSelected(this.files)"">

      <!-- Interactive Dropzone -->
      <div class=""dropzone-card"" id=""dropZone"" onclick=""document.getElementById('multiFileInput').click()"">
        <div class=""dz-icon-circle"">
          <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2.2"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4""/><polyline points=""17 8 12 3 7 8""/><line x1=""12"" y1=""3"" x2=""12"" y2=""15""/></svg>
        </div>
        <div class=""dz-title"">Select Files to Send</div>
        <div class=""dz-sub"">Choose photos, 4K videos, documents, or entire folders to send over local high-speed Wi-Fi.</div>
        <div class=""dz-btn-group"" onclick=""event.stopPropagation()"">
          <button class=""dz-action-btn"" onclick=""document.getElementById('multiFileInput').click()"">
            <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z""/><polyline points=""14 2 14 8 20 8""/><line x1=""12"" y1=""18"" x2=""12"" y2=""12""/><line x1=""9"" y1=""15"" x2=""15"" y2=""15""/></svg>
            Browse Files
          </button>
          <button class=""dz-action-btn"" onclick=""document.getElementById('folderInput').click()"">
            <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z""/></svg>
            Add Folder
          </button>
          <button class=""dz-action-btn"" onclick=""document.getElementById('cameraInput').click()"">
            <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M23 19a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4l2-3h6l2 3h4a2 2 0 0 1 2 2z""/><circle cx=""12"" cy=""13"" r=""4""/></svg>
            Camera Roll
          </button>
        </div>
      </div>

      <!-- Staging Tray -->
      <div class=""staging-card"" id=""stagingTray"" style=""display:none; margin-top:16px;"">
        <div class=""staging-header"">
          <div class=""staging-title"">
            <span>Staged for Transfer</span>
            <span class=""staging-badge"" id=""stagingCountBadge"">0 files</span>
          </div>
          <button class=""staging-clear-btn"" onclick=""clearStagingTray()"">Clear All</button>
        </div>

        <div class=""staging-list"" id=""stagingList"">
          <!-- Populated dynamically -->
        </div>

        <div class=""staging-footer"">
          <div class=""staging-summary-row"">
            <span class=""staging-summary-lbl"">Total Volume</span>
            <span class=""staging-summary-val"" id=""stagingTotalSize"">0 MB</span>
          </div>
          <button class=""send-all-btn"" id=""sendAllBtn"" onclick=""sendBatchToConnectedTarget()"">
            <span id=""sendAllBtnText"">Send Files to Recipient</span>
            <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2.5"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M22 2L11 13""/><path d=""M22 2l-7 20-4-9-9-4 20-7z""/></svg>
          </button>
        </div>
      </div>
    </div>

    </div>
  </div>

  <!-- ===+ -->
  <!-- 2. RECEIVE TAB VIEW (RECEIVER MODE BEACON)                 -->
  <!-- ========================================================= -->
  <div class=""tab-view"" id=""viewReceive"">
    <div class=""receive-hero-card"">
      <div class=""receive-status-pill"">
        <span class=""status-dot""></span>
        <span>READY TO RECEIVE</span>
      </div>

      <!-- Concentric Pulsing Receiver Beacon -->
      <div class=""receive-beacon-wrap"">
        <div class=""beacon-ring-1""></div>
        <div class=""beacon-ring-2""></div>
        <div class=""beacon-center-disc"">
          <img src=""/assets/receive.png"" class=""beacon-core-img"" onerror=""this.style.display='none'; this.nextElementSibling.style.display='block';"" alt=""Receive"">
          <svg style=""display:none;"" xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2.2"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4""/><polyline points=""7 10 12 15 17 10""/><line x1=""12"" y1=""15"" x2=""12"" y2=""3""/></svg>
        </div>
      </div>

      <div class=""receive-device-title"" id=""receiveDeviceName"">Mobile Web</div>
      <div class=""receive-device-sub"">Broadcasting presence to This Computer and nearby senders</div>

      <div class=""receive-guide-box"">
        <div class=""rgb-row"">
          <div class=""rgb-dot""></div>
          <div><b>Keep this screen open</b> while waiting for files. Your device is now visible on senders' radar.</div>
        </div>
        <div class=""rgb-row"">
          <div class=""rgb-dot""></div>
          <div>When a sender selects this device, an incoming transfer prompt will appear for you to accept.</div>
        </div>
        <div class=""rgb-row"">
          <div class=""rgb-dot""></div>
          <div>Transfers operate completely offline over your local Wi-Fi or hotspot with zero cloud usage.</div>
        </div>
      </div>

      <button onclick=""promptEditNickname()"" class=""dz-action-btn"" style=""margin-top:6px;"">
        <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2""/><circle cx=""12"" cy=""7"" r=""4""/></svg>
        Edit Device Name
      </button>
    </div>
  </div>

  <!-- ========================================================= -->
  <!-- 3. DOWNLOADS / LIBRARY TAB VIEW                           -->
  <!-- ========================================================= -->
  <div class=""tab-view"" id=""viewDownloads"">
    <div class=""card-section"">
      <div class=""section-header"">
        <div class=""section-title"">
          <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round"" style=""width:16px; height:16px; color:var(--primary-light);""><path d=""M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4""/><polyline points=""7 10 12 15 17 10""/><line x1=""12"" y1=""15"" x2=""12"" y2=""3""/></svg>
          <span>Available Files</span>
        </div>
        <button onclick=""loadAvailableFiles()"" style=""background:none;border:none;color:var(--primary-light);font-size:11px;font-weight:700;cursor:pointer;"">REFRESH</button>
      </div>

      <div class=""file-cards-list"" id=""downloadFilesList"">
        <div class=""empty-state-box"">
          <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""1.8"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M13 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V9z""/><polyline points=""13 2 13 9 20 9""/></svg>
          <div class=""empty-state-title"">No Files Available</div>
          <div class=""empty-state-text"">Files shared from the PC will appear here for instant 1-click download.</div>
        </div>
      </div>
    </div>
  </div>

  <!-- ========================================================= -->
  <!-- 4. ACTIVITY TAB VIEW                                      -->
  <!-- ========================================================= -->
  <div class=""tab-view"" id=""viewActivity"">
    <div class=""card-section"">
      <div class=""section-header"">
        <div class=""section-title"">
          <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round"" style=""width:16px; height:16px; color:var(--primary-light);""><circle cx=""12"" cy=""12"" r=""10""/><polyline points=""12 6 12 12 16 14""/></svg>
          <span>Transfer Activity</span>
        </div>
        <button onclick=""loadHistory()"" style=""background:none;border:none;color:var(--primary-light);font-size:11px;font-weight:700;cursor:pointer;"">REFRESH</button>
      </div>

      <div class=""file-cards-list"" id=""historyItemsList"">
        <div class=""empty-state-box"">
          <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""1.8"" stroke-linecap=""round"" stroke-linejoin=""round""><polyline points=""22 12 18 12 15 21 9 3 6 12 2 12""/></svg>
          <div class=""empty-state-title"">No Transfers Yet</div>
          <div class=""empty-state-text"">Completed uploads and downloads will be recorded here.</div>
        </div>
      </div>
    </div>
  </div>

  <!-- ========================================================= -->
  <!-- 5. SHARE TAB VIEW                                         -->
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
          <img src=""/api/qr"" alt=""QR Code"">
        </div>
        <div style=""display:flex; flex-direction:column; gap:8px;"">
          <div style=""font-size:15px; font-weight:800; color:var(--text);"">Scan from Camera</div>
          <div style=""font-size:12px; color:var(--text-dim); line-height:1.5;"">Scan this QR code with any phone or tablet on the same Wi-Fi network to open We Share instantly.</div>
        </div>
      </div>

      <div class=""url-copy-box"">
        <span class=""url-text"" id=""portalUrlDisplay"">http://...</span>
        <button class=""copy-btn"" onclick=""copyPortalUrl()"">COPY</button>
      </div>
    </div>
  </div>

  <!-- ========================================================= -->
  <!-- 6. DEDICATED FULL ACTIVE TRANSFER VIEW                     -->
  <!-- ========================================================= -->
  <div class=""tab-view"" id=""viewTransfer"">
    <div class=""transfer-view-card"">
      <div class=""tv-top-row"">
        <div class=""tv-peer-info"">
          <div class=""tv-peer-avatar"">
            <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><rect x=""2"" y=""3"" width=""20"" height=""14"" rx=""2"" ry=""2""/><line x1=""8"" y1=""21"" x2=""16"" y2=""21""/><line x1=""12"" y1=""17"" x2=""12"" y2=""21""/></svg>
          </div>
          <div class=""tv-peer-text"">
            <span class=""tv-role-badge"" id=""transferPageRoleBadge"">DIRECT P2P TRANSFER</span>
            <span class=""tv-peer-name"" id=""transferPagePeerName"">This Computer</span>
          </div>
        </div>
        <button class=""tv-abort-btn"" onclick=""cancelActiveUpload()"">Cancel</button>
      </div>

      <!-- Main Progress Box -->
      <div class=""tv-progress-box"">
        <div class=""tv-pb-top"">
          <span class=""tv-current-filename"" id=""transferPageFileTitle"">Preparing files...</span>
          <span class=""tv-pct-display"" id=""transferPagePercentText"">0%</span>
        </div>

        <div class=""tv-bar-track"">
          <div class=""tv-bar-fill"" id=""transferPageBarFill""></div>
        </div>

        <div class=""tv-stats-grid"">
          <div class=""tv-stat-card"">
            <span class=""tv-stat-lbl"">Speed</span>
            <span class=""tv-stat-val"" id=""transferPageSpeedText"">0.0 MB/s</span>
          </div>
          <div class=""tv-stat-card"">
            <span class=""tv-stat-lbl"">Files</span>
            <span class=""tv-stat-val"" id=""transferPageFilesText"">0 / 0</span>
          </div>
          <div class=""tv-stat-card"">
            <span class=""tv-stat-lbl"">Volume</span>
            <span class=""tv-stat-val"" id=""transferPageBytesText"">0 MB / 0 MB</span>
          </div>
          <div class=""tv-stat-card"">
            <span class=""tv-stat-lbl"">ETA</span>
            <span class=""tv-stat-val"" id=""transferPageEtaText"">--:--</span>
          </div>
        </div>
      </div>

      <!-- File Checklist Queue -->
      <div style=""font-size:12px; font-weight:800; color:var(--text); margin-top:4px;"">
        Transfer Queue (<span id=""transferPageQueueCount"">0 files</span>)
      </div>
      <div class=""tv-queue-container"" id=""transferPageQueueList"">
        <!-- Rows populated dynamically -->
      </div>
    </div>
  </div>

</div>

<!-- =========================================================== -->
<!-- MODALS                                                      -->
<!-- =========================================================== -->

<!-- SINGLE FILE OFFER MODAL -->
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

<!-- BATCH CHECKLIST MODAL -->
<div class=""modal-overlay"" id=""batchOfferModal"">
  <div class=""modal-sheet"" style=""max-width:440px; text-align:left;"">
    <div style=""display:flex; align-items:center; gap:12px; margin-bottom:10px;"">
      <div class=""modal-icon-disc"" style=""margin:0; width:44px; height:44px;"">
        <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z""/></svg>
      </div>
      <div>
        <div class=""modal-title"" id=""batchOfferTitle"" style=""font-size:16px;"">Incoming Batch</div>
        <div class=""modal-desc"" id=""batchOfferDesc"" style=""font-size:11px;"">Select the files you want to receive.</div>
      </div>
    </div>

    <div style=""display:flex; justify-content:space-between; align-items:center; padding:8px 12px; background:rgba(255,255,255,0.04); border:1px solid var(--border-subtle); border-radius:8px; margin-bottom:8px;"">
      <label style=""display:flex; align-items:center; gap:8px; cursor:pointer; font-size:12px; font-weight:700; color:var(--text);"">
        <input type=""checkbox"" id=""batchSelectAllCheck"" onchange=""toggleBatchSelectAll(this.checked)"" checked style=""accent-color:var(--primary); width:16px; height:16px; cursor:pointer;"">
        Select All (<span id=""batchSelectedCount"">0</span>/<span id=""batchTotalCount"">0</span>)
      </label>
      <span id=""batchSelectedSize"" style=""font-size:11px; font-weight:700; color:var(--primary-light);"">0 MB</span>
    </div>

    <div id=""batchOfferFileList"" style=""max-height:240px; overflow-y:auto; display:flex; flex-direction:column; gap:6px; margin-bottom:12px; padding-right:4px;""></div>

    <div class=""modal-actions"" style=""flex-wrap:wrap;"">
      <button class=""modal-btn modal-btn-decline"" onclick=""declineBatchOffer()"">Decline</button>
      <a class=""modal-btn"" id=""batchZipDownloadLink"" href=""#"" style=""background:rgba(16,185,129,0.18); border:1px solid rgba(16,185,129,0.35); color:var(--emerald); text-decoration:none; display:inline-flex; align-items:center; justify-content:center;"">Download 1 ZIP</a>
      <button class=""modal-btn modal-btn-accept"" id=""batchAcceptBtn"" onclick=""acceptSelectedBatchOffer()"">Accept Selected</button>
    </div>
  </div>
</div>

<!-- TRANSFER SUCCESS MODAL -->
<div class=""modal-overlay"" id=""transferSuccessModal"">
  <div class=""modal-sheet"">
    <div class=""modal-icon-disc"" style=""background:rgba(16, 185, 129, 0.15); color:var(--emerald);"">
      <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2.5"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M20 6L9 17l-5-5""/></svg>
    </div>
    <div class=""modal-title"" style=""color:var(--emerald);"">Transfer Completed</div>
    <div class=""modal-desc"" id=""transferSuccessDesc"">Files were transferred successfully.</div>
    <div style=""background:rgba(255,255,255,0.03); border:1px solid var(--border-subtle); border-radius:10px; padding:12px; text-align:left; font-size:12px;"">
      <div style=""display:flex; justify-content:space-between; margin-bottom:6px;"">
        <span style=""color:var(--text-dim);"">Files Delivered:</span>
        <span id=""successFileCount"" style=""font-weight:700; color:var(--text);"">0 files</span>
      </div>
      <div style=""display:flex; justify-content:space-between; margin-bottom:6px;"">
        <span style=""color:var(--text-dim);"">Total Volume:</span>
        <span id=""successTotalSize"" style=""font-weight:700; color:var(--text);"">0 MB</span>
      </div>
      <div style=""display:flex; justify-content:space-between;"">
        <span style=""color:var(--text-dim);"">Peer:</span>
        <span id=""successPeerName"" style=""font-weight:700; color:var(--primary-light);"">This Computer</span>
      </div>
    </div>
    <div class=""modal-actions"">
      <button class=""modal-btn modal-btn-accept"" onclick=""closeSuccessModal()"">Done</button>
    </div>
  </div>
</div>

<!-- TRANSFER FAILURE MODAL -->
<div class=""modal-overlay"" id=""transferFailureModal"">
  <div class=""modal-sheet"">
    <div class=""modal-icon-disc"" style=""background:rgba(244, 63, 94, 0.15); color:var(--rose);"">
      <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2.5"" stroke-linecap=""round"" stroke-linejoin=""round""><line x1=""18"" y1=""6"" x2=""6"" y2=""18""/><line x1=""6"" y1=""6"" x2=""18"" y2=""18""/></svg>
    </div>
    <div class=""modal-title"" style=""color:var(--rose);"">Transfer Interrupted</div>
    <div class=""modal-desc"" id=""transferFailureDesc"">The file transfer was stopped or interrupted.</div>
    <div class=""modal-actions"">
      <button class=""modal-btn modal-btn-decline"" onclick=""closeFailureModal()"">Dismiss</button>
    </div>
  </div>
</div>


<!-- FIRST-TIME DEVICE NAME ONBOARDING MODAL -->
<div class=""modal-overlay"" id=""onboardingNameModal"">
  <div class=""modal-sheet"" style=""max-width:360px;"">
    <div style=""display:flex; justify-content:center; margin-bottom:12px;"">
      <div style=""width:56px; height:56px; border-radius:28px; background:rgba(79,70,229,0.18); display:flex; align-items:center; justify-content:center; color:var(--primary-light);"">
        <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round"" width=""28"" height=""28""><rect x=""5"" y=""2"" width=""14"" height=""20"" rx=""2"" ry=""2""/><line x1=""12"" y1=""18"" x2=""12.01"" y2=""18""/></svg>
      </div>
    </div>
    <div class=""modal-title"" style=""text-align:center; font-size:18px;"">Set Your Device Name</div>
    <div class=""modal-desc"" style=""text-align:center; font-size:12px; margin-bottom:12px;"">Choose a friendly name so you can easily identify your device when sending or receiving files:</div>
    <div style=""position:relative; margin-bottom:8px;"">
      <input type=""text"" id=""onboardingNameInput"" maxlength=""28"" style=""width:100%; box-sizing:border-box; padding:12px 14px; background:rgba(255,255,255,0.06); border:1.5px solid var(--primary); border-radius:10px; color:var(--text); font-size:14px; font-weight:700; outline:none;"">
    </div>
    <div class=""name-chips-row"" id=""nameChipsRow"">
      <span class=""name-chip"" onclick=""setNameChip(this)"">My iPhone</span>
      <span class=""name-chip"" onclick=""setNameChip(this)"">Samsung Galaxy</span>
      <span class=""name-chip"" onclick=""setNameChip(this)"">MacBook</span>
      <span class=""name-chip"" onclick=""setNameChip(this)"">Work Laptop</span>
      <span class=""name-chip"" onclick=""setNameChip(this)"">Personal</span>
    </div>
    <div class=""modal-actions"" style=""margin-top:0;"">
      <button class=""modal-btn modal-btn-accept"" style=""width:100%;"" onclick=""confirmOnboardingName()"">Confirm &amp; Continue</button>
    </div>
  </div>
</div>

<!-- EDIT NICKNAME MODAL -->
<div class=""modal-overlay"" id=""editNicknameModal"">
  <div class=""modal-sheet"">
    <div class=""modal-title"">Device Name</div>
    <div class=""modal-desc"">Change how your device appears to nearby peers:</div>
    <input type=""text"" id=""editNicknameInput"" maxlength=""32"" placeholder=""Device name..."" style=""width:100%; box-sizing:border-box; padding:12px 14px; background:rgba(255,255,255,0.06); border:1.5px solid var(--primary); border-radius:10px; color:var(--text); font-size:14px; font-weight:600; outline:none;"">
    <div class=""modal-actions"">
      <button class=""modal-btn modal-btn-decline"" onclick=""document.getElementById('editNicknameModal').classList.remove('active')"">Cancel</button>
      <button class=""modal-btn modal-btn-accept"" onclick=""confirmEditNickname()"">Save</button>
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
let stagedFiles = [];
let isUploading = false;
let isTransferActive = false;
let currentXhr = null;
let pairedHostName = 'Receiver';
let connectedTarget = null;
let pendingSingleOffer = null;
let pendingBatchManifest = null;
let toastTimeout = null;

function generateRandomDeviceName() {
  const syllables = ['shi', 'wo', 'fe', 'je', 'rig', 'ko', 'la', 'mi', 'zu', 'no', 'ba', 'te', 'lu', 'va', 'ro', 'ki', 'pa', 'ze', 'du', 'li', 'ka', 'po', 'ne', 'si', 'ta', 'ra', 'vi', 'xen', 'mox', 'lun'];
  const s1 = syllables[Math.floor(Math.random() * syllables.length)];
  const s2 = syllables[Math.floor(Math.random() * syllables.length)];
  const s3 = (Math.random() > 0.45) ? syllables[Math.floor(Math.random() * syllables.length)] : '';
  return 'web-' + s1 + s2 + s3;
}

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
    let name = localStorage.getItem('weshare_nickname');
    if (!name) {
      name = detectDeviceDefaultName();
      localStorage.setItem('weshare_nickname', name);
    }
    return name;
  } catch(e) {
    return generateRandomDeviceName();
  }
}

function saveNickname(name) {
  name = (name || '').trim();
  if (!name) return;
  try { 
    localStorage.setItem('weshare_nickname', name); 
    localStorage.setItem('weshare_named', 'true');
  } catch(e) {}
  
  const nd = document.getElementById('nicknameDisplay');
  if (nd) nd.textContent = name;
  const devTitle = document.getElementById('receiveDeviceName');
  if (devTitle) devTitle.textContent = name;

  const cid = getClientId();
  const role = (document.getElementById('viewReceive')?.classList.contains('active')) ? 'Receiver' : 'Sender';
  fetch(`/api/client-role?clientId=${encodeURIComponent(cid)}&role=${role}&name=${encodeURIComponent(name)}`, { method: 'POST' }).catch(() => {});
  fetch(`/api/heartbeat?clientId=${encodeURIComponent(cid)}&name=${encodeURIComponent(name)}&role=${role}`, { method: 'POST' }).catch(() => {});
}

function randomizeOnboardingName() {
  const input = document.getElementById('onboardingNameInput');
  if (input) input.value = generateRandomDeviceName();
}

function confirmOnboardingName() {
  const input = document.getElementById('onboardingNameInput');
  let name = (input ? input.value : '').trim();
  if (!name) name = generateRandomDeviceName();
  saveNickname(name);
  const modal = document.getElementById('onboardingNameModal');
  if (modal) modal.classList.remove('active');
  showToast('Device name confirmed: ' + name);
}

function promptEditNickname() {
  const modal = document.getElementById('editNicknameModal');
  const input = document.getElementById('editNicknameInput');
  if (input) input.value = getSavedNickname();
  if (modal) modal.classList.add('active');
}

function confirmEditNickname() {
  const input = document.getElementById('editNicknameInput');
  let name = (input ? input.value : '').trim();
  if (!name) return;
  saveNickname(name);
  const modal = document.getElementById('editNicknameModal');
  if (modal) modal.classList.remove('active');
  showToast('Device name saved: ' + name);
}

function showToast(msg) {
  const t = document.getElementById('toastPill');
  document.getElementById('toastText').textContent = msg;
  t.classList.add('show');
  if (toastTimeout) clearTimeout(toastTimeout);
  toastTimeout = setTimeout(() => {
    t.classList.remove('show');
  }, 3000);
}

function initTheme() {
  let theme = 'dark';
  try { theme = localStorage.getItem('weshare_theme') || 'dark'; } catch(e) {}
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
  if (!icon) return;
  if (theme === 'light') {
    icon.innerHTML = '<path d=""M12 3a6 6 0 0 0 9 9 9 9 0 1 1-9-9Z""/>';
  } else {
    icon.innerHTML = '<circle cx=""12"" cy=""12"" r=""4""/><path d=""M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M6.34 17.66l-1.41 1.41M19.07 4.93l-1.41 1.41""/>';
  }
}

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
  if (['mp4', 'mkv', 'mov', 'avi', 'wmv', 'flv', 'webm', 'm4v'].includes(ext)) {
    return '<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><polygon points=""23 7 16 12 23 17 23 7""/><rect x=""1"" y=""5"" width=""15"" height=""14"" rx=""2"" ry=""2""/></svg>';
  }
  if (['jpg', 'jpeg', 'png', 'gif', 'webp', 'svg', 'bmp', 'heic'].includes(ext)) {
    return '<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><rect x=""3"" y=""3"" width=""18"" height=""18"" rx=""2"" ry=""2""/><circle cx=""8.5"" cy=""8.5"" r=""1.5""/><polyline points=""21 15 16 10 5 21""/></svg>';
  }
  if (['mp3', 'wav', 'flac', 'aac', 'ogg', 'm4a'].includes(ext)) {
    return '<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M9 18V5l12-2v13""/><circle cx=""6"" cy=""18"" r=""3""/><circle cx=""18"" cy=""16"" r=""3""/></svg>';
  }
  if (['zip', 'rar', '7z', 'tar', 'gz'].includes(ext)) {
    return '<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M21 8v13H3V8""/><path d=""M1 3h22v5H1z""/><path d=""M10 12h4""/></svg>';
  }
  return '<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z""/><polyline points=""14 2 14 8 20 8""/></svg>';
}

/* ------------------------------------------------------------- */
/* TAB SWITCHING & ROLE BROADCAST                                */
/* ------------------------------------------------------------- */
function switchTab(name) {
  if (isTransferActive && name !== 'transfer') {
    showToast('File transfer is currently active.');
    return;
  }

  document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
  document.querySelectorAll('.tab-view').forEach(v => v.classList.remove('active'));

  if (name === 'transfer') {
    const vt = document.getElementById('viewTransfer');
    if (vt) vt.classList.add('active');
  } else if (name === 'send') {
    document.getElementById('tabBtnSend').classList.add('active');
    document.getElementById('viewSend').classList.add('active');
    fetch('/api/client-role?clientId=' + getClientId() + '&role=Sender&name=' + encodeURIComponent(getSavedNickname()), { method: 'POST' }).catch(() => {});
    // When entering Send mode, immediately start radar to choose/get device!
    if (!connectedTarget) {
      startSenderRadar();
    } else {
      document.getElementById('sendRadarStage').style.display = 'none';
      document.getElementById('sendSelectionStage').style.display = 'block';
    }
  } else if (name === 'receive') {
    document.getElementById('tabBtnReceive').classList.add('active');
    document.getElementById('viewReceive').classList.add('active');
    if (radarScanInterval) { clearInterval(radarScanInterval); radarScanInterval = null; }
    fetch('/api/client-role?clientId=' + getClientId() + '&role=Receiver&name=' + encodeURIComponent(getSavedNickname()), { method: 'POST' }).catch(() => {});
  } else if (name === 'downloads') {
    document.getElementById('tabBtnDownloads').classList.add('active');
    document.getElementById('viewDownloads').classList.add('active');
    if (radarScanInterval) { clearInterval(radarScanInterval); radarScanInterval = null; }
    fetch('/api/client-role?clientId=' + getClientId() + '&role=Idle&name=' + encodeURIComponent(getSavedNickname()), { method: 'POST' }).catch(() => {});
    loadAvailableFiles();
  } else if (name === 'activity') {
    document.getElementById('tabBtnActivity').classList.add('active');
    document.getElementById('viewActivity').classList.add('active');
    if (radarScanInterval) { clearInterval(radarScanInterval); radarScanInterval = null; }
    fetch('/api/client-role?clientId=' + getClientId() + '&role=Idle&name=' + encodeURIComponent(getSavedNickname()), { method: 'POST' }).catch(() => {});
    loadHistory();
  } else if (name === 'share') {
    document.getElementById('tabBtnShare').classList.add('active');
    document.getElementById('viewShare').classList.add('active');
    if (radarScanInterval) { clearInterval(radarScanInterval); radarScanInterval = null; }
    fetch('/api/client-role?clientId=' + getClientId() + '&role=Idle&name=' + encodeURIComponent(getSavedNickname()), { method: 'POST' }).catch(() => {});
  }
}

/* ------------------------------------------------------------- */
/* FILE STAGING IN SENDER FLOW                                   */
/* ------------------------------------------------------------- */
function onFilesSelected(fileList) {
  if (!fileList || fileList.length === 0) return;
  for (let i = 0; i < fileList.length; i++) {
    const f = fileList[i];
    f.customRelativePath = f.webkitRelativePath || f.name;
    stagedFiles.push(f);
  }
  document.getElementById('multiFileInput').value = '';
  document.getElementById('cameraInput').value = '';
  renderStagingTray();
  showToast('Added ' + fileList.length + ' file(s) to queue');
}

function onFolderSelected(fileList) {
  if (!fileList || fileList.length === 0) return;
  for (let i = 0; i < fileList.length; i++) {
    const f = fileList[i];
    f.customRelativePath = f.webkitRelativePath || f.name;
    stagedFiles.push(f);
  }
  document.getElementById('folderInput').value = '';
  renderStagingTray();
  showToast('Added folder with ' + fileList.length + ' file(s)');
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
  updateSendButtonText();
  const tray = document.getElementById('stagingTray');
  const list = document.getElementById('stagingList');
  const countBadge = document.getElementById('stagingCountBadge');
  const tabBadge = document.getElementById('stagingBadge');
  const totalSizeEl = document.getElementById('stagingTotalSize');

  if (stagedFiles.length === 0) {
    tray.style.display = 'none';
    tabBadge.style.display = 'none';
    return;
  }

  tray.style.display = 'flex';
  tabBadge.style.display = 'inline-block';
  tabBadge.textContent = stagedFiles.length;
  countBadge.textContent = `${stagedFiles.length} file${stagedFiles.length > 1 ? 's' : ''}`;

  const totalBytes = stagedFiles.reduce((acc, f) => acc + (f.size || 0), 0);
  totalSizeEl.textContent = formatBytes(totalBytes);

  list.innerHTML = '';
  stagedFiles.forEach((f, idx) => {
    const item = document.createElement('div');
    item.className = 'staging-item';
    item.innerHTML = `
      <div class=""si-left"">
        <div class=""si-icon"">${getFileCategorySvg(f.name)}</div>
        <div class=""si-info"">
          <span class=""si-name"" title=""${f.name}"">${f.name}</span>
          <span class=""si-size"">${formatBytes(f.size)}</span>
        </div>
      </div>
      <button class=""si-remove-btn"" onclick=""removeStagedFile(${idx})"" title=""Remove file"">
        <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2.2"" stroke-linecap=""round"" stroke-linejoin=""round""><line x1=""18"" y1=""6"" x2=""6"" y2=""18""/><line x1=""6"" y1=""6"" x2=""18"" y2=""18""/></svg>
      </button>
    `;
    list.appendChild(item);
  });
}

/* ------------------------------------------------------------- */
/* RADAR STAGE (SENDER DISCOVERY - SHAREit STYLE)               */
/* ------------------------------------------------------------- */
let radarScanInterval = null;


function startSenderRadar() {
  document.getElementById('sendSelectionStage').style.display = 'none';
  document.getElementById('sendRadarStage').style.display = 'block';
  loadDiscoveredReceivers();
  if (radarScanInterval) clearInterval(radarScanInterval);
  radarScanInterval = setInterval(loadDiscoveredReceivers, 2500);
}

function connectToTarget(rec) {
  if (radarScanInterval) {
    clearInterval(radarScanInterval);
    radarScanInterval = null;
  }
  connectedTarget = rec;
  pairedHostName = rec.name;
  
  const nameEl = document.getElementById('connectedTargetName');
  if (nameEl) nameEl.textContent = `${rec.name} (${rec.type || 'Device'})`;
  
  updateSendButtonText();
  
  document.getElementById('sendRadarStage').style.display = 'none';
  document.getElementById('sendSelectionStage').style.display = 'block';
  
  showToast(`Selected ${rec.name}! Now choose files to send.`);
}

function sendBatchToConnectedTarget() {
  if (!connectedTarget) {
    showToast('Please select a recipient from the Radar first.');
    startSenderRadar();
    return;
  }
  if (stagedFiles.length === 0) {
    showToast('Please select at least one file to send.');
    return;
  }
  sendBatchToTarget(connectedTarget.id, connectedTarget.name);
}

function updateSendButtonText() {
  const btnText = document.getElementById('sendAllBtnText');
  if (!btnText) return;
  const count = stagedFiles.length;
  const target = connectedTarget ? connectedTarget.name : 'Recipient';
  btnText.textContent = `Send ${count} File${count === 1 ? '' : 's'} to ${target}`;
}

async function loadDiscoveredReceivers() {
  const container = document.getElementById('radarNodesContainer');
  const emptyHint = document.getElementById('radarEmptyHint');
  if (!container) return;

  try {
    // 1. Check This Computer info
    const meRes = await fetch('/api/me').then(r => r.json()).catch(() => null);
    // 2. Check other discovered devices
    const peers = await fetch('/api/devices?role=Receiver').then(r => r.json()).catch(() => []);

    const myId = getClientId();
    const receivers = [];

    // Check if This Computer is in Receive Mode
    if (meRes && meRes.name) {
      const isHostRecv = meRes.isReceiver || meRes.role === 'Receiver';
      if (isHostRecv) {
        receivers.push({
          id: 'pc',
          name: meRes.name,
          type: 'PC',
          isHost: true
        });
      }
    }

    // Check other LAN peers
    if (Array.isArray(peers)) {
      peers.forEach(d => {
        if (!d) return;
        if (d.id === myId || (d.id && d.id.includes(myId))) return;
        const isRecv = d.isReceiver || d.role === 'Receiver';
        // STRICT RULE: Only show devices that are in RECEIVE MODE!
        if (isRecv) {
          receivers.push({
            id: d.id,
            name: d.name,
            type: d.type || 'Phone',
            isHost: false
          });
        }
      });
    }

    container.innerHTML = '';


    if (receivers.length === 0) {
      if (emptyHint) emptyHint.style.display = 'flex';
      if (!window.radarAssistantTimer) {
        window.radarAssistantTimer = setTimeout(() => {
          const rsa = document.getElementById('radarSmartAssistant');
          if (rsa) rsa.style.display = 'flex';
        }, 6000);
      }
      return;
    }

    if (emptyHint) emptyHint.style.display = 'none';
    const rsa = document.getElementById('radarSmartAssistant');
    if (rsa) rsa.style.display = 'none';
    if (window.radarAssistantTimer) {
      clearTimeout(window.radarAssistantTimer);
      window.radarAssistantTimer = null;
    }


    // Render both on radar rings and in clean selection list below
    const listContainer = document.getElementById('radarReceiversList');
    if (listContainer) listContainer.innerHTML = '';

    const radarRadius = 105;
    const total = receivers.length;
    receivers.forEach((rec, idx) => {
      const angle = (2 * Math.PI / total) * idx - (Math.PI / 2);
      const x = 140 + radarRadius * Math.cos(angle);
      const y = 140 + radarRadius * Math.sin(angle);

      const node = document.createElement('div');
      node.className = 'web-radar-node';
      node.style.left = `${x}px`;
      node.style.top = `${y}px`;
      node.onclick = () => connectToTarget(rec);

      const iconSvg = rec.type === 'PC'
        ? '<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><rect x=""2"" y=""3"" width=""20"" height=""14"" rx=""2"" ry=""2""/><line x1=""8"" y1=""21"" x2=""16"" y2=""21""/><line x1=""12"" y1=""17"" x2=""12"" y2=""21""/></svg>'
        : '<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><rect x=""5"" y=""2"" width=""14"" height=""20"" rx=""2"" ry=""2""/><line x1=""12"" y1=""18"" x2=""12.01"" y2=""18""/></svg>';

      node.innerHTML = `
        <div class=""web-radar-node-disc"" title=""Tap to choose ${rec.name}"">
          ${iconSvg}
        </div>
        <span class=""web-radar-node-label"" title=""${rec.name}"">${rec.name}</span>
      `;
      container.appendChild(node);

      if (listContainer) {
        const row = document.createElement('div');
        row.style.cssText = 'background:var(--card-solid); border:1px solid var(--border); border-radius:12px; padding:10px 14px; display:flex; justify-content:space-between; align-items:center; cursor:pointer;';
        row.onclick = () => connectToTarget(rec);
        row.innerHTML = `
          <div style=""display:flex; align-items:center; gap:10px;"">
            <div style=""width:34px; height:34px; border-radius:8px; background:rgba(16,185,129,0.12); color:var(--emerald); display:flex; align-items:center; justify-content:center;"">
              ${iconSvg}
            </div>
            <div style=""display:flex; flex-direction:column;"">
              <span style=""font-size:13px; font-weight:800; color:var(--text);"">${rec.name}</span>
              <span style=""font-size:11px; color:var(--emerald); font-weight:600;"">Ready to Receive</span>
            </div>
          </div>
          <button type=""button"" style=""background:var(--primary); color:#FFFFFF; border:none; border-radius:8px; padding:6px 14px; font-size:11px; font-weight:700; cursor:pointer;"">Select</button>
        `;
        listContainer.appendChild(row);
      }
    });

  } catch (err) {
    console.warn('Failed to load discovered receivers:', err);
  }
}

/* ------------------------------------------------------------- */
/* BATCH DATA TRANSFER TO TARGET RECEIVER                        */
/* ------------------------------------------------------------- */
async function sendBatchToTarget(targetId, targetName) {
  acquireWakeLock();
  if (stagedFiles.length === 0 || isUploading) return;
  isUploading = true;

  if (radarScanInterval) {
    clearInterval(radarScanInterval);
    radarScanInterval = null;
  }

  const clientId = getClientId();
  const batchId = 'wb_' + Date.now();
  const total = stagedFiles.length;
  const totalBatchBytes = stagedFiles.reduce((acc, f) => acc + (f.size || 0), 0);

  isTransferActive = true;
  pairedHostName = targetName || 'Receiver';
  switchTab('transfer');

  // Populate viewTransfer
  const roleBadge = document.getElementById('transferPageRoleBadge');
  if (roleBadge) {
    roleBadge.textContent = 'UPLOADING';
    roleBadge.style.color = '#A855F7';
  }
  const peerLabel = document.getElementById('transferPagePeerName');
  if (peerLabel) peerLabel.textContent = pairedHostName;

  const queueCountEl = document.getElementById('transferPageQueueCount');
  if (queueCountEl) queueCountEl.textContent = `${total} file${total > 1 ? 's' : ''}`;
  const filesDoneEl = document.getElementById('transferPageFilesText');
  if (filesDoneEl) filesDoneEl.textContent = `0 / ${total}`;
  const bytesDoneEl = document.getElementById('transferPageBytesText');
  if (bytesDoneEl) bytesDoneEl.textContent = `0 B / ${formatBytes(totalBatchBytes)}`;

  const queueList = document.getElementById('transferPageQueueList');
  if (queueList) {
    queueList.innerHTML = '';
    stagedFiles.forEach((f, idx) => {
      const row = document.createElement('div');
      row.id = 'transferPageQueueItem_' + idx;
      row.className = 'transfer-queue-row';
      row.innerHTML = `
        <div style=""display:flex; align-items:center; gap:10px; overflow:hidden;"">
          <div style=""color:var(--text-muted);"">${getFileCategorySvg(f.name)}</div>
          <div style=""display:flex; flex-direction:column; overflow:hidden;"">
            <span style=""font-size:13px; font-weight:700; color:var(--text); white-space:nowrap; overflow:hidden; text-overflow:ellipsis;"" title=""${f.name}"">${f.name}</span>
            <span style=""font-size:11px; color:var(--text-dim);"">${formatBytes(f.size)}</span>
          </div>
        </div>
        <span id=""transferPageQueueStatus_${idx}"" style=""font-size:11px; font-weight:700; color:var(--text-muted); padding:3px 8px; border-radius:6px; background:rgba(255,255,255,0.04);"">Waiting</span>
      `;
      queueList.appendChild(row);
    });
  }

  let successCount = 0;
  let totalBytesTransferred = 0;
  let failMessage = '';

  for (let i = 0; i < stagedFiles.length; i++) {
    if (!isUploading) break;
    const file = stagedFiles[i];

    const statusEl = document.getElementById('transferPageQueueStatus_' + i);
    if (statusEl) {
      statusEl.textContent = 'Transferring...';
      statusEl.style.color = 'var(--cyan)';
      statusEl.style.background = 'rgba(6,182,212,0.12)';
    }

    const titleEl = document.getElementById('transferPageFileTitle');
    if (titleEl) titleEl.textContent = `[${i + 1}/${total}] ${file.name}`;
    updateTransferStats(0, file.size, 0, '--');

    try {
      // 1. Send pre-transfer check to ensure receiver is ready
      const askUrl = '/api/ask-receive?clientId=' + encodeURIComponent(clientId) +
                     '&targetId=' + encodeURIComponent(targetId) +
                     '&name=' + encodeURIComponent(file.name) +
                     '&size=' + file.size;
      const askRes = await fetch(askUrl, { method: 'POST' }).then(r => r.json());
      if (!askRes || !askRes.accepted) {
        throw new Error(askRes && askRes.error ? askRes.error : 'Recipient is not in Receive mode');
      }

      // 2. Stream the raw file payload directly via HTTP upload
      await streamFileUpload(file, askRes.id || '', clientId, batchId, file.customRelativePath || file.webkitRelativePath || '');

      successCount++;
      totalBytesTransferred += file.size;

      if (statusEl) {
        statusEl.textContent = 'Completed';
        statusEl.style.color = 'var(--emerald)';
        statusEl.style.background = 'rgba(16,185,129,0.12)';
      }
      if (filesDoneEl) filesDoneEl.textContent = `${successCount} / ${total}`;
      if (bytesDoneEl) bytesDoneEl.textContent = `${formatBytes(totalBytesTransferred)} / ${formatBytes(totalBatchBytes)}`;
    } catch (err) {
      console.error('File upload failed:', err);
      failMessage = err.message || 'Upload error';
      if (statusEl) {
        statusEl.textContent = 'Failed';
        statusEl.style.color = '#EF4444';
        statusEl.style.background = 'rgba(239,68,68,0.12)';
      }
      break;
    }
  }

  isUploading = false;
  isTransferActive = false;

  if (successCount > 0 && successCount === total) {
    fetch(`/api/batch-complete?clientId=${encodeURIComponent(clientId)}&count=${successCount}&bytes=${totalBytesTransferred}`, { method: 'POST' }).catch(() => {});
    releaseWakeLock();
    showTransferSuccessModal(true, pairedHostName, successCount, totalBytesTransferred);
    stagedFiles = [];
    renderStagingTray();
    closeSenderRadar();
  } else {
    releaseWakeLock();
    showTransferFailureModal(failMessage || 'Transfer interrupted.', pairedHostName);
  }

  loadHistory();
}

/* ------------------------------------------------------------- */
/* STREAM FILE UPLOAD ENGINE (HTTP RAW POST)                     */
/* ------------------------------------------------------------- */
function streamFileUpload(file, uploadId, clientId, batchId, relativePath) {
  return new Promise((resolve, reject) => {
    let isFinished = false;

    const safeResolve = () => {
      if (isFinished) return;
      isFinished = true;
      currentXhr = null;
      updateTransferStats(100, file.size, file.size, 0, 'Done');
      resolve();
    };

    const safeReject = (err) => {
      if (isFinished) return;
      isFinished = true;
      currentXhr = null;
      reject(err);
    };

    const xhr = new XMLHttpRequest();
    currentXhr = xhr;

    let uploadUrl = uploadId
      ? `/upload?id=${encodeURIComponent(uploadId)}&clientId=${encodeURIComponent(clientId)}&name=${encodeURIComponent(file.name)}`
      : `/upload?name=${encodeURIComponent(file.name)}&size=${file.size}&clientId=${encodeURIComponent(clientId)}&batchId=${encodeURIComponent(batchId)}&relPath=${encodeURIComponent(relativePath)}`;

    xhr.open('POST', uploadUrl, true);
    xhr.setRequestHeader('X-File-Name', encodeURIComponent(file.name));
    xhr.setRequestHeader('X-Relative-Path', encodeURIComponent(relativePath || ''));

    let lastLoaded = 0;
    let lastTime = Date.now();
    let rollingSpeed = 0;

    xhr.upload.onprogress = (e) => {
      if (e.lengthComputable && e.total > 0) {
        const now = Date.now();
        const dt = (now - lastTime) / 1000;
        if (dt > 0.25) {
          const bytesDiff = e.loaded - lastLoaded;
          const currentSpeed = (bytesDiff / dt) / (1024 * 1024);
          rollingSpeed = rollingSpeed === 0 ? currentSpeed : (rollingSpeed * 0.6 + currentSpeed * 0.4);
          lastLoaded = e.loaded;
          lastTime = now;
        }

        const pct = Math.min(100, Math.round((e.loaded / e.total) * 100));
        let etaStr = '--';
        if (rollingSpeed > 0.05) {
          const remBytes = e.total - e.loaded;
          const remSec = Math.round((remBytes / (1024 * 1024)) / rollingSpeed);
          const m = Math.floor(remSec / 60);
          const s = remSec % 60;
          etaStr = `${m}m ${s < 10 ? '0' : ''}${s}s`;
        }

        updateTransferStats(pct, e.total, e.loaded, rollingSpeed, etaStr);
      }
    };

    xhr.onload = () => {
      if (xhr.status >= 200 && xhr.status < 300) {
        safeResolve();
      } else {
        safeReject(new Error(`Server error: ${xhr.status}`));
      }
    };

    xhr.onerror = () => {
      safeReject(new Error('Network connection error during transfer'));
    };

    xhr.onabort = () => {
      safeReject(new Error('Transfer cancelled'));
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
  isTransferActive = false;
  showToast('Transfer cancelled');
  switchTab('send');
}

function updateTransferStats(percent, totalBytes, loadedBytes, speedMb, etaStr) {
  const pgBar = document.getElementById('transferPageBarFill');
  if (pgBar) pgBar.style.width = percent + '%';
  const pgPct = document.getElementById('transferPagePercentText');
  if (pgPct) pgPct.textContent = percent + '%';
  const pgSpeed = document.getElementById('transferPageSpeedText');
  if (pgSpeed) pgSpeed.textContent = (speedMb || 0).toFixed(1) + ' MB/s';
  const pgEta = document.getElementById('transferPageEtaText');
  if (pgEta) pgEta.textContent = etaStr || '--:--';
  const pgBytes = document.getElementById('transferPageBytesText');
  if (pgBytes) pgBytes.textContent = `${formatBytes(loadedBytes)} / ${formatBytes(totalBytes)}`;
}

/* ------------------------------------------------------------- */
/* AVAILABLE FILES & HISTORY                                     */
/* ------------------------------------------------------------- */
async function loadAvailableFiles() {
  try {
    const cid = getClientId();
    const files = await fetch('/api/files?clientId=' + encodeURIComponent(cid)).then(r => r.json());
    const list = document.getElementById('downloadFilesList');
    const badge = document.getElementById('downloadsBadge');

    if (!Array.isArray(files) || files.length === 0) {
      if (badge) badge.style.display = 'none';
      list.innerHTML = `
        <div class=""empty-state-box"">
          <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""1.8"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M13 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V9z""/><polyline points=""13 2 13 9 20 9""/></svg>
          <div class=""empty-state-title"">No Files Available</div>
          <div class=""empty-state-text"">Files shared from the PC will appear here for instant 1-click download.</div>
        </div>`;
      return;
    }

    if (badge) {
      badge.style.display = 'inline-block';
      badge.textContent = files.length;
    }
    list.innerHTML = '';

    files.forEach(f => {
      const item = document.createElement('div');
      item.className = 'file-card-item';
      const fileId = f.id || '';
      item.innerHTML = `
        <div class=""fci-left"">
          <div class=""fci-icon"">${getFileCategorySvg(f.name)}</div>
          <div style=""display:flex; flex-direction:column; overflow:hidden;"">
            <span class=""fci-name"" title=""${f.name}"">${f.name}</span>
            <span class=""fci-meta"">${formatBytes(f.size)}</span>
          </div>
        </div>
        <div style=""display:flex; gap:6px; align-items:center;"">
          ${(/\.(jpg|jpeg|png|webp|heic|mp4|mov)$/i.test(f.name))
            ? `<button class=""fci-dl-btn"" style=""background:rgba(16,185,129,0.18); color:var(--emerald); border:1px solid rgba(16,185,129,0.3);"" onclick=""saveMediaToPhotos('${encodeURIComponent(fileId)}', '${encodeURIComponent(f.name)}')"">Photos</button>`
            : ''}
          <a class=""fci-dl-btn"" href=""/download?id=${encodeURIComponent(fileId)}&file=${encodeURIComponent(f.name)}&clientId=${encodeURIComponent(cid)}"" download=""${f.name}"">
            <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2.5"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4""/><polyline points=""7 10 12 15 17 10""/><line x1=""12"" y1=""15"" x2=""12"" y2=""3""/></svg>
            Download
          </a>
        </div>
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
          <div class=""empty-state-text"">Completed transfers between this device and peers will be recorded here.</div>
        </div>`;
      return;
    }

    list.innerHTML = '';
    items.forEach(h => {
      const isRecv = (h.direction === 0 || h.direction === 'Received');
      const isDone = (h.status === 2 || h.status === 'Completed' || h.status === 'Done');
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
        <span class=""hi-status ${isDone ? 'completed' : 'failed'}"">${isDone ? 'COMPLETED' : 'FAILED'}</span>
      `;
      list.appendChild(item);
    });
  } catch(e) {}
}


/* ------------------------------------------------------------- */
/* SMART DEVICE AUTO-DETECTION                                   */
/* ------------------------------------------------------------- */
function detectDeviceDefaultName() {
  const ua = navigator.userAgent || '';
  if (/iPad/i.test(ua)) return 'My iPad';
  if (/iPhone/i.test(ua)) return 'My iPhone';
  if (/Macintosh|Mac OS X/i.test(ua)) return 'My MacBook';
  if (/Android/i.test(ua)) {
    if (/Samsung/i.test(ua)) return 'Samsung Galaxy';
    if (/Pixel/i.test(ua)) return 'Google Pixel';
    return 'Android Phone';
  }
  if (/Windows/i.test(ua)) return 'Windows PC';
  return 'Personal Device';
}

function setNameChip(el) {
  const input = document.getElementById('onboardingNameInput');
  if (input) input.value = el.textContent.trim();
}

/* ------------------------------------------------------------- */
/* SCREEN WAKE LOCK API (STOPS MOBILE PHONE SLEEP IN TRANSFERS) */
/* ------------------------------------------------------------- */
let activeWakeLock = null;
async function acquireWakeLock() {
  try {
    if ('wakeLock' in navigator && !activeWakeLock) {
      activeWakeLock = await navigator.wakeLock.request('screen');
      activeWakeLock.addEventListener('release', () => { activeWakeLock = null; });
    }
  } catch(e) {}
}

function releaseWakeLock() {
  try {
    if (activeWakeLock) {
      activeWakeLock.release();
      activeWakeLock = null;
    }
  } catch(e) {}
}

/* ------------------------------------------------------------- */
/* WEB SHARE API (SAVE DIRECTLY TO PHOTOS / CAMERA ROLL)         */
/* ------------------------------------------------------------- */
async function saveMediaToPhotos(fileId, fileName) {
  try {
    showToast('Preparing photo/video for Photos app...');
    const cid = getClientId();
    const resp = await fetch(`/download?id=${encodeURIComponent(fileId)}&file=${encodeURIComponent(fileName)}&clientId=${encodeURIComponent(cid)}`);
    const blob = await resp.blob();
    const ext = (fileName.split('.').pop() || '').toLowerCase();
    let mime = 'image/jpeg';
    if (['png', 'webp', 'gif'].includes(ext)) mime = `image/${ext}`;
    else if (['mp4', 'mov', 'webm'].includes(ext)) mime = `video/${ext === 'mov' ? 'quicktime' : ext}`;
    
    const file = new File([blob], fileName, { type: mime });
    if (navigator.canShare && navigator.canShare({ files: [file] })) {
      await navigator.share({
        files: [file],
        title: fileName
      });
      showToast('Saved to Photos / Camera Roll!');
    } else {
      const a = document.createElement('a');
      a.href = URL.createObjectURL(blob);
      a.download = fileName;
      a.click();
      showToast('Download started');
    }
  } catch (err) {
    showToast('Download started');
  }
}

/* ------------------------------------------------------------- */
/* GLOBAL INCOMING TRANSFER ALERT BANNER                         */
/* ------------------------------------------------------------- */
let globalIncomingData = null;

function  {
  globalIncomingData = { data, isBatch };
  const banner = document.getElementById('globalIncomingBanner');
  const title = document.getElementById('gibSenderTitle');
  const info = document.getElementById('gibInfoText');
  const zipBtn = document.getElementById('gibZipBtn');
  const acceptBtn = document.getElementById('gibAcceptBtn');

  if (!banner || !title || !info) return;

  const sender = data.senderName || data.from || 'This Computer';
  title.textContent = `Incoming from ${sender}`;

  if (isBatch) {
    const files = data.files || [];
    const totalBytes = data.totalSize || files.reduce((acc, f) => acc + (f.size || 0), 0);
    info.textContent = `${files.length} file${files.length > 1 ? 's' : ''} • Total: ${formatBytes(totalBytes)}`;
    if (zipBtn) {
      zipBtn.style.display = 'inline-flex';
      zipBtn.href = `/download-zip?clientId=${encodeURIComponent(getClientId())}`;
      zipBtn.onclick = () => { dismissGlobalIncoming(); showToast('Downloading ZIP archive...'); };
    }
    acceptBtn.textContent = 'Accept & Download';
  } else {
    const cleanName = data.displayName || data.name || 'file';
    info.textContent = `""${cleanName}"" • ${formatBytes(data.size || 0)}`;
    if (zipBtn) zipBtn.style.display = 'none';
    acceptBtn.textContent = 'Accept & Download';
  }

  banner.classList.add('active');
  acquireWakeLock();
}

function dismissGlobalIncoming() {
  const banner = document.getElementById('globalIncomingBanner');
  if (banner) banner.classList.remove('active');
  releaseWakeLock();
}

function acceptGlobalIncoming() {
  dismissGlobalIncoming();
  if (!globalIncomingData) return;
  if (globalIncomingData.isBatch) {
    acceptBatchManifest();
  } else {
    acceptSingleOffer();
  }
}

function declineGlobalIncoming() {
  dismissGlobalIncoming();
  if (!globalIncomingData) return;
  if (globalIncomingData.isBatch) {
    declineBatchManifest();
  } else {
    declineSingleOffer();
  }
}

/* ------------------------------------------------------------- */
/* SSE LISTENER & INCOMING OFFERS                                */
/* ------------------------------------------------------------- */
function initSSE() {
  const cid = getClientId();
  const cname = getSavedNickname();
  const sse = new EventSource(`/api/events?clientId=${encodeURIComponent(cid)}&name=${encodeURIComponent(cname)}&role=Sender`);

  sse.addEventListener('offer', (e) => {
    try {
      pendingSingleOffer = JSON.parse(e.data);
      const cleanName = decodeURIComponent(pendingSingleOffer.name || 'file');
      pendingSingleOffer.displayName = cleanName;
      document.getElementById('singleOfferDesc').textContent = `Incoming file from ${pendingSingleOffer.from || 'This Computer'}: ""${cleanName}"" (${formatBytes(pendingSingleOffer.size)})`;
      document.getElementById('singleOfferModal').classList.add('active');
      
    } catch(err) {}
  });

  sse.addEventListener('batch-manifest', (e) => {
    try {
      const manifest = JSON.parse(e.data);
      showIncomingBatchChecklist(manifest);
      
    } catch(err) {}
  });

  sse.addEventListener('batch-offer', (e) => {
    try {
      const data = JSON.parse(e.data);
      const filesArr = Array.isArray(data) ? data : (data.files || []);
      filesArr.forEach(f => {
        f.fileName = decodeURIComponent(f.name || f.fileName || 'file');
      });
      showIncomingBatchChecklist({ files: filesArr, senderName: 'This Computer' });
      
    } catch(err) {}
  });

  sse.addEventListener('batch-complete', (e) => {
    try {
      const data = JSON.parse(e.data);
      showTransferSuccessModal(false, pairedHostName, data.count || 1, data.bytes || 0);
    } catch(err) {}
  });

  sse.addEventListener('upload-complete', () => {
    loadHistory();
    loadAvailableFiles();
  });

  sse.addEventListener('refresh', () => {
    loadAvailableFiles();
    loadDiscoveredReceivers();
  });
}

function acceptSingleOffer() {
  const modal = document.getElementById('singleOfferModal');
  if (modal) modal.classList.remove('active');
  if (pendingSingleOffer) {
    const fid = pendingSingleOffer.id || pendingSingleOffer.fileId || '';
    const cid = getClientId();
    const cleanName = pendingSingleOffer.displayName || pendingSingleOffer.name || 'file';
    const a = document.createElement('a');
    a.href = `/download?id=${encodeURIComponent(fid)}&file=${encodeURIComponent(cleanName)}&clientId=${encodeURIComponent(cid)}`;
    a.download = cleanName;
    document.body.appendChild(a);
    a.click();
    setTimeout(() => a.remove(), 1000);
    showToast('Download started: ' + cleanName);
  }
}

function declineSingleOffer() {
  const modal = document.getElementById('singleOfferModal');
  if (modal) modal.classList.remove('active');
  if (pendingSingleOffer) {
    const fid = pendingSingleOffer.id || pendingSingleOffer.fileId || '';
    const cid = getClientId();
    fetch(`/api/decline?id=${encodeURIComponent(fid)}&clientId=${encodeURIComponent(cid)}`, { method: 'POST' }).catch(() => {});
    showToast('Transfer declined');
  }
}

function showIncomingBatchChecklist(manifest) {
  pendingBatchManifest = manifest;
  const modal = document.getElementById('batchOfferModal');
  const title = document.getElementById('batchOfferTitle');
  const desc = document.getElementById('batchOfferDesc');
  const list = document.getElementById('batchOfferFileList');
  const allChk = document.getElementById('batchSelectAllCheck');

  const files = manifest.files || manifest.Files || [];
  title.textContent = `Incoming Batch (${files.length} Files)`;
  desc.textContent = `From ${manifest.senderName || manifest.SenderName || 'This Computer'} • Total: ${formatBytes(manifest.totalSize || files.reduce((a, b) => a + (b.size || 0), 0))}`;

  allChk.checked = true;
  list.innerHTML = '';

  files.forEach((f, idx) => {
    const fid = f.id || f.fileId || ('f_' + idx);
    const fname = f.fileName || f.name || 'file';
    const fsize = f.size || 0;

    const row = document.createElement('label');
    row.className = 'batch-chk-item';
    row.innerHTML = `
      <input type=""checkbox"" class=""batch-file-chk"" data-file-id=""${fid}"" data-file-name=""${encodeURIComponent(fname)}"" data-size=""${fsize}"" checked onchange=""updateBatchChecklistStats()"">
      <div style=""flex:1; overflow:hidden;"">
        <span class=""batch-chk-name"" title=""${fname}"">${fname}</span>
        <span style=""font-size:10px; color:var(--text-dim); display:block;"">${formatBytes(fsize)}</span>
      </div>
    `;
    list.appendChild(row);
  });

  updateBatchChecklistStats();
  modal.classList.add('active');
}

function toggleBatchSelectAll(checked) {
  document.querySelectorAll('.batch-file-chk').forEach(c => c.checked = checked);
  updateBatchChecklistStats();
}

function updateBatchChecklistStats() {
  const chks = document.querySelectorAll('.batch-file-chk');
  let selected = 0;
  let totalBytes = 0;
  chks.forEach(c => {
    if (c.checked) {
      selected++;
      totalBytes += parseInt(c.getAttribute('data-size') || '0', 10);
    }
  });

  document.getElementById('batchSelectedCount').textContent = selected;
  document.getElementById('batchTotalCount').textContent = chks.length;
  document.getElementById('batchSelectedSize').textContent = formatBytes(totalBytes);

  const acceptBtn = document.getElementById('batchAcceptBtn');
  if (acceptBtn) {
    acceptBtn.textContent = `Accept Selected (${selected})`;
    acceptBtn.disabled = selected === 0;
  }
}

async function acceptSelectedBatchOffer() {
  const modal = document.getElementById('batchOfferModal');
  if (modal) modal.classList.remove('active');

  if (!pendingBatchManifest) return;

  const acceptedFiles = [];
  document.querySelectorAll('.batch-file-chk:checked').forEach(c => {
    acceptedFiles.push({
      id: c.getAttribute('data-file-id'),
      name: c.getAttribute('data-file-name') || ''
    });
  });

  const batchId = pendingBatchManifest.batchId || pendingBatchManifest.BatchId || '';
  await fetch('/api/batch-response', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      batchId: batchId,
      accepted: acceptedFiles.length > 0,
      acceptedFileIds: acceptedFiles.map(x => x.id)
    })
  }).catch(() => {});

  if (acceptedFiles.length > 0) {
    showToast(`Accepted ${acceptedFiles.length} files. Starting download...`);
    const cid = getClientId();
    acceptedFiles.forEach((item, idx) => {
      setTimeout(() => {
        const a = document.createElement('a');
        a.href = `/download?id=${encodeURIComponent(item.id)}&file=${item.name}&clientId=${encodeURIComponent(cid)}`;
        a.download = decodeURIComponent(item.name) || 'download';
        document.body.appendChild(a);
        a.click();
        setTimeout(() => a.remove(), 1000);
      }, idx * 350);
    });
  }
}

function declineBatchOffer() {
  const modal = document.getElementById('batchOfferModal');
  if (modal) modal.classList.remove('active');
  showToast('Batch transfer declined');
}

function showTransferSuccessModal(isSender, peerName, count, bytes) {
  document.getElementById('successFileCount').textContent = `${count} file${count > 1 ? 's' : ''}`;
  document.getElementById('successTotalSize').textContent = formatBytes(bytes);
  document.getElementById('successPeerName').textContent = peerName || 'Peer';
  document.getElementById('transferSuccessModal').classList.add('active');
}

function closeSuccessModal() {
  document.getElementById('transferSuccessModal').classList.remove('active');
  switchTab('send');
}

function showTransferFailureModal(reason, peerName) {
  document.getElementById('transferFailureDesc').textContent = reason || 'The transfer was interrupted.';
  document.getElementById('transferFailureModal').classList.add('active');
}

function closeFailureModal() {
  document.getElementById('transferFailureModal').classList.remove('active');
  switchTab('send');
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
  const devTitle = document.getElementById('receiveDeviceName');
  if (devTitle) devTitle.textContent = getSavedNickname();

  const urlDisplay = document.getElementById('portalUrlDisplay');
  if (urlDisplay) urlDisplay.textContent = window.location.origin;

  loadAvailableFiles();
  loadHistory();
  initSSE();

  // Send initial role as Sender (matching initial active tab)
  fetch('/api/client-role?clientId=' + getClientId() + '&role=Sender&name=' + encodeURIComponent(getSavedNickname()), { method: 'POST' }).catch(() => {});
  fetch('/api/heartbeat?clientId=' + getClientId() + '&name=' + encodeURIComponent(getSavedNickname()), { method: 'POST' }).catch(() => {});

  setInterval(() => {
    fetch('/api/heartbeat?clientId=' + getClientId() + '&name=' + encodeURIComponent(getSavedNickname()), { method: 'POST' }).catch(() => {});
  }, 8000);
});
</script>

</body>
</html>";
    }
}
