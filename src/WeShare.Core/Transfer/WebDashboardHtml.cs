namespace WeShare.Core.Transfer
{
    public partial class WebDashboardService
    {
        private string GetDashboardHtml() => @"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<meta name='viewport' content='width=device-width,initial-scale=1,maximum-scale=1,user-scalable=no'>
<title>We Share | Portal</title>
<link href='https://fonts.googleapis.com/css2?family=Inter:wght@400;600;800&family=Sora:wght@700&display=swap' rel='stylesheet'>
<style>
:root {
  --bg: #13131b;
  --card: rgba(255, 255, 255, 0.05);
  --border: rgba(255, 255, 255, 0.1);
  --primary: #6366f1;
  --primary-dim: #4f46e5;
  --text: #ffffff;
  --text-dim: #908fa0;
}
html[data-theme='light'] {
  --bg: #f8fafc;
  --card: rgba(15, 23, 42, 0.04);
  --border: rgba(15, 23, 42, 0.08);
  --primary: #4f46e5;
  --primary-dim: #4338ca;
  --text: #0f172a;
  --text-dim: #64748b;
}
* { box-sizing: border-box; margin: 0; padding: 0; -webkit-tap-highlight-color: transparent; }
body {
  background: var(--bg);
  color: var(--text);
  font-family: 'Inter', sans-serif;
  line-height: 1.6;
  overflow-x: hidden;
}
h1, h2, h3 { font-family: 'Sora', sans-serif; }

/* -- Layout -- */
.container {
  max-width: 500px;
  margin: 0 auto;
  padding: 40px 20px;
  display: flex;
  flex-direction: column;
  gap: 40px;
}

/* -- Header -- */
header {
  text-align: center;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
}
.logo {
  width: 56px;
  height: 56px;
  background: var(--primary);
  border-radius: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 8px 24px rgba(99, 102, 241, 0.3);
}
.logo svg { width: 28px; height: 28px; fill: white; }
.brand { font-size: 20px; font-weight: 800; letter-spacing: 2px; }

/* -- Sections -- */
.section-title {
  font-size: 11px;
  font-weight: 800;
  color: var(--text-dim);
  text-transform: uppercase;
  letter-spacing: 3px;
  margin-bottom: 20px;
  display: flex;
  align-items: center;
  gap: 15px;
}
.section-title::after { content: ''; flex: 1; height: 1px; background: var(--border); }

/* -- Upload Zone -- */
.drop-zone {
  position: relative;
  background: var(--card);
  border: 1px dashed var(--border);
  border-radius: 24px;
  padding: 60px 20px;
  text-align: center;
  transition: all 0.3s ease;
  overflow: hidden;
}
.drop-zone:active, .drop-zone.drag {
  border-color: var(--primary);
  background: rgba(99, 102, 241, 0.05);
  transform: scale(0.98);
}
.drop-zone input {
  position: absolute;
  inset: 0;
  opacity: 0;
  cursor: pointer;
}
.dz-icon { font-size: 40px; margin-bottom: 20px; display: block; filter: grayscale(1); }
.dz-text { font-size: 18px; font-weight: 700; margin-bottom: 8px; }
.dz-sub { font-size: 13px; color: var(--text-dim); }

/* -- File Cards -- */
.file-list { display: flex; flex-direction: column; gap: 12px; }
.file-card {
  background: var(--card);
  border: 1px solid var(--border);
  border-radius: 16px;
  padding: 16px;
  display: flex;
  align-items: center;
  gap: 16px;
  text-decoration: none;
  color: inherit;
  transition: background 0.2s;
}
.file-card:active { background: rgba(255, 255, 255, 0.1); }
.f-icon {
  width: 44px;
  height: 44px;
  background: rgba(255,255,255,0.05);
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
}
.f-icon svg { width: 20px; height: 20px; fill: var(--primary); }
.f-info { flex: 1; min-width: 0; }
.f-name { font-size: 14px; font-weight: 600; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.f-size { font-size: 11px; color: var(--text-dim); }
.f-btn {
  background: var(--primary);
  color: white;
  border: none;
  padding: 8px 16px;
  border-radius: 8px;
  font-size: 11px;
  font-weight: 700;
}

/* -- Overlay -- */
.overlay {
  position: fixed;
  inset: 0;
  background: var(--bg);
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
  width: 200px;
  height: 200px;
  position: relative;
  margin-bottom: 30px;
}
.progress-svg { transform: rotate(-90deg); }
.prog-bg { fill: none; stroke: var(--border); stroke-width: 4; }
.prog-fill {
  fill: none;
  stroke: var(--primary);
  stroke-width: 4;
  stroke-linecap: round;
  stroke-dasharray: 565;
  stroke-dashoffset: 565;
  transition: stroke-dashoffset 0.1s linear;
}
.prog-text {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 32px;
  font-weight: 800;
  font-family: 'Sora', sans-serif;
}
.ov-file { font-size: 14px; color: var(--text-dim); margin-top: 10px; max-width: 100%; overflow: hidden; text-overflow: ellipsis; }

/* -- Toast -- */
#toast {
  position: fixed;
  bottom: 30px;
  left: 50%;
  transform: translateX(-50%) translateY(100px);
  background: var(--primary);
  padding: 14px 28px;
  border-radius: 50px;
  font-size: 13px;
  font-weight: 700;
  box-shadow: 0 10px 30px rgba(0,0,0,0.5);
  transition: transform 0.4s cubic-bezier(0.175, 0.885, 0.32, 1.275);
  z-index: 1100;
}
#toast.show { transform: translateX(-50%) translateY(0); }

</style>
</head>
<body>

<div class='container'>
  <header>
    <div class='logo'>
      <svg viewBox='0 0 24 24'><path d='M12 2L4.5 20.29L5.21 21L12 18L18.79 21L19.5 20.29L12 2Z'/></svg>
    </div>
    <div class='brand'>WE SHARE</div>
    <div id='hostLine' style='font-size: 11px; color: var(--text-dim); letter-spacing: 1px;'>READY</div>
  </header>

  <section id='uploadSection'>
    <div class='section-title'>Send to PC</div>
    <div class='drop-zone' id='dropZone'>
      <input type='file' id='fileInput' multiple onchange='handleFiles(this.files)'>
      <span class='dz-icon'>🚀</span>
      <div class='dz-text'>Tap to Upload</div>
      <div class='dz-sub'>Files will be sent to the host PC</div>
    </div>
  </section>

  <section id='filesSection' style='display:none'>
    <div class='section-title'>Files from PC</div>
    <div class='file-list' id='fileList'></div>
  </section>

  <section id='shareSection'>
    <div class='section-title'>Share Portal</div>
    <div class='file-card' style='display: flex; flex-direction: column; align-items: center; text-align: center; gap: 16px; padding: 24px;'>
      <img src='/api/qr' style='width: 140px; height: 140px; border-radius: 12px; border: 1px solid var(--border); background: white; padding: 8px;' alt='Portal QR Code'/>
      <div>
        <div style='font-weight: 700; font-size: 14px;'>Scan to connect</div>
        <div style='font-size: 12px; color: var(--text-dim); margin-top: 4px;'>Let others scan this QR to open the portal instantly</div>
      </div>
    </div>
  </section>
</div>
<button id='themeToggle' style='position: absolute; top: 20px; right: 20px; background: var(--card); border: 1px solid var(--border); border-radius: 50%; width: 40px; height: 40px; display: flex; align-items: center; justify-content: center; cursor: pointer; color: var(--text); font-size: 18px; z-index: 2000;'>☀️</button>

<div class='overlay' id='overlay'>
  <div class='progress-container'>
    <svg class='progress-svg' width='200' height='200' viewBox='0 0 200 200'>
      <circle class='prog-bg' cx='100' cy='100' r='90'/>
      <circle class='prog-fill' id='progFill' cx='100' cy='100' r='90'/>
    </svg>
    <div class='prog-text' id='progText'>0%</div>
  </div>
  <div class='brand' style='font-size: 14px;'>UPLOADING</div>
  <div class='ov-file' id='ovFile'>filename.ext</div>
</div>

<div id='toast'>Files sent successfully!</div>

<script>
const CIRC = 2 * Math.PI * 90;

async function init() {
  try {
    const me = await fetch('/api/me').then(r=>r.json());
    document.getElementById('hostLine').textContent = 'CONNECTED TO ' + (me.name).toUpperCase();
  } catch(_){}
  
  loadFiles();
  const es = new EventSource('/api/events');
  es.onmessage = e => { if (e.data === 'refresh') loadFiles(); };
}

async function loadFiles() {
  try {
    const files = await fetch('/api/files').then(r=>r.json());
    const sec = document.getElementById('filesSection');
    const list = document.getElementById('fileList');
    if (files && files.length) {
      sec.style.display = 'block';
      list.innerHTML = files.map(f => `
        <a class='file-card' href='/download?id=${f.id}' download='${f.name}'>
          <div class='f-icon'>
            <svg viewBox='0 0 24 24'><path d='M14 2H6c-1.1 0-1.99.9-1.99 2L4 20c0 1.1.89 2 1.99 2H18c1.1 0 2-.9 2-2V8l-6-6zM6 20V4h7v5h5v11H6z'/></svg>
          </div>
          <div class='f-info'>
            <div class='f-name'>${f.name}</div>
            <div class='f-size'>${fmt(f.size)}</div>
          </div>
          <button class='f-btn'>GET</button>
        </a>
      `).join('');
    } else { sec.style.display = 'none'; }
  } catch(_){}
}

async function handleFiles(files) {
  if (!files.length) return;
  const ov = document.getElementById('overlay');
  ov.classList.add('active');
  for (const f of files) {
    document.getElementById('ovFile').textContent = f.name;
    setProg(0);
    await upload(f);
  }
  ov.classList.remove('active');
  showToast('Sent ' + files.length + ' file(s)');
  document.getElementById('fileInput').value = '';
}

function setProg(p) {
  document.getElementById('progFill').style.strokeDashoffset = CIRC - (p/100)*CIRC;
  document.getElementById('progText').textContent = Math.round(p) + '%';
}

function upload(file) {
  return new Promise((res,rej)=>{
    const xhr = new XMLHttpRequest();
    xhr.open('POST','/upload');
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

init();
</script>
</body>
</html>";
    }
}
