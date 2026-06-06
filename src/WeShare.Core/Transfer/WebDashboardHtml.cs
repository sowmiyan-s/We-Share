namespace WeShare.Core.Transfer
{
    public partial class WebDashboardService
    {
        private string GetDashboardHtml() => @"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<meta name='viewport' content='width=device-width,initial-scale=1,maximum-scale=1,user-scalable=no'>
<meta name='description' content='We Share Web Portal for fast, secure local file sharing between your mobile device and PC.'>
<title>We Share | Portal</title>

<style>
@import url('https://fonts.googleapis.com/css2?family=Bricolage+Grotesque:opsz,wght@12..96,300..800&family=Plus+Jakarta+Sans:ital,wght@0,300..800;1,300..800&display=swap');

:root {
  --bg: #09090b;
  --panel: #18181b;
  --border: #27272a;
  --primary: #10b981;
  --primary-gradient: linear-gradient(135deg, #10b981 0%, #059669 100%);
  --text: #f4f4f5;
  --text-dim: #a1a1aa;
  --card: #202023;
  --input-bg: #27272a;
  --badge-bg: rgba(16, 185, 129, 0.1);
  --badge-text: #34d399;
}
html[data-theme='light'] {
  --bg: #fafafa;
  --panel: #ffffff;
  --border: #e4e4e7;
  --primary: #059669;
  --primary-gradient: linear-gradient(135deg, #059669 0%, #047857 100%);
  --text: #09090b;
  --text-dim: #71717a;
  --card: #f4f4f5;
  --input-bg: #e4e4e7;
  --badge-bg: rgba(5, 150, 105, 0.08);
  --badge-text: #047857;
}

* { box-sizing: border-box; margin: 0; padding: 0; -webkit-tap-highlight-color: transparent; }
body {
  background: var(--bg);
  color: var(--text);
  font-family: 'Plus Jakarta Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  line-height: 1.6;
  overflow-x: hidden;
  transition: background 0.3s ease, color 0.3s ease;
}
.container {
  max-width: 600px;
  margin: 0 auto;
  padding: 30px 20px;
  display: flex;
  flex-direction: column;
  gap: 24px;
}

/* -- Marquee Banner -- */
.marquee-banner {
  background: var(--panel);
  border: 1px solid var(--border);
  border-radius: 12px;
  padding: 8px 16px;
  overflow: hidden;
  white-space: nowrap;
  position: relative;
  display: flex;
  align-items: center;
}
.marquee-content {
  display: inline-block;
  padding-left: 100%;
  animation: marquee-scroll 30s linear infinite;
  font-family: 'Bricolage Grotesque', sans-serif;
  font-size: 10px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 1.5px;
  color: var(--primary);
}
@keyframes marquee-scroll {
  0% { transform: translate3d(0, 0, 0); }
  100% { transform: translate3d(-100%, 0, 0); }
}

/* -- Header -- */
header {
  background: var(--panel);
  border: 1px solid var(--border);
  border-radius: 20px;
  padding: 24px;
  display: flex;
  flex-direction: column;
  gap: 20px;
  position: relative;
}
.header-top {
  display: flex;
  align-items: center;
  gap: 16px;
}
.logo {
  width: 48px;
  height: 48px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1px solid var(--border);
  background: var(--card);
}
.brand-container { flex: 1; }
.brand { 
  font-family: 'Bricolage Grotesque', sans-serif; 
  font-size: 20px; 
  font-weight: 800; 
  letter-spacing: 1.5px; 
}
.host-status {
  font-size: 11px;
  color: var(--text-dim);
  font-weight: 700;
  letter-spacing: 0.5px;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  margin-top: 6px;
  padding: 4px 10px;
  border-radius: 20px;
  background: var(--input-bg);
}

/* -- Status Pulse Ring -- */
.status-pulse {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  position: relative;
  display: inline-block;
}
.status-pulse::after {
  content: '';
  position: absolute;
  top: 0; left: 0; right: 0; bottom: 0;
  border-radius: 50%;
  border: 2px solid transparent;
}
.status-pulse.online {
  background-color: #10b981;
}
.status-pulse.online::after {
  border-color: #10b981;
  animation: pulse-ring 1.5s cubic-bezier(0.215, 0.61, 0.355, 1) infinite;
}
.status-pulse.connecting {
  background-color: #f59e0b;
}
.status-pulse.connecting::after {
  border-color: #f59e0b;
  animation: pulse-ring 1.5s cubic-bezier(0.215, 0.61, 0.355, 1) infinite;
}
.status-pulse.offline {
  background-color: #ef4444;
}
.status-pulse.offline::after {
  border-color: #ef4444;
}
@keyframes pulse-ring {
  0% { transform: scale(0.5); opacity: 1; }
  100% { transform: scale(2.5); opacity: 0; }
}

/* -- Profile Settings in Header -- */
.profile-box {
  display: flex;
  align-items: center;
  gap: 12px;
  background: var(--card);
  border: 1px solid var(--border);
  padding: 12px 16px;
  border-radius: 14px;
}
.profile-label { 
  font-family: 'Bricolage Grotesque', sans-serif; 
  font-size: 11px; 
  font-weight: 800; 
  color: var(--text-dim); 
  letter-spacing: 0.5px;
}
.profile-input {
  flex: 1;
  background: var(--input-bg);
  border: 1px solid var(--border);
  color: var(--text);
  padding: 8px 12px;
  border-radius: 8px;
  font-family: inherit;
  font-size: 13px;
  font-weight: 600;
  outline: none;
  transition: all 0.2s;
}
.profile-input:focus { 
  border-color: var(--primary); 
  background: var(--panel);
}

/* -- Section -- */
section {
  background: var(--panel);
  border: 1px solid var(--border);
  border-radius: 20px;
  padding: 24px;
  display: flex;
  flex-direction: column;
}
.section-title {
  font-family: 'Bricolage Grotesque', sans-serif;
  font-size: 12px;
  font-weight: 800;
  color: var(--text-dim);
  text-transform: uppercase;
  letter-spacing: 1.5px;
  margin-bottom: 18px;
  display: flex;
  align-items: center;
  gap: 12px;
}
.section-title::after { content: ''; flex: 1; height: 1px; background: var(--border); }

/* -- Upload Zone -- */
.drop-zone {
  position: relative;
  background: var(--card);
  border: 1.5px dashed var(--border);
  border-radius: 16px;
  padding: 40px 20px;
  text-align: center;
  transition: all 0.2s ease;
  cursor: pointer;
}
.drop-zone:hover {
  border-color: var(--primary);
  background: var(--input-bg);
}
.drop-zone.drag {
  border-color: var(--primary);
  background: var(--input-bg);
}
.drop-zone input {
  position: absolute;
  inset: 0;
  opacity: 0;
  cursor: pointer;
}
.dz-icon-container {
  margin-bottom: 12px;
  display: inline-block;
  transition: transform 0.2s ease;
}
.drop-zone:hover .dz-icon-container { transform: translateY(-3px); }
.dz-text { 
  font-family: 'Bricolage Grotesque', sans-serif; 
  font-size: 16px; 
  font-weight: 800; 
  margin-bottom: 4px; 
}
.dz-sub { font-size: 12px; color: var(--text-dim); }

/* -- File Cards -- */
.file-list { display: flex; flex-direction: column; gap: 10px; }
.file-card {
  background: var(--card);
  border: 1px solid var(--border);
  border-radius: 12px;
  padding: 12px 14px;
  display: flex;
  align-items: center;
  gap: 14px;
  text-decoration: none;
  color: inherit;
  transition: all 0.2s ease;
}
.file-card:hover {
  border-color: var(--primary);
  background: var(--input-bg);
}
.f-icon {
  width: 40px;
  height: 40px;
  background: var(--input-bg);
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1px solid var(--border);
}
.f-info { flex: 1; min-width: 0; }
.f-name { 
  font-family: 'Bricolage Grotesque', sans-serif; 
  font-size: 14px; 
  font-weight: 700; 
  white-space: nowrap; 
  overflow: hidden; 
  text-overflow: ellipsis; 
}
.f-size { font-size: 12px; color: var(--text-dim); font-weight: 500; margin-top: 2px; }
.f-btn {
  font-family: 'Bricolage Grotesque', sans-serif;
  background: var(--primary-gradient);
  color: white;
  border: none;
  padding: 8px 16px;
  border-radius: 8px;
  font-size: 11px;
  font-weight: 800;
  cursor: pointer;
  transition: opacity 0.2s;
}
.f-btn:hover { opacity: 0.9; }

/* -- Progress Overlay -- */
.overlay {
  position: fixed;
  inset: 0;
  background: rgba(9, 9, 11, 0.96);
  z-index: 1000;
  display: none;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 24px;
  text-align: center;
}
.overlay.active { display: flex; }

/* -- Windows Style Transfer Dialog -- */
.win-dialog {
  background: var(--panel);
  border: 1px solid var(--border);
  border-radius: 16px;
  width: 100%;
  max-width: 400px;
  padding: 24px;
  font-family: inherit;
  color: var(--text);
  box-sizing: border-box;
}

.win-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-family: 'Bricolage Grotesque', sans-serif;
  font-size: 15px;
  font-weight: 800;
  margin-bottom: 20px;
  color: var(--text-dim);
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.win-close-btn {
  font-size: 24px;
  cursor: pointer;
  color: var(--text-dim);
  line-height: 1;
  transition: color 0.2s;
}
.win-close-btn:hover {
  color: var(--text);
}

.win-file-info {
  margin-bottom: 20px;
  text-align: left;
}

.win-filename {
  font-family: 'Bricolage Grotesque', sans-serif;
  font-size: 16px;
  font-weight: 800;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  margin-bottom: 6px;
}

.win-detail-text {
  font-size: 12px;
  color: var(--text-dim);
  font-weight: 500;
}

.win-progress-row {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 20px;
}

.win-progress-bar-bg {
  flex: 1;
  height: 8px;
  background: var(--input-bg);
  border-radius: 4px;
  overflow: hidden;
  position: relative;
  border: 1px solid var(--border);
}

.win-progress-bar-fill {
  height: 100%;
  width: 0%;
  background: #107c41; /* Windows Green */
  border-radius: 4px;
  transition: width 0.1s linear;
}

.win-percent {
  font-family: 'Bricolage Grotesque', sans-serif;
  font-size: 14px;
  font-weight: 800;
  min-width: 35px;
  text-align: right;
}

.win-graph-container {
  border: 1px solid var(--border);
  border-radius: 12px;
  background: var(--card);
  overflow: hidden;
  margin-bottom: 20px;
  position: relative;
  height: 120px;
}

#speedGraph {
  display: block;
  width: 100%;
  height: 100%;
}

.win-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.win-cancel-btn {
  font-family: 'Bricolage Grotesque', sans-serif;
  background: transparent;
  border: 1px solid var(--border);
  color: var(--text);
  padding: 8px 18px;
  border-radius: 8px;
  font-size: 12px;
  font-weight: 800;
  cursor: pointer;
  transition: all 0.2s;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.win-cancel-btn:hover {
  background: var(--input-bg);
  border-color: var(--text-dim);
}

/* -- Toast -- */
#toast {
  position: fixed;
  bottom: 24px;
  left: 50%;
  transform: translateX(-50%) translateY(100px);
  background: var(--primary-gradient);
  color: white;
  padding: 12px 24px;
  border-radius: 30px;
  font-family: 'Bricolage Grotesque', sans-serif;
  font-size: 13px;
  font-weight: 800;
  transition: transform 0.3s cubic-bezier(0.175, 0.885, 0.32, 1.275);
  z-index: 1100;
}
#toast.show { transform: translateX(-50%) translateY(0); }

/* -- Theme Button -- */
#themeToggle {
  position: absolute;
  top: 24px;
  right: 24px;
  background: var(--card);
  border: 1px solid var(--border);
  border-radius: 10px;
  width: 40px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  color: var(--text);
  transition: all 0.2s;
}
#themeToggle:hover { border-color: var(--primary); background: var(--input-bg); }

/* -- Captive Portal Warning Card -- */
.captive-warning-card {
  background: rgba(245, 158, 11, 0.08);
  border: 1px solid rgba(245, 158, 11, 0.2);
  border-radius: 16px;
  padding: 16px;
  display: none;
  gap: 12px;
  align-items: flex-start;
  margin-bottom: 10px;
}
.cw-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
}
.cw-content {
  flex: 1;
}
.cw-title {
  font-family: 'Bricolage Grotesque', sans-serif;
  font-weight: 800;
  font-size: 14px;
  color: #f59e0b;
  margin-bottom: 4px;
}
.cw-text {
  font-size: 12px;
  color: var(--text-dim);
  line-height: 1.5;
}
.cw-link {
  color: var(--primary);
  font-weight: 700;
  text-decoration: underline;
  word-break: break-all;
}

/* -- Accept Dialog Modal -- */
.dialog-overlay {
  position: fixed;
  top: 0; left: 0; right: 0; bottom: 0;
  background: rgba(9, 9, 11, 0.96);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 2000;
  opacity: 0;
  pointer-events: none;
  transition: opacity 0.2s ease;
}
.dialog-overlay.active {
  opacity: 1;
  pointer-events: auto;
}
.dialog-box {
  background: var(--panel);
  border: 1px solid var(--border);
  border-radius: 20px;
  padding: 32px 24px;
  width: 90%;
  max-width: 380px;
  text-align: center;
  transform: scale(0.95);
  transition: transform 0.2s ease;
}
.dialog-overlay.active .dialog-box {
  transform: scale(1);
}
.dialog-icon {
  margin-bottom: 16px;
  display: inline-block;
  animation: bounce 2s infinite;
}
@keyframes bounce {
  0%, 100% { transform: translateY(0); }
  50% { transform: translateY(-6px); }
}
.dialog-title {
  font-family: 'Bricolage Grotesque', sans-serif;
  font-size: 20px;
  font-weight: 800;
  margin-bottom: 10px;
  letter-spacing: 0.5px;
}
.dialog-msg {
  font-size: 13px;
  color: var(--text-dim);
  margin-bottom: 24px;
  line-height: 1.6;
}
.dialog-actions {
  display: flex;
  gap: 12px;
}
.dialog-actions button {
  flex: 1;
  padding: 12px 16px;
  border-radius: 10px;
  font-family: 'Bricolage Grotesque', sans-serif;
  font-size: 13px;
  font-weight: 800;
  cursor: pointer;
  border: none;
  transition: all 0.2s;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}
.btn-decline {
  background: rgba(239, 68, 68, 0.08);
  color: #ef4444;
  border: 1px solid rgba(239, 68, 68, 0.15) !important;
}
.btn-decline:hover {
  background: rgba(239, 68, 68, 0.15);
}
.btn-accept {
  background: var(--primary-gradient);
  color: white;
}
.btn-accept:hover {
  opacity: 0.95;
}
</style>
</head>
<body>

<div class='container'>
  <div class='marquee-banner'>
    <div class='marquee-content'>
      LOCAL HIGH-SPEED SHARING ACTIVE • SECURE DIRECT P2P TRANSFER • ZERO CLOUD STORAGE • WE SHARE PORTAL v1.0.0 • CONNECTED AND READY
    </div>
  </div>

  <div id='captiveWarning' class='captive-warning-card' style='position: relative; display: none; flex-direction: column; gap: 12px;'>
    <div style='display: flex; gap: 12px; align-items: flex-start;'>
      <div class='cw-icon'>
        <svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='#f59e0b' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round' style='width: 24px; height: 24px; min-width: 24px;'><path d='M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z'/><line x1='12' y1='9' x2='12' y2='13'/><line x1='12' y1='17' x2='12.01' y2='17'/></svg>
      </div>
      <div class='cw-content'>
        <div class='cw-title'>File Uploads Restricted?</div>
        <div class='cw-text'>If you are using the system 'Sign in' popup, your phone restricts file uploads. Tap the button below to open WeShare in your default browser:</div>
      </div>
      <button onclick=""document.getElementById('captiveWarning').style.display='none'"" style=""background:none; border:none; color:var(--text-dim); cursor:pointer; font-size:18px; line-height:1; padding:0 4px;"">&times;</button>
    </div>
    <div style='padding-left: 36px;'>
      <button onclick='openInBrowser()' style='background: var(--primary-gradient); color: white; border: none; padding: 8px 16px; border-radius: 8px; font-family: inherit; font-size: 11px; font-weight: 700; cursor: pointer; display: inline-flex; align-items: center; gap: 6px;'>
        <svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round' style='width: 12px; height: 12px;'><path d='M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6'/><polyline points='15 3 21 3 21 9'/><line x1='10' y1='14' x2='21' y2='3'/></svg>
        OPEN IN SYSTEM BROWSER
      </button>
    </div>
  </div>

  <header>
    <button id='themeToggle' aria-label='Toggle theme'></button>
    <div class='header-top'>
      <div class='logo' style='padding: 6px;'>
        <img src='/api/logo' style='width: 100%; height: 100%; object-fit: contain;' alt='Logo'/>
      </div>
      <div class='brand-container'>
        <div class='brand'>WE SHARE</div>
        <div id='hostLine' class='host-status'><span class='status-pulse connecting'></span>CONNECTING...</div>
      </div>
    </div>
    
    <div class='profile-box'>
      <div class='profile-label'>YOUR NICKNAME:</div>
      <input type='text' id='nameInput' class='profile-input' placeholder='Enter nickname' maxlength='15'>
    </div>
  </header>

  <section id='uploadSection'>
    <div class='section-title'>Send to PC</div>
    <div class='drop-zone' id='dropZone'>
      <input type='file' id='fileInput' multiple onchange='handleFiles(this.files)'>
      <div class='dz-icon-container'>
        <svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round' style='width: 44px; height: 44px; stroke: var(--primary);'><path d='M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4'/><polyline points='17 8 12 3 7 8'/><line x1='12' y1='3' x2='12' y2='15'/></svg>
      </div>
      <div class='dz-text'>Tap or Drop Files</div>
      <div class='dz-sub'>Files will be sent straight to the host PC</div>
    </div>
  </section>

  <section id='filesSection' style='display:none'>
    <div class='section-title'>Files from PC</div>
    <div class='file-list' id='fileList'></div>
  </section>

  <section id='shareSection'>
    <div class='section-title'>Share Portal</div>
    <div style='display: flex; align-items: center; gap: 20px;'>
      <img src='/api/qr' style='width: 100px; height: 100px; border-radius: 12px; border: 1px solid var(--border); background: white; padding: 4px;' alt='Portal QR Code'/>
      <div style='flex: 1;'>
        <div style='font-weight: 700; font-size: 13px; margin-bottom: 4px;'>Scan to join</div>
        <div style='font-size: 11px; color: var(--text-dim); line-height: 1.4;'>Scan this QR on another device to instantly open the We Share Web Portal and exchange files.</div>
      </div>
    </div>
  </section>
</div>

<div class='overlay' id='overlay'>
  <div class='win-dialog'>
    <div class='win-header'>
      <span id='overlayTitle'>Copying...</span>
      <span class='win-close-btn' onclick='window.location.reload()'>&times;</span>
    </div>
    
    <div class='win-file-info'>
      <div id='ovFile' class='win-filename'>filename.ext</div>
      <div id='winTimeSpeed' class='win-detail-text'>Calculating speed and time...</div>
    </div>
    
    <div class='win-progress-row'>
      <div class='win-progress-bar-bg'>
        <div id='winProgressBarFill' class='win-progress-bar-fill'></div>
      </div>
      <div id='progText' class='win-percent'>0%</div>
    </div>

    <div class='win-graph-container'>
      <canvas id='speedGraph' width='300' height='100'></canvas>
    </div>

    <div class='win-footer'>
      <div id='winProgressDetails' class='win-detail-text'>0 KB of 0 KB</div>
      <button class='win-cancel-btn' onclick='window.location.reload()'>Cancel</button>
    </div>
  </div>
</div>

<div id='toast'>Files sent successfully!</div>

<div class='dialog-overlay' id='acceptDialogOverlay'>
  <div class='dialog-box'>
    <div class='dialog-icon'>
      <svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round' style='width: 48px; height: 48px; stroke: var(--primary);'><path d='M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4'/><polyline points='7 10 12 15 17 10'/><line x1='12' y1='15' x2='12' y2='3'/></svg>
    </div>
    <div class='dialog-title'>Incoming File</div>
    <div class='dialog-msg' id='dialogMsg'>Incoming file from PC: filename.ext (size).</div>
    <div class='dialog-actions'>
      <button class='btn-decline' id='btnDecline'>Decline</button>
      <button class='btn-accept' id='btnAccept'>Accept</button>
    </div>
  </div>
</div>

<script>
const CIRC = 2 * Math.PI * 70;
let sse = null;
let speedHistory = [];
const maxGraphPoints = 40;

const sunIcon = `<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round' style='width: 18px; height: 18px; display: block;'><circle cx='12' cy='12' r='4'/><path d='M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M6.34 17.66l-1.41 1.41M19.07 4.93l-1.41 1.41'/></svg>`;
const moonIcon = `<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round' style='width: 18px; height: 18px; display: block;'><path d='M12 3a6 6 0 0 0 9 9 9 9 0 1 1-9-9Z'/></svg>`;

function getClientId() {
  try {
    let id = localStorage.getItem('weshare_client_id');
    if (!id) {
      id = 'wc_' + Math.random().toString(36).substring(2, 11) + '_' + Date.now().toString(36);
      localStorage.setItem('weshare_client_id', id);
    }
    return id;
  } catch (e) {
    if (!window.weshare_mem_client_id) {
      window.weshare_mem_client_id = 'wc_mem_' + Math.random().toString(36).substring(2, 11) + '_' + Date.now().toString(36);
    }
    return window.weshare_mem_client_id;
  }
}

function getClientName() {
  try {
    let name = localStorage.getItem('weshare_client_name');
    if (!name) {
      const platforms = ['iPhone', 'Android', 'iPad', 'Mac', 'Windows', 'Linux'];
      const ua = navigator.userAgent;
      let detected = 'Mobile Web';
      for (const p of platforms) {
        if (ua.includes(p)) { detected = p + ' Web'; break; }
      }
      name = detected + ' ' + Math.floor(Math.random() * 900 + 100);
      localStorage.setItem('weshare_client_name', name);
    }
    return name;
  } catch (e) {
    if (!window.weshare_mem_client_name) {
      const platforms = ['iPhone', 'Android', 'iPad', 'Mac', 'Windows', 'Linux'];
      const ua = navigator.userAgent;
      let detected = 'Mobile Web';
      for (const p of platforms) {
        if (ua.includes(p)) { detected = p + ' Web'; break; }
      }
      window.weshare_mem_client_name = detected + ' ' + Math.floor(Math.random() * 900 + 100);
    }
    return window.weshare_mem_client_name;
  }
}

function initName() {
  const input = document.getElementById('nameInput');
  input.value = getClientName();
  input.addEventListener('change', () => {
    let name = input.value.trim();
    if (!name) name = 'Web Client';
    try {
      localStorage.setItem('weshare_client_name', name);
    } catch (e) {
      window.weshare_mem_client_name = name;
    }
    connectSSE();
    showToast('Nickname updated to ' + name);
  });
}

let currentOffer = null;
let loadedFilesList = [];
let offerQueue = [];
let isShowingOffer = false;

function escapeHtml(str) {
  return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/""/g, '&quot;').replace(/'/g, '&#039;');
}

function downloadFile(id, name) {
  return new Promise((resolve, reject) => {
    const xhr = new XMLHttpRequest();
    const cId = getClientId();
    xhr.open('GET', '/download?id=' + id + '&clientId=' + cId);
    xhr.responseType = 'blob';
    
    speedHistory = [];
    let lastTime = Date.now();
    let lastLoaded = 0;
    
    xhr.onprogress = e => {
      if (e.lengthComputable) {
        const pct = e.loaded / e.total * 100;
        const now = Date.now();
        const elapsed = (now - lastTime) / 1000;
        
        if (elapsed >= 0.25 || pct >= 99.9) {
          const loadedDiff = e.loaded - lastLoaded;
          const speedMb = elapsed > 0 ? (loadedDiff / elapsed) / 1000000 : 0;
          
          lastTime = now;
          lastLoaded = e.loaded;
          
          setProg(pct);
          
          document.getElementById('winTimeSpeed').textContent = speedMb.toFixed(1) + ' MB/s';
          document.getElementById('winProgressDetails').textContent = fmt(e.loaded) + ' of ' + fmt(e.total);
          drawGraph(speedMb);
        }
      }
    };
    
    xhr.onload = () => {
      if (xhr.status === 200) {
        const blob = xhr.response;
        const blobUrl = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = blobUrl;
        link.download = name;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(blobUrl);
        resolve();
      } else {
        reject(new Error('Download failed with status ' + xhr.status));
      }
    };
    
    xhr.onerror = () => {
      reject(new Error('Network error during download'));
    };
    
    xhr.send();
  });
}

function triggerFileDownload(idx) {
  const file = loadedFilesList[idx];
  if (!file) return;
  const ov = document.getElementById('overlay');
  document.getElementById('overlayTitle').textContent = 'DOWNLOADING';
  document.getElementById('ovFile').textContent = file.name;
  setProg(0);
  ov.classList.add('active');
  downloadFile(file.id, file.name)
    .then(() => {
      showToast('Downloaded ' + file.name);
    })
    .catch(err => {
      console.error(err);
      showToast('Download failed');
    })
    .finally(() => {
      ov.classList.remove('active');
    });
}

function handleIncomingOffer(offer) {
  offerQueue.push(offer);
  processNextOffer();
}

function processNextOffer() {
  if (isShowingOffer || offerQueue.length === 0) return;
  isShowingOffer = true;
  const offer = offerQueue.shift();
  currentOffer = offer;
  document.getElementById('dialogMsg').textContent = 'Do you want to accept this file from host PC?\n\n' + offer.name + ' (' + fmt(offer.size) + ')';
  document.getElementById('acceptDialogOverlay').classList.add('active');
}

window.addEventListener('load', () => {
  document.getElementById('btnAccept').addEventListener('click', () => {
    if (!currentOffer) return;
    const offer = currentOffer;
    document.getElementById('acceptDialogOverlay').classList.remove('active');
    isShowingOffer = false;
    
    const ov = document.getElementById('overlay');
    document.getElementById('overlayTitle').textContent = 'DOWNLOADING';
    document.getElementById('ovFile').textContent = offer.name;
    setProg(0);
    ov.classList.add('active');
    downloadFile(offer.id, offer.name)
      .then(() => {
        showToast('Downloaded ' + offer.name);
      })
      .catch(err => {
        console.error(err);
        showToast('Download failed');
      })
      .finally(() => {
        ov.classList.remove('active');
        processNextOffer();
      });
  });

  document.getElementById('btnDecline').addEventListener('click', () => {
    if (!currentOffer) return;
    const id = getClientId();
    const offer = currentOffer;
    document.getElementById('acceptDialogOverlay').classList.remove('active');
    isShowingOffer = false;
    fetch('/api/decline?clientId=' + id + '&id=' + offer.id, { method: 'POST' })
      .then(() => {
        showToast('Declined ' + offer.name);
      })
      .catch(console.error)
      .finally(() => {
        processNextOffer();
      });
  });
});

function connectSSE() {
  if (sse) {
    sse.close();
  }
  const id = getClientId();
  const name = getClientName();
  sse = new EventSource('/api/events?clientId=' + id + '&name=' + encodeURIComponent(name));
  sse.onopen = () => {
    updateConnectionState();
  };
  sse.onerror = () => {
    if (sse.readyState === 0) { // EventSource.CONNECTING
      document.getElementById('hostLine').innerHTML = `<span class='status-pulse connecting'></span>RECONNECTING...`;
    } else {
      document.getElementById('hostLine').innerHTML = `<span class='status-pulse offline'></span>DISCONNECTED`;
    }
  };
  sse.onmessage = e => {
    if (e.data === 'refresh') {
      loadFiles();
    } else if (e.data.startsWith('offer:')) {
      try {
        const data = JSON.parse(e.data.substring(6));
        handleIncomingOffer({ id: data.id, name: decodeURIComponent(data.name), size: data.size });
      } catch (ex) {
        console.error('Error parsing SSE offer:', ex);
      }
    }
  };
}

async function updateConnectionState() {
  try {
    const me = await fetch('/api/me').then(r=>r.json());
    document.getElementById('hostLine').innerHTML = `<span class='status-pulse online'></span>CONNECTED TO ` + escapeHtml((me.name).toUpperCase());
  } catch(_){
    document.getElementById('hostLine').innerHTML = `<span class='status-pulse offline'></span>DISCONNECTED`;
  }
}

function openInBrowser() {
  const host = window.location.host;
  const url = window.location.protocol + '//' + host;
  const ua = navigator.userAgent.toLowerCase();
  
  if (/android/i.test(ua)) {
    const intentUrl = 'intent://' + host + '/#Intent;scheme=http;action=android.intent.action.VIEW;end';
    window.location.href = intentUrl;
  } else {
    navigator.clipboard.writeText(url).then(() => {
      alert('Link copied! Please open Safari, paste the link, and hit Go.');
    }).catch(() => {
      const el = document.createElement('textarea');
      el.value = url;
      document.body.appendChild(el);
      el.select();
      document.execCommand('copy');
      document.body.removeChild(el);
      alert('Link copied! Please open Safari, paste the link, and hit Go.');
    });
  }
}

function checkCaptivePortal() {
  const ua = navigator.userAgent.toLowerCase();
  const isMobile = /android|webos|iphone|ipad|ipod|blackberry|iemobile|opera mini/i.test(ua);
  
  const isIOSCNA = (ua.includes('iphone') || ua.includes('ipad') || ua.includes('ipod')) && !ua.includes('safari');
  const isAndroidCNA = ua.includes('captiveportallogin') || ua.includes('g-portal') || ua.includes('okhttp') || ua.includes('; wv)');
  
  if (isMobile && (isIOSCNA || isAndroidCNA || document.referrer.includes('captive') || window.name === 'captive')) {
    const card = document.getElementById('captiveWarning');
    card.style.display = 'flex';
    
    const dz = document.getElementById('dropZone');
    const input = document.getElementById('fileInput');
    if (dz && input) {
      input.disabled = true;
      dz.style.opacity = '0.5';
      dz.style.pointerEvents = 'none';
      dz.style.cursor = 'not-allowed';
      const dzText = dz.querySelector('.dz-text');
      const dzSub = dz.querySelector('.dz-sub');
      if (dzText) dzText.textContent = 'Uploads Restricted in Sign-in Window';
      if (dzSub) dzSub.textContent = 'Please open in Chrome/Safari to send files';
    }
  }
}

async function init() {
  initName();
  updateThemeButton();
  connectSSE();
  await updateConnectionState();
  loadFiles();
  checkCaptivePortal();
}

async function loadFiles() {
  try {
    const id = getClientId();
    const files = await fetch('/api/files?clientId=' + id).then(r=>r.json());
    loadedFilesList = files || [];
    const sec = document.getElementById('filesSection');
    const list = document.getElementById('fileList');
    if (loadedFilesList.length) {
      sec.style.display = 'block';
      list.innerHTML = loadedFilesList.map((f, idx) => {
        const svgIcon = getFileIconSvg(f.name);
        return `
        <div class='file-card'>
          <div class='f-icon'>${svgIcon}</div>
          <div class='f-info'>
            <div class='f-name'>${escapeHtml(f.name)}</div>
            <div class='f-size'>${fmt(f.size)}</div>
          </div>
          <button class='f-btn' onclick='triggerFileDownload(${idx})'>GET</button>
        </div>
      `}).join('');
    } else { sec.style.display = 'none'; }
  } catch(_){}
}

function getFileIconSvg(name) {
  const ext = name.split('.').pop().toLowerCase();
  if (['png', 'jpg', 'jpeg', 'gif', 'webp', 'svg'].includes(ext)) {
    return `<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round' style='width: 22px; height: 22px; stroke: #06b6d4;'><rect x='3' y='3' width='18' height='18' rx='2' ry='2'/><circle cx='8.5' cy='8.5' r='1.5'/><polyline points='21 15 16 10 5 21'/></svg>`;
  }
  if (['mp4', 'mkv', 'avi', 'mov', 'webm'].includes(ext)) {
    return `<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round' style='width: 22px; height: 22px; stroke: #10b981;'><path d='M23 7l-7 5 7 5V7z'/><rect x='1' y='5' width='15' height='14' rx='2' ry='2'/></svg>`;
  }
  if (['mp3', 'wav', 'flac', 'ogg', 'm4a'].includes(ext)) {
    return `<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round' style='width: 22px; height: 22px; stroke: #ec4899;'><path d='M9 18V5l12-2v13'/><circle cx='6' cy='18' r='3'/><circle cx='18' cy='16' r='3'/></svg>`;
  }
  if (['pdf', 'doc', 'docx', 'xls', 'xlsx', 'ppt', 'pptx', 'txt'].includes(ext)) {
    return `<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round' style='width: 22px; height: 22px; stroke: #3b82f6;'><path d='M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z'/><polyline points='14 2 14 8 20 8'/><line x1='16' y1='13' x2='8' y2='13'/><line x1='16' y1='17' x2='8' y2='17'/></svg>`;
  }
  if (['zip', 'rar', 'tar', 'gz', '7z'].includes(ext)) {
    return `<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round' style='width: 22px; height: 22px; stroke: #eab308;'><polyline points='22 12 16 12 14 15 10 15 8 12 2 12'/><path d='M5.45 5.11L2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.45-6.89A2 2 0 0 0 16.76 4H7.24a2 2 0 0 0-1.79 1.11z'/></svg>`;
  }
  return `<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round' style='width: 22px; height: 22px; stroke: var(--text-dim);'><path d='M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z'/><polyline points='14 2 14 8 20 8'/></svg>`;
}

function drawGraph(speed) {
  speedHistory.push(speed);
  if (speedHistory.length > maxGraphPoints) {
    speedHistory.shift();
  }

  const canvas = document.getElementById('speedGraph');
  if (!canvas) return;
  const ctx = canvas.getContext('2d');
  const w = canvas.width;
  const h = canvas.height;
  
  ctx.clearRect(0, 0, w, h);
  
  // Find max speed to scale graph
  let maxSpeed = Math.max(...speedHistory, 5); // at least scale up to 5 MB/s
  
  // Draw grid lines
  ctx.strokeStyle = 'rgba(128, 128, 128, 0.15)';
  ctx.lineWidth = 1;
  ctx.beginPath();
  for (let i = 1; i < 4; i++) {
    const y = (h / 4) * i;
    ctx.moveTo(0, y);
    ctx.lineTo(w, y);
  }
  ctx.stroke();
  
  // Draw speed line
  ctx.strokeStyle = '#107c41'; // Windows Green
  ctx.lineWidth = 2.5;
  ctx.beginPath();
  
  const step = w / (maxGraphPoints - 1);
  const getX = (idx) => idx * step;
  const getY = (val) => h - (val / maxSpeed) * (h - 20) - 10;
  
  // Align data to right of the graph
  const startIndex = maxGraphPoints - speedHistory.length;
  
  ctx.moveTo(getX(startIndex), getY(speedHistory[0]));
  for (let i = 1; i < speedHistory.length; i++) {
    ctx.lineTo(getX(startIndex + i), getY(speedHistory[i]));
  }
  ctx.stroke();
  
  // Fill under the line
  ctx.fillStyle = 'rgba(16, 124, 65, 0.15)';
  ctx.beginPath();
  ctx.moveTo(getX(startIndex), h);
  ctx.lineTo(getX(startIndex), getY(speedHistory[0]));
  for (let i = 1; i < speedHistory.length; i++) {
    ctx.lineTo(getX(startIndex + i), getY(speedHistory[i]));
  }
  ctx.lineTo(getX(startIndex + speedHistory.length - 1), h);
  ctx.closePath();
  ctx.fill();
  
  // Draw current speed value text in graph
  ctx.fillStyle = 'var(--text)';
  ctx.font = '10px sans-serif';
  ctx.fillText(maxSpeed.toFixed(1) + ' MB/s max', 8, 15);
}

async function handleFiles(files) {
  if (!files.length) return;
  const id = getClientId();
  let successCount = 0;
  let failMessage = '';
  
  for (const f of files) {
    const ov = document.getElementById('overlay');
    document.getElementById('overlayTitle').textContent = 'Requesting permission...';
    document.getElementById('ovFile').textContent = f.name;
    setProg(0);
    speedHistory = [];
    
    // Clear graph drawing
    const canvas = document.getElementById('speedGraph');
    if (canvas) {
      const ctx = canvas.getContext('2d');
      ctx.clearRect(0, 0, canvas.width, canvas.height);
    }
    
    ov.classList.add('active');

    try {
      // 1. Request permission
      const askRes = await fetch('/api/ask-receive?clientId=' + id + '&name=' + encodeURIComponent(f.name) + '&size=' + f.size, { method: 'POST' }).then(r => r.json());
      if (!askRes.accepted) {
        throw new Error(askRes.error || 'Rejected by host PC');
      }
      
      // 2. Perform upload
      document.getElementById('overlayTitle').textContent = 'Uploading...';
      await upload(f, askRes.id);
      successCount++;
    } catch(err) {
      console.error(err);
      failMessage = err.message || 'Upload failed';
      ov.classList.remove('active');
      break;
    }
    ov.classList.remove('active');
  }

  if (successCount === files.length) {
    showToast('Sent ' + files.length + ' file(s) to PC');
  } else if (successCount > 0) {
    showToast('Sent ' + successCount + ' of ' + files.length + ' file(s). Error: ' + failMessage);
  } else {
    showToast('Upload failed: ' + failMessage);
  }
  document.getElementById('fileInput').value = '';
}

function setProg(p) {
  const fill = document.getElementById('winProgressBarFill');
  if (fill) fill.style.width = p + '%';
  document.getElementById('progText').textContent = Math.round(p) + '%';
}

function upload(file, uploadId) {
  return new Promise((res,rej)=>{
    const xhr = new XMLHttpRequest();
    const id = getClientId();
    xhr.open('POST','/upload?clientId=' + id + '&id=' + uploadId);
    xhr.setRequestHeader('X-File-Name', encodeURIComponent(file.name));
    
    let lastTime = Date.now();
    let lastLoaded = 0;
    
    xhr.upload.onprogress = e => {
      if (e.lengthComputable) {
        const pct = e.loaded / e.total * 100;
        const now = Date.now();
        const elapsed = (now - lastTime) / 1000;
        
        if (elapsed >= 0.25 || pct >= 99.9) {
          const loadedDiff = e.loaded - lastLoaded;
          const speedMb = elapsed > 0 ? (loadedDiff / elapsed) / 1000000 : 0;
          
          lastTime = now;
          lastLoaded = e.loaded;
          
          setProg(pct);
          
          document.getElementById('winTimeSpeed').textContent = speedMb.toFixed(1) + ' MB/s';
          document.getElementById('winProgressDetails').textContent = fmt(e.loaded) + ' of ' + fmt(e.total);
          drawGraph(speedMb);
          
          if (pct >= 99.9) {
            document.getElementById('overlayTitle').textContent = 'Finishing...';
          }
        }
      }
    };
    xhr.onload = () => {
      if (xhr.status >= 200 && xhr.status < 300) {
        try {
          const resp = JSON.parse(xhr.responseText);
          if (resp.success) {
            res(resp);
          } else {
            rej(new Error(resp.error || 'Upload failed'));
          }
        } catch (e) {
          res();
        }
      } else {
        try {
          const resp = JSON.parse(xhr.responseText);
          rej(new Error(resp.error || 'Upload failed'));
        } catch (e) {
          rej(new Error('Upload failed with status ' + xhr.status));
        }
      }
    };
    xhr.onerror = () => rej(new Error('Network error'));
    xhr.send(file);
  });
}

function showToast(msg) {
  const t = document.getElementById('toast');
  t.textContent = msg;
  t.classList.add('show');
  setTimeout(() => t.classList.remove('show'), 3000);
}

function fmt(b){
  if(b<1024)return b+' B';
  if(b<1048576)return (b/1024).toFixed(1)+' KB';
  return (b/1048576).toFixed(1)+' MB';
}

function updateThemeButton() {
  const current = document.documentElement.getAttribute('data-theme');
  toggleBtn.innerHTML = current === 'light' ? moonIcon : sunIcon;
}

const dz = document.getElementById('dropZone');
dz.addEventListener('dragover',e=>{e.preventDefault();dz.classList.add('drag');});
dz.addEventListener('dragleave',()=>dz.classList.remove('drag'));
dz.addEventListener('drop',e=>{e.preventDefault();dz.classList.remove('drag');handleFiles(e.dataTransfer.files);});

const toggleBtn = document.getElementById('themeToggle');
toggleBtn.addEventListener('click', () => {
  const current = document.documentElement.getAttribute('data-theme');
  if (current === 'light') {
    document.documentElement.removeAttribute('data-theme');
  } else {
    document.documentElement.setAttribute('data-theme', 'light');
  }
  updateThemeButton();
});

// Auto detect system theme
if (window.matchMedia && window.matchMedia('(prefers-color-scheme: light)').matches) {
  document.documentElement.setAttribute('data-theme', 'light');
}

window.addEventListener('load', () => {
  init();
});
</script>
</body>
</html>";
    }
}
