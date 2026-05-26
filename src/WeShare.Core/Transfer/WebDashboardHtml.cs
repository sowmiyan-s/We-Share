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
<link href='https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@400;500;600;700;800&display=swap' rel='stylesheet'>
<style>
:root {
  --bg: #0b0f17;
  --panel: rgba(17, 24, 39, 0.75);
  --border: rgba(255, 255, 255, 0.08);
  --primary: #6366f1;
  --primary-gradient: linear-gradient(135deg, #6366f1 0%, #a855f7 100%);
  --primary-glow: rgba(99, 102, 241, 0.25);
  --text: #f3f4f6;
  --text-dim: #9ca3af;
  --card: rgba(255, 255, 255, 0.03);
  --input-bg: rgba(255, 255, 255, 0.05);
}
html[data-theme='light'] {
  --bg: #f8fafc;
  --panel: rgba(255, 255, 255, 0.85);
  --border: rgba(15, 23, 42, 0.08);
  --primary: #4f46e5;
  --primary-gradient: linear-gradient(135deg, #4f46e5 0%, #8b5cf6 100%);
  --primary-glow: rgba(79, 70, 229, 0.15);
  --text: #0f172a;
  --text-dim: #64748b;
  --card: rgba(15, 23, 42, 0.03);
  --input-bg: rgba(15, 23, 42, 0.05);
}
* { box-sizing: border-box; margin: 0; padding: 0; -webkit-tap-highlight-color: transparent; }
body {
  background: var(--bg);
  color: var(--text);
  font-family: 'Plus Jakarta Sans', sans-serif;
  line-height: 1.5;
  overflow-x: hidden;
  transition: background 0.3s ease, color 0.3s ease;
}
.container {
  max-width: 600px;
  margin: 0 auto;
  padding: 30px 20px;
  display: flex;
  flex-direction: column;
  gap: 30px;
}
/* -- Header -- */
header {
  background: var(--panel);
  border: 1px solid var(--border);
  border-radius: 24px;
  padding: 24px;
  backdrop-filter: blur(20px);
  -webkit-backdrop-filter: blur(20px);
  display: flex;
  flex-direction: column;
  gap: 16px;
  box-shadow: 0 10px 30px rgba(0,0,0,0.15);
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
  background: var(--primary-gradient);
  border-radius: 14px;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 8px 20px var(--primary-glow);
}
.logo svg { width: 24px; height: 24px; fill: white; }
.brand-container { flex: 1; }
.brand { font-size: 18px; font-weight: 800; letter-spacing: 2px; }
.host-status { font-size: 11px; color: var(--text-dim); font-weight: 600; text-transform: uppercase; letter-spacing: 1px; }

/* -- Profile Settings in Header -- */
.profile-box {
  display: flex;
  align-items: center;
  gap: 12px;
  background: var(--card);
  border: 1px solid var(--border);
  padding: 12px 16px;
  border-radius: 16px;
}
.profile-label { font-size: 11px; font-weight: 700; color: var(--text-dim); }
.profile-input {
  flex: 1;
  background: var(--input-bg);
  border: 1px solid var(--border);
  color: var(--text);
  padding: 8px 12px;
  border-radius: 10px;
  font-family: inherit;
  font-size: 13px;
  font-weight: 600;
  outline: none;
  transition: border-color 0.2s;
}
.profile-input:focus { border-color: var(--primary); }

/* -- Section -- */
section {
  background: var(--panel);
  border: 1px solid var(--border);
  border-radius: 24px;
  padding: 24px;
  backdrop-filter: blur(20px);
  -webkit-backdrop-filter: blur(20px);
  box-shadow: 0 10px 30px rgba(0,0,0,0.1);
  display: flex;
  flex-direction: column;
}
.section-title {
  font-size: 11px;
  font-weight: 800;
  color: var(--text-dim);
  text-transform: uppercase;
  letter-spacing: 2px;
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
  border: 2px dashed var(--border);
  border-radius: 20px;
  padding: 50px 20px;
  text-align: center;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  cursor: pointer;
}
.drop-zone:hover {
  border-color: var(--primary);
  background: var(--primary-glow);
  transform: translateY(-2px);
}
.drop-zone.drag {
  border-color: var(--primary);
  background: var(--primary-glow);
  transform: scale(0.98);
}
.drop-zone input {
  position: absolute;
  inset: 0;
  opacity: 0;
  cursor: pointer;
}
.dz-icon {
  font-size: 44px;
  margin-bottom: 16px;
  display: inline-block;
  transition: transform 0.3s ease;
}
.drop-zone:hover .dz-icon { transform: translateY(-5px) scale(1.1); }
.dz-text { font-size: 16px; font-weight: 700; margin-bottom: 6px; }
.dz-sub { font-size: 12px; color: var(--text-dim); }

/* -- File Cards -- */
.file-list { display: flex; flex-direction: column; gap: 10px; }
.file-card {
  background: var(--card);
  border: 1px solid var(--border);
  border-radius: 16px;
  padding: 14px 16px;
  display: flex;
  align-items: center;
  gap: 14px;
  text-decoration: none;
  color: inherit;
  transition: all 0.2s ease;
}
.file-card:hover {
  border-color: var(--primary);
  background: var(--primary-glow);
  transform: translateX(4px);
}
.f-icon {
  width: 44px;
  height: 44px;
  background: var(--input-bg);
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
}
.f-info { flex: 1; min-width: 0; }
.f-name { font-size: 13px; font-weight: 600; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.f-size { font-size: 11px; color: var(--text-dim); font-weight: 500; margin-top: 2px; }
.f-btn {
  background: var(--primary-gradient);
  color: white;
  border: none;
  padding: 8px 16px;
  border-radius: 10px;
  font-size: 11px;
  font-weight: 700;
  box-shadow: 0 4px 10px var(--primary-glow);
  cursor: pointer;
  transition: transform 0.15s ease;
}
.file-card:hover .f-btn { transform: scale(1.05); }

/* -- Progress Overlay -- */
.overlay {
  position: fixed;
  inset: 0;
  background: rgba(11, 15, 23, 0.85);
  backdrop-filter: blur(10px);
  -webkit-backdrop-filter: blur(10px);
  z-index: 1000;
  display: none;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 40px;
  text-align: center;
}
.overlay.active { display: flex; }
.progress-container {
  width: 160px;
  height: 160px;
  position: relative;
  margin-bottom: 24px;
}
.progress-svg { transform: rotate(-90deg); }
.prog-bg { fill: none; stroke: var(--border); stroke-width: 6; }
.prog-fill {
  fill: none;
  stroke: url(#grad);
  stroke-width: 6;
  stroke-linecap: round;
  stroke-dasharray: 502;
  stroke-dashoffset: 502;
  transition: stroke-dashoffset 0.1s linear;
}
.prog-text {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 28px;
  font-weight: 800;
}
.ov-file { font-size: 13px; color: var(--text-dim); margin-top: 12px; max-width: 250px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; font-weight: 600; }

/* -- Toast -- */
#toast {
  position: fixed;
  bottom: 24px;
  left: 50%;
  transform: translateX(-50%) translateY(100px);
  background: var(--primary-gradient);
  color: white;
  padding: 12px 24px;
  border-radius: 50px;
  font-size: 13px;
  font-weight: 700;
  box-shadow: 0 10px 25px var(--primary-glow);
  transition: transform 0.4s cubic-bezier(0.175, 0.885, 0.32, 1.275);
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
  border-radius: 12px;
  width: 40px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  color: var(--text);
  font-size: 16px;
  transition: all 0.2s;
}
#themeToggle:hover { border-color: var(--primary); background: var(--primary-glow); }

/* -- Accept Dialog Modal -- */
.dialog-overlay {
  position: fixed;
  top: 0; left: 0; right: 0; bottom: 0;
  background: rgba(11, 15, 23, 0.85);
  backdrop-filter: blur(10px);
  -webkit-backdrop-filter: blur(10px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 2000;
  opacity: 0;
  pointer-events: none;
  transition: opacity 0.3s ease;
}
.dialog-overlay.active {
  opacity: 1;
  pointer-events: auto;
}
.dialog-box {
  background: var(--panel);
  border: 1px solid var(--border);
  border-radius: 24px;
  padding: 32px 24px;
  width: 90%;
  max-width: 360px;
  text-align: center;
  box-shadow: 0 20px 50px rgba(0,0,0,0.3);
  transform: scale(0.9);
  transition: transform 0.3s cubic-bezier(0.34, 1.56, 0.64, 1);
}
.dialog-overlay.active .dialog-box {
  transform: scale(1);
}
.dialog-icon {
  font-size: 40px;
  margin-bottom: 16px;
  animation: bounce 2s infinite;
}
@keyframes bounce {
  0%, 100% { transform: translateY(0); }
  50% { transform: translateY(-8px); }
}
.dialog-title {
  font-size: 18px;
  font-weight: 800;
  margin-bottom: 8px;
  letter-spacing: 1px;
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
  border-radius: 12px;
  font-family: inherit;
  font-size: 13px;
  font-weight: 700;
  cursor: pointer;
  border: none;
  transition: all 0.2s;
}
.btn-decline {
  background: rgba(239, 68, 68, 0.1);
  color: #ef4444;
  border: 1px solid rgba(239, 68, 68, 0.2) !important;
}
.btn-decline:hover {
  background: rgba(239, 68, 68, 0.2);
}
.btn-accept {
  background: var(--primary-gradient);
  color: white;
  box-shadow: 0 6px 15px var(--primary-glow);
}
.btn-accept:hover {
  transform: translateY(-2px);
  box-shadow: 0 10px 20px var(--primary-glow);
}
</style>
</head>
<body>

<div class='container'>
  <header>
    <button id='themeToggle'>☀️</button>
    <div class='header-top'>
      <div class='logo' style='padding: 6px;'>
        <img src='/api/logo' style='width: 100%; height: 100%; object-fit: contain;' alt='Logo'/>
      </div>
      <div class='brand-container'>
        <div class='brand'>WE SHARE</div>
        <div id='hostLine' class='host-status'>CONNECTING...</div>
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
      <span class='dz-icon'>🚀</span>
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
  <div class='progress-container'>
    <svg class='progress-svg' width='160' height='160' viewBox='0 0 160 160'>
      <defs>
        <linearGradient id='grad' x1='0%' y1='0%' x2='100%' y2='100%'>
          <stop offset='0%' stop-color='#6366f1' />
          <stop offset='100%' stop-color='#a855f7' />
        </linearGradient>
      </defs>
      <circle class='prog-bg' cx='80' cy='80' r='70'/>
      <circle class='prog-fill' id='progFill' cx='80' cy='80' r='70'/>
    </svg>
    <div class='prog-text' id='progText'>0%</div>
  </div>
  <div class='brand' id='overlayTitle' style='font-size: 12px; letter-spacing: 2px; color: var(--primary); font-weight: 800;'>UPLOADING</div>
  <div class='ov-file' id='ovFile'>filename.ext</div>
</div>

<div id='toast'>Files sent successfully!</div>

<div class='dialog-overlay' id='acceptDialogOverlay'>
  <div class='dialog-box'>
    <div class='dialog-icon'>📥</div>
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

function getClientId() {
  let id = localStorage.getItem('weshare_client_id');
  if (!id) {
    id = 'wc_' + Math.random().toString(36).substring(2, 11) + '_' + Date.now().toString(36);
    localStorage.setItem('weshare_client_id', id);
  }
  return id;
}

function getClientName() {
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
}

function initName() {
  const input = document.getElementById('nameInput');
  input.value = getClientName();
  input.addEventListener('change', () => {
    let name = input.value.trim();
    if (!name) name = 'Web Client';
    localStorage.setItem('weshare_client_name', name);
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
    xhr.open('GET', '/download?id=' + id);
    xhr.responseType = 'blob';
    
    xhr.onprogress = e => {
      if (e.lengthComputable) {
        setProg(e.loaded / e.total * 100);
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

function connectSSE() {
  if (sse) {
    sse.close();
  }
  const id = getClientId();
  const name = getClientName();
  sse = new EventSource('/api/events?clientId=' + id + '&name=' + encodeURIComponent(name));
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

async function init() {
  initName();
  connectSSE();
  
  try {
    const me = await fetch('/api/me').then(r=>r.json());
    document.getElementById('hostLine').textContent = 'CONNECTED TO ' + (me.name).toUpperCase();
  } catch(_){
    document.getElementById('hostLine').textContent = 'CONNECTED TO PC';
  }
  
  loadFiles();
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
        const emoji = getFileEmoji(f.name);
        return `
        <div class='file-card'>
          <div class='f-icon'>${emoji}</div>
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

function getFileEmoji(name) {
  const ext = name.split('.').pop().toLowerCase();
  if (['png', 'jpg', 'jpeg', 'gif', 'webp', 'svg'].includes(ext)) return '🖼️';
  if (['mp4', 'mkv', 'avi', 'mov', 'webm'].includes(ext)) return '🎥';
  if (['mp3', 'wav', 'flac', 'ogg', 'm4a'].includes(ext)) return '🎵';
  if (['pdf', 'doc', 'docx', 'xls', 'xlsx', 'ppt', 'pptx', 'txt'].includes(ext)) return '📄';
  if (['zip', 'rar', 'tar', 'gz', '7z'].includes(ext)) return '📦';
  return '📁';
}

async function handleFiles(files) {
  if (!files.length) return;
  const ov = document.getElementById('overlay');
  document.getElementById('overlayTitle').textContent = 'UPLOADING';
  ov.classList.add('active');
  for (const f of files) {
    document.getElementById('ovFile').textContent = f.name;
    setProg(0);
    await upload(f);
  }
  ov.classList.remove('active');
  showToast('Sent ' + files.length + ' file(s) to PC');
  document.getElementById('fileInput').value = '';
}

function setProg(p) {
  document.getElementById('progFill').style.strokeDashoffset = CIRC - (p/100)*CIRC;
  document.getElementById('progText').textContent = Math.round(p) + '%';
}

function upload(file) {
  return new Promise((res,rej)=>{
    const xhr = new XMLHttpRequest();
    const id = getClientId();
    xhr.open('POST','/upload?clientId=' + id);
    xhr.setRequestHeader('X-File-Name', encodeURIComponent(file.name));
    xhr.upload.onprogress = e => {
      if (e.lengthComputable) setProg(e.loaded/e.total*100);
    };
    xhr.onload=res; xhr.onerror=rej; xhr.send(file);
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

const dz = document.getElementById('dropZone');
dz.addEventListener('dragover',e=>{e.preventDefault();dz.classList.add('drag');});
dz.addEventListener('dragleave',()=>dz.classList.remove('drag'));
dz.addEventListener('drop',e=>{e.preventDefault();dz.classList.remove('drag');handleFiles(e.dataTransfer.files);});

const toggleBtn = document.getElementById('themeToggle');
toggleBtn.addEventListener('click', () => {
  const current = document.documentElement.getAttribute('data-theme');
  if (current === 'light') {
    document.documentElement.removeAttribute('data-theme');
    toggleBtn.textContent = '☀️';
  } else {
    document.documentElement.setAttribute('data-theme', 'light');
    toggleBtn.textContent = '🌙';
  }
});

// Auto detect system theme
if (window.matchMedia && window.matchMedia('(prefers-color-scheme: light)').matches) {
  document.documentElement.setAttribute('data-theme', 'light');
  toggleBtn.textContent = '🌙';
}

document.getElementById('btnAccept').addEventListener('click', async () => {
  if (currentOffer) {
    const offer = currentOffer;
    document.getElementById('acceptDialogOverlay').classList.remove('active');
    isShowingOffer = false;
    currentOffer = null;

    const ov = document.getElementById('overlay');
    document.getElementById('overlayTitle').textContent = 'DOWNLOADING';
    document.getElementById('ovFile').textContent = offer.name;
    setProg(0);
    ov.classList.add('active');

    try {
      await downloadFile(offer.id, offer.name);
      showToast('Downloaded ' + offer.name);
    } catch (ex) {
      console.error(ex);
      showToast('Download failed');
    } finally {
      ov.classList.remove('active');
      loadFiles();
      processNextOffer();
    }
  }
});

document.getElementById('btnDecline').addEventListener('click', async () => {
  if (currentOffer) {
    const offer = currentOffer;
    const id = getClientId();
    document.getElementById('acceptDialogOverlay').classList.remove('active');
    isShowingOffer = false;
    currentOffer = null;

    try {
      await fetch('/api/decline?clientId=' + id + '&id=' + offer.id, { method: 'POST' });
    } catch(_) {}
    
    loadFiles();
    processNextOffer();
  }
});

init();
</script>
</body>
</html>";
    }
}
