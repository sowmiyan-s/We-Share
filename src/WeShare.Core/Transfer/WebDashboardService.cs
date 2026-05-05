using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WeShare.Core.Models;

namespace WeShare.Core.Transfer
{
    /// <summary>
    /// Lightweight raw-TCP HTTP server — no HttpListener, no admin rights needed.
    /// Exposes:
    ///   GET  /             → Mobile dashboard HTML
    ///   GET  /api/devices  → JSON list of discovered peers
    ///   GET  /api/me       → JSON { name, ip }
    ///   POST /upload       → Stream a file; Header: X-File-Name
    /// </summary>
    public class WebDashboardService
    {
        private readonly string _saveDirectory;
        private readonly DeviceModel _localDevice;
        private Func<IReadOnlyList<DeviceModel>>? _getPeers;
        private TcpListener? _listener;
        private CancellationTokenSource? _cts;
        private readonly List<SharedFile> _sharedFiles = new();
        private readonly SemaphoreSlim _filesLock = new(1, 1);
        public int Port { get; } = 8080;

        public class SharedFile
        {
            public string Id { get; set; } = Guid.NewGuid().ToString("n");
            public string Name { get; set; } = "";
            public string Path { get; set; } = "";
            public long Size { get; set; }
        }

        public WebDashboardService(string saveDirectory, DeviceModel localDevice)
        {
            _saveDirectory = saveDirectory;
            _localDevice = localDevice;
        }

        /// <summary>Supply a live snapshot of discovered devices.</summary>
        public void SetPeersProvider(Func<IReadOnlyList<DeviceModel>> provider) => _getPeers = provider;

        public void ShareForWeb(string filePath)
        {
            var info = new FileInfo(filePath);
            if (!info.Exists) return;
            _filesLock.Wait();
            try
            {
                if (_sharedFiles.Any(f => f.Path == filePath)) return;
                _sharedFiles.Add(new SharedFile { Name = info.Name, Path = filePath, Size = info.Length });
            }
            finally { _filesLock.Release(); }
        }

        public void ClearSharedFiles() 
        { 
            _filesLock.Wait();
            try { _sharedFiles.Clear(); }
            finally { _filesLock.Release(); }
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            try
            {
                _listener = new TcpListener(IPAddress.Any, Port);
                _listener.Start();
                _ = Task.Run(() => AcceptLoop(_cts.Token));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WebDashboard] Failed to start: {ex.Message}");
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            _listener?.Stop();
        }

        // ── Accept loop ──────────────────────────────────────────────────────
        private async Task AcceptLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener!.AcceptTcpClientAsync(token);
                    _ = Task.Run(() => HandleClient(client), token);
                }
                catch { break; }
            }
        }

        // ── Per-connection handler ────────────────────────────────────────────
        private async Task HandleClient(TcpClient client)
        {
            using (client)
            {
                try
                {
                    var stream = client.GetStream();
                    var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);

                    // --- Parse request line ---
                    string? requestLine = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(requestLine)) return;

                    var parts = requestLine.Split(' ');
                    if (parts.Length < 2) return;

                    string method = parts[0].ToUpperInvariant();
                    string path   = parts[1].Split('?')[0];

                    // --- Read headers ---
                    var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    string? headerLine;
                    while (!string.IsNullOrEmpty(headerLine = await reader.ReadLineAsync()))
                    {
                        var idx = headerLine.IndexOf(':');
                        if (idx > 0)
                            headers[headerLine[..idx].Trim()] = headerLine[(idx + 1)..].Trim();
                    }

                    // --- CORS + route dispatch ---
                    if (method == "GET" && path == "/")
                        await SendResponse(stream, 200, "text/html; charset=utf-8", GetDashboardHtml());

                    else if (method == "GET" && path == "/api/me")
                        await SendJson(stream, new { name = _localDevice.Name, ip = _localDevice.IpAddress });

                    else if (method == "GET" && path == "/api/devices")
                    {
                        var peers = _getPeers?.Invoke() ?? Array.Empty<DeviceModel>();
                        await SendJson(stream, peers);
                    }

                    else if (method == "GET" && path == "/api/files")
                    {
                        await _filesLock.WaitAsync();
                        try { await SendJson(stream, _sharedFiles); }
                        finally { _filesLock.Release(); }
                    }

                    else if (method == "GET" && path == "/download")
                    {
                        // Parse query string for id
                        var query = parts[1].Contains('?') ? parts[1].Split('?')[1] : "";
                        var id = query.Split('&').FirstOrDefault(p => p.StartsWith("id="))?.Split('=')[1];
                        
                        SharedFile? file = null;
                        await _filesLock.WaitAsync();
                        try { file = _sharedFiles.FirstOrDefault(f => f.Id == id); }
                        finally { _filesLock.Release(); }

                        if (file != null && File.Exists(file.Path))
                        {
                            var info = new FileInfo(file.Path);
                            var header = $"HTTP/1.1 200 OK\r\n" +
                                         $"Content-Type: application/octet-stream\r\n" +
                                         $"Content-Disposition: attachment; filename=\"{Uri.EscapeDataString(file.Name)}\"\r\n" +
                                         $"Content-Length: {info.Length}\r\n" +
                                         "Connection: close\r\n" +
                                         "\r\n";
                            await stream.WriteAsync(Encoding.ASCII.GetBytes(header));
                            using var fs = new FileStream(file.Path, FileMode.Open, FileAccess.Read, FileShare.Read);
                            await fs.CopyToAsync(stream);
                        }
                        else
                            await SendResponse(stream, 404, "text/plain", "File Not Found");
                    }

                    else if (method == "POST" && path == "/upload")
                    {
                        string rawName = headers.GetValueOrDefault("X-File-Name", $"upload_{DateTime.Now.Ticks}.dat");
                        string filename = Path.GetFileName(Uri.UnescapeDataString(rawName)); // decode + sanitise
                        long contentLength = long.TryParse(headers.GetValueOrDefault("Content-Length", "0"), out var cl) ? cl : 0;

                        Directory.CreateDirectory(_saveDirectory);
                        string dest = GetUniqueFilePath(_saveDirectory, filename);

                        // The reader has buffered some bytes — switch to raw stream copy
                        using var fs = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None, 65536, true);
                        byte[] buf = new byte[65536];
                        long written = 0;
                        while (written < contentLength)
                        {
                            int toRead = (int)Math.Min(buf.Length, contentLength - written);
                            int n = await stream.ReadAsync(buf.AsMemory(0, toRead));
                            if (n == 0) break;
                            await fs.WriteAsync(buf.AsMemory(0, n));
                            written += n;
                        }

                        await SendJson(stream, new { success = true, saved = Path.GetFileName(dest), bytes = written });
                    }

                    else
                        await SendResponse(stream, 404, "text/plain", "Not Found");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WebDashboard] Handler error: {ex.Message}");
                }
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static async Task SendResponse(Stream stream, int status, string contentType, string body)
        {
            var bodyBytes = Encoding.UTF8.GetBytes(body);
            var header = $"HTTP/1.1 {status} OK\r\n" +
                         $"Content-Type: {contentType}\r\n" +
                         $"Content-Length: {bodyBytes.Length}\r\n" +
                         "Connection: close\r\n" +
                         "Access-Control-Allow-Origin: *\r\n" +
                         "\r\n";
            await stream.WriteAsync(Encoding.ASCII.GetBytes(header));
            await stream.WriteAsync(bodyBytes);
        }

        private static Task SendJson(Stream stream, object obj)
            => SendResponse(stream, 200, "application/json",
                            JsonSerializer.Serialize(obj, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));

        private static string GetUniqueFilePath(string dir, string filename)
        {
            string dest = Path.Combine(dir, filename);
            if (!File.Exists(dest)) return dest;
            string name = Path.GetFileNameWithoutExtension(filename);
            string ext  = Path.GetExtension(filename);
            int i = 1;
            while (File.Exists(dest))
                dest = Path.Combine(dir, $"{name} ({i++}){ext}");
            return dest;
        }

        // ── Mobile Dashboard HTML ─────────────────────────────────────────────
        private string GetDashboardHtml() => @"<!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='UTF-8'>
  <meta name='viewport' content='width=device-width, initial-scale=1, maximum-scale=1'>
  <title>We Share — Mobile</title>
  <style>
    :root{--bg:#080E1A;--surface:#0A1628;--card:#111C2D;--border:#1E3A5F;--accent:#38BDF8;--accent2:#6366F1;--text:#E2E8F0;--muted:#4E6278;--green:#22C55E;--red:#EF4444;}
    *{box-sizing:border-box;margin:0;padding:0;-webkit-tap-highlight-color:transparent;}
    body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;background:var(--bg);color:var(--text);min-height:100dvh;display:flex;flex-direction:column;}

    /* Header */
    header{background:var(--surface);border-bottom:1px solid var(--border);padding:16px 20px;display:flex;align-items:center;gap:12px;position:sticky;top:0;z-index:10;}
    .logo{width:36px;height:36px;border-radius:10px;background:linear-gradient(135deg,#0EA5E9,#6366F1);display:flex;align-items:center;justify-content:center;font-size:18px;font-weight:900;color:#fff;flex-shrink:0;}
    .brand h1{font-size:16px;font-weight:800;color:var(--text);}
    .brand p{font-size:11px;color:var(--muted);margin-top:1px;}
    .dot{width:7px;height:7px;border-radius:50%;background:var(--green);margin-left:auto;flex-shrink:0;box-shadow:0 0 6px var(--green);}

    /* Sections */
    main{flex:1;padding:20px 16px;display:flex;flex-direction:column;gap:20px;}
    .section-title{font-size:10px;font-weight:700;color:var(--muted);letter-spacing:1.5px;margin-bottom:10px;}

    /* Device card */
    .devices-grid{display:flex;flex-direction:column;gap:10px;}
    .device-card{background:var(--card);border:1px solid var(--border);border-radius:16px;padding:16px;display:flex;align-items:center;gap:14px;transition:border-color .2s,background .2s;cursor:pointer;}
    .device-card:active{background:#162030;}
    .device-card.selected{border-color:var(--accent);background:#0D1E33;}
    .device-avatar{width:48px;height:48px;border-radius:14px;background:linear-gradient(135deg,#0EA5E9,#6366F1);display:flex;align-items:center;justify-content:center;font-size:22px;font-weight:900;color:#fff;flex-shrink:0;}
    .device-info{flex:1;min-width:0;}
    .device-name{font-size:15px;font-weight:700;color:var(--text);white-space:nowrap;overflow:hidden;text-overflow:ellipsis;}
    .device-ip{font-size:11px;color:var(--muted);font-family:monospace;margin-top:2px;}
    .device-badge{font-size:10px;font-weight:700;color:var(--accent);background:#0F2744;padding:3px 8px;border-radius:20px;white-space:nowrap;margin-left:auto;flex-shrink:0;}
    .empty{text-align:center;padding:40px 20px;color:var(--muted);}
    .empty .icon{font-size:48px;display:block;margin-bottom:12px;}
    .empty p{font-size:14px;line-height:1.5;}

    /* Send panel */
    .send-panel{background:var(--card);border:1px solid var(--border);border-radius:16px;padding:16px;}
    .send-target{font-size:12px;color:var(--muted);margin-bottom:14px;}
    .send-target span{color:var(--accent);font-weight:700;}
    .upload-area{border:2px dashed var(--border);border-radius:12px;padding:28px 16px;text-align:center;position:relative;transition:border-color .2s;}
    .upload-area.dragover{border-color:var(--accent);}
    .upload-area input{position:absolute;inset:0;opacity:0;cursor:pointer;width:100%;height:100%;}
    .upload-area .up-icon{font-size:36px;display:block;margin-bottom:8px;}
    .upload-area p{font-size:14px;color:var(--muted);}
    .upload-area strong{color:var(--accent);}

    /* Progress */
    .progress-wrap{margin-top:14px;display:none;}
    .progress-wrap.visible{display:block;}
    .prog-bar{height:6px;background:#0D1929;border-radius:3px;overflow:hidden;margin-top:8px;}
    .prog-fill{height:100%;width:0;background:linear-gradient(90deg,#0EA5E9,#6366F1);border-radius:3px;transition:width .1s;}
    .prog-label{font-size:12px;color:var(--muted);margin-top:6px;display:flex;justify-content:space-between;}

    /* Shared Files List */
    .file-card{background:var(--card);border:1px solid var(--border);border-radius:12px;padding:12px 16px;display:flex;align-items:center;gap:12px;margin-bottom:8px;text-decoration:none;}
    .file-card:active{background:#162030;}
    .file-icon{font-size:20px;}
    .file-info{flex:1;min-width:0;}
    .file-name{font-size:14px;font-weight:600;color:var(--text);white-space:nowrap;overflow:hidden;text-overflow:ellipsis;}
    .file-size{font-size:11px;color:var(--muted);}
    .btn-dl{background:#0F2744;color:var(--accent);padding:6px 12px;border-radius:8px;font-size:11px;font-weight:700;border:none;}

    /* Toast */
    .toast{position:fixed;bottom:24px;left:50%;transform:translateX(-50%) translateY(80px);background:#112134;border:1px solid var(--border);border-radius:12px;padding:12px 20px;font-size:13px;font-weight:600;transition:transform .3s;z-index:100;white-space:nowrap;}
    .toast.show{transform:translateX(-50%) translateY(0);}
    .toast.success{border-color:var(--green);color:var(--green);}
    .toast.error{border-color:var(--red);color:var(--red);}

    /* Refresh btn */
    .btn-refresh{background:transparent;border:1px solid var(--border);color:var(--muted);padding:8px 14px;border-radius:10px;font-size:12px;font-weight:600;cursor:pointer;transition:border-color .2s,color .2s;}
    .btn-refresh:active{border-color:var(--accent);color:var(--accent);}
  </style>
</head>
<body>

<header>
  <div class='logo'>⇄</div>
  <div class='brand'>
    <h1>We Share</h1>
    <p id='pcName'>Connecting…</p>
  </div>
  <div class='dot'></div>
</header>

<main>
  <!-- Devices -->
  <div>
    <div style='display:flex;align-items:center;justify-content:space-between;margin-bottom:10px;'>
      <div class='section-title' style='margin-bottom:0'>NEARBY DEVICES</div>
      <button class='btn-refresh' onclick='loadDevices()'>↻ Refresh</button>
    </div>
    <div class='devices-grid' id='devicesGrid'>
      <div class='empty'><span class='icon'>📡</span><p>Scanning your network…</p></div>
    </div>
  </div>

  <!-- Shared from PC -->
  <div id='sharedSection' style='display:none;'>
    <div class='section-title'>RECEIVE FROM PC</div>
    <div id='filesGrid'></div>
  </div>

  <!-- Send -->
  <div>
    <div class='section-title'>SEND A FILE</div>
    <div class='send-panel'>
      <p class='send-target'>Target: <span id='targetName'>— select a device above —</span></p>
      <div class='upload-area' id='uploadArea'>
        <input type='file' id='fileInput' multiple onchange='handleFiles(this.files)'/>
        <span class='up-icon'>📂</span>
        <p>Tap to choose files<br><strong>or drag &amp; drop</strong></p>
      </div>
      <div class='progress-wrap' id='progressWrap'>
        <div class='prog-bar'><div class='prog-fill' id='progFill'></div></div>
        <div class='prog-label'>
          <span id='progFile'>Uploading…</span>
          <span id='progPct'>0%</span>
        </div>
      </div>
    </div>
  </div>
</main>

<div class='toast' id='toast'></div>

<script>
let selectedDevice = null;

// ── Bootstrap ──────────────────────────────────────────────────────────
async function init(){
  try{
    const r = await fetch('/api/me');
    const me = await r.json();
    document.getElementById('pcName').textContent = 'Connected to ' + me.name;
  }catch(e){}
  loadDevices();
  setInterval(loadDevices, 5000);
}

// ── Load devices ────────────────────────────────────────────────────────
async function loadDevices(){
  try{
    const r = await fetch('/api/devices');
    const devices = await r.json();
    renderDevices(devices);
  }catch(e){ console.log('fetch /api/devices:', e); }
  loadFiles();
}

async function loadFiles(){
  try{
    const r = await fetch('/api/files');
    const files = await r.json();
    const section = document.getElementById('sharedSection');
    const grid = document.getElementById('filesGrid');
    if(files && files.length > 0){
      section.style.display = 'block';
      grid.innerHTML = files.map(f => `
        <a href='/download?id=${f.id}' class='file-card' download='${f.name}'>
          <span class='file-icon'>📄</span>
          <div class='file-info'>
            <div class='file-name'>${esc(f.name)}</div>
            <div class='file-size'>${formatBytes(f.size)}</div>
          </div>
          <button class='btn-dl'>Download</button>
        </a>
      `).join('');
    } else {
      section.style.display = 'none';
    }
  }catch(e){}
}

function formatBytes(b){
  if(b<1024)return b+' B';
  if(b<1048576)return (b/1024).toFixed(1)+' KB';
  return (b/1048576).toFixed(1)+' MB';
}

function renderDevices(list){
  const grid = document.getElementById('devicesGrid');
  if(!list || list.length === 0){
    grid.innerHTML = `<div class='empty'><span class='icon'>📡</span><p>No devices found yet.<br>Make sure other PCs have We Share open.</p></div>`;
    return;
  }
  grid.innerHTML = list.map(d=>`
    <div class='device-card${selectedDevice && selectedDevice.id===d.id?' selected':''}' onclick='selectDevice(${JSON.stringify(JSON.stringify(d))})'>
      <div class='device-avatar'>${d.name ? d.name[0].toUpperCase() : '?'}</div>
      <div class='device-info'>
        <div class='device-name'>${esc(d.name)}</div>
        <div class='device-ip'>${esc(d.ipAddress||'')}</div>
      </div>
      <div class='device-badge'>${esc(d.type||'PC')}</div>
    </div>`).join('');
}

function selectDevice(json){
  selectedDevice = JSON.parse(json);
  document.getElementById('targetName').textContent = selectedDevice.name;
  // re-render to update .selected class
  fetch('/api/devices').then(r=>r.json()).then(renderDevices).catch(()=>{});
}

// ── Upload ──────────────────────────────────────────────────────────────
function handleFiles(files){
  if(!selectedDevice){ toast('Select a device first!','error'); return; }
  uploadQueue([...files]);
}

async function uploadQueue(files){
  const wrap = document.getElementById('progressWrap');
  wrap.classList.add('visible');
  for(const file of files){
    document.getElementById('progFile').textContent = file.name;
    await uploadFile(file);
  }
  wrap.classList.remove('visible');
  document.getElementById('progFill').style.width='0';
  document.getElementById('fileInput').value='';
  toast(`✓ ${files.length} file${files.length>1?'s':''} sent!`,'success');
}

function uploadFile(file){
  return new Promise((resolve,reject)=>{
    const xhr = new XMLHttpRequest();
    xhr.open('POST','/upload',true);
    xhr.setRequestHeader('X-File-Name', encodeURIComponent(file.name));
    xhr.setRequestHeader('Content-Length', file.size);
    xhr.upload.onprogress = e=>{
      if(e.lengthComputable){
        const pct = Math.round(e.loaded/e.total*100);
        document.getElementById('progFill').style.width=pct+'%';
        document.getElementById('progPct').textContent=pct+'%';
      }
    };
    xhr.onload = ()=>{ resolve(); };
    xhr.onerror = ()=>{ toast('Upload failed','error'); reject(); };
    xhr.send(file);
  });
}

// ── Drag & drop ─────────────────────────────────────────────────────────
const area = document.getElementById('uploadArea');
area.addEventListener('dragover',e=>{e.preventDefault();area.classList.add('dragover');});
area.addEventListener('dragleave',()=>area.classList.remove('dragover'));
area.addEventListener('drop',e=>{
  e.preventDefault();area.classList.remove('dragover');
  handleFiles(e.dataTransfer.files);
});

// ── Toast ────────────────────────────────────────────────────────────────
function toast(msg,type='success'){
  const t=document.getElementById('toast');
  t.textContent=msg;t.className='toast '+type+' show';
  setTimeout(()=>t.classList.remove('show'),2800);
}

function esc(s){return String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;');}

init();
</script>
</body>
</html>";
    }
}
