# We Share &mdash; Network Protocol Specification

This document provides the formal wire protocol specification for all network communications in **We Share**, including the **Peer Discovery Protocol (UDP)**, the **Direct Binary Transfer Protocol (TCP)**, the **Universal Web Portal REST & SSE API**, and the **Desert Mode Captive Portal**.

---

## 1. Protocol Architecture & Port Allocations

| Subsystem | Transport | Default Port | Configurable / Dynamic | Purpose |
| :--- | :--- | :--- | :--- | :--- |
| **Peer Discovery** | UDP | `45678` | Fixed | LAN peer announcements and radar heartbeat |
| **Direct Transfer** | TCP | `45679` | Dynamic (fallback to OS-assigned) | High-speed binary file and batch manifest transfers |
| **Universal Web Portal** | TCP (HTTP 1.1) | `8080` | Dynamic hunting (`8080` &ndash; `8099`) | Browser-based mobile uploads, downloads, and SSE |
| **Captive Portal DNS** | UDP | `53` | Fixed (Desert Mode only) | DNS query interception for captive network detection |
| **Captive Portal HTTP** | TCP | `80` | Fixed (Desert Mode only) | HTTP 302 redirection to web dashboard |

---

## 2. Peer Discovery Protocol (UDP `45678`)

### 2.1. Discovery Overview
- **Broadcast Frequency**: Beacons are transmitted every **5.0 seconds** across all active physical network adapters.
- **Heartbeat Expiration**: Peers not heard from within **6.0 seconds** are marked stale and evicted from the discovery registry.
- **Subnet Addressing**: Beacons are broadcast to each adapter's calculated directed broadcast address (e.g. `192.168.1.255`), followed by a global broadcast to `255.255.255.255`.
- **Desert Mode Directed Ping**:
  - Hotspot host (`192.168.137.1`) sends directed unicast beacons to `192.168.137.2` through `192.168.137.15`.
  - Client nodes send directed unicast beacons to `192.168.137.1`.

### 2.2. Beacon Payload (JSON Schema)
The UDP datagram payload is UTF-8 encoded JSON:

```json
{
  "Id": "8f3e2b10a45c49d8a1e8e2b8f0476c3a",
  "Name": "DESKTOP-ALPHA",
  "IpAddress": "192.168.1.105",
  "Port": 45679,
  "Type": "PC",
  "Role": "Sender",
  "IsReceiver": false,
  "LastSeen": "2026-09-22T08:00:00.0000000Z",
  "ConnectionStatus": "Idle"
}
```

#### Field Definitions:
- `Id` (`string`): Unique GUID identifying the node instance.
- `Name` (`string`): Human-readable machine hostname or user alias.
- `IpAddress` (`string`): Sender's active IPv4 address.
- `Port` (`int32`): TCP transfer listening port (usually `45679`).
- `Type` (`string`): Device form factor (`"PC"`, `"Phone"`, `"Tablet"`).
- `Role` (`string`): Current operating mode (`"Sender"`, `"Receiver"`, `"Idle"`).
- `IsReceiver` (`bool`): Flag indicating receiver readiness.

---

## 3. Direct Transfer Protocol (`WESH` over TCP)

### 3.1. Frame Structure
All control and metadata packets transmitted over the TCP transfer socket start with a standard 9-byte header:

```text
 0                   1                   2                   3
 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|   'W' (0x57)  |   'E' (0x45)  |   'S' (0x53)  |   'H' (0x48)  | Magic Header (4 Bytes)
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|  MsgType (1B) |                 Payload Length (4 Bytes)      |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|                        Payload (Length Bytes)                 |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
```

- **Magic Header (4 Bytes)**: `0x57, 0x45, 0x53, 0x48` (ASCII `"WESH"`).
- **Message Type (1 Byte)**: Identifies the operation code.
- **Payload Length (4 Bytes, Little-Endian)**: Length in bytes of the following payload.
- **Payload Data**: JSON text or structured binary content depending on `MsgType`.

### 3.2. Message Types (`MsgType`)

| Opcode | Identifier | Direction | Payload Format | Description |
| :---: | :--- | :--- | :--- | :--- |
| `0x01` | `CONNECT_REQUEST` | Sender &rarr; Receiver | JSON (`ConnectionRequest`) | Initiates pairing session |
| `0x02` | `CONNECT_RESPONSE` | Receiver &rarr; Sender | JSON (`ConnectionResponse`) | Accept/reject pairing response |
| `0x03` | `DISCONNECT` | Either &rarr; Either | Empty | Terminates active pairing session |
| `0x10` | `FILE_MANIFEST` | Sender &rarr; Receiver | JSON (`FileMetadata`) | Initiates single file stream |
| `0x11` | `FILE_RESPONSE` | Receiver &rarr; Sender | JSON (`bool Accepted`) | Receiver consent for single file |
| `0x12` | `BATCH_MANIFEST` | Sender &rarr; Receiver | JSON (`BatchManifest`) | Staged batch list before sending |
| `0x13` | `BATCH_RESPONSE` | Receiver &rarr; Sender | JSON (`BatchResponse`) | List of accepted file IDs |
| `0x14` | `RESEND_REQUEST` | Receiver &rarr; Sender | JSON (`ResendRequest`) | Requests single file re-transmission |
| `0x15` | `RESEND_RESPONSE`| Sender &rarr; Receiver | JSON (`ResendResponse`) | Approves/declines re-send request |

### 3.3. Batch Manifest Schema (`0x12 BATCH_MANIFEST`)
```json
{
  "BatchId": "d5a86a7b3c4e402685936ad1782e4e71",
  "SenderName": "DESKTOP-ALPHA",
  "SenderType": "PC",
  "SenderIp": "192.168.1.105",
  "TotalBytes": 10737418240,
  "Files": [
    {
      "FileId": "c45817db4b49463c9b7405ba49eb7218",
      "FileName": "project_render.mp4",
      "RelativePath": "Videos/project_render.mp4",
      "FileSize": 5368709120,
      "IsFolderItem": false,
      "IsSelected": true
    }
  ]
}
```

### 3.4. Binary Payload Streaming
Once a file transfer is accepted:
1. Sender writes 1 MB chunks (`EnterpriseBufferSize = 1048576` bytes) sequentially into the socket.
2. Receiver reads chunks directly from `NetworkStream` into a local `FileStream` pre-allocated to the file's target size.
3. Live throughput calculation evaluates bytes transferred per elapsed millisecond to compute a smooth moving average.

---

## 4. Universal Web Portal REST & SSE API

The embedded HTTP 1.1 engine listens on port `8080` (or `8081`&ndash;`8099`).

### 4.1. HTTP Endpoints

| Method | Path | Request Body | Response Format | Purpose |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/` | &mdash; | `text/html` | Serves the single-page web dashboard |
| `GET` | `/api/me` | &mdash; | JSON | Returns desktop host name, IP, and role |
| `GET` | `/api/devices` | &mdash; | JSON | Snapshot of active network peers |
| `GET` | `/api/history` | &mdash; | JSON | Last 25 transfer history records |
| `GET` | `/api/qr` | &mdash; | `image/png` | Returns local session pairing QR code PNG |
| `GET` | `/api/events` | &mdash; | `text/event-stream` | SSE push stream for mobile events |
| `POST` | `/api/connect` | JSON / Form | JSON | Mobile device initiates pairing |
| `POST` | `/api/connect-respond` | JSON / Form | JSON | Response to pairing invitation |
| `POST` | `/api/disconnect` | JSON / Form | JSON | Terminates paired session |
| `GET` | `/api/connect-status` | &mdash; | JSON | Queries active pairing status |
| `POST` | `/api/batch-manifest` | JSON (`BatchManifest`) | JSON | Mobile submits batch file list for desktop approval |
| `POST` | `/api/batch-complete` | JSON | JSON | Notifies desktop of batch completion |
| `POST` | `/api/ask-receive` | Query / Form | JSON | Mobile requests desktop permission to upload file |
| `POST` | `/api/upload` | Multipart / Stream | JSON | Streams incoming file upload directly to disk |
| `GET` | `/api/download` | `?id={fileId}` | Binary Stream | Streams desktop file to mobile browser |
| `POST` | `/api/decline` | `?id={fileId}` | Plain Text | Declines an incoming web file offer |
| `POST` | `/api/portal-login` | &mdash; | JSON | Authorizes captive portal login |

### 4.2. Server-Sent Events (SSE) Protocol (`/api/events`)
Clients establish an HTTP GET request with header `Accept: text/event-stream`.

#### SSE Channels:
- `event: connect-request`: Sent when desktop invites the mobile client to connect.
- `event: connect-accepted`: Sent when desktop approves mobile connection.
- `event: batch-offer`: Pushes staged files from desktop to mobile:
  ```text
  event: batch-offer
  data: [{"id":"f1","name":"design.psd","size":45829104}]
  ```
- `event: batch-manifest`: Pushes batch list submitted by desktop.
- `event: batch-complete`: Signals completion of all files in a batch.
- `event: resend-request`: Mobile requested to resend a specific file.
- `event: disconnected`: Desktop ended the session.
- `event: refresh`: Forces browser client to refresh device list or state.

---

## 5. Captive Portal Protocol (Desert Mode)

### 5.1. DNS Interception (UDP `53`)
- Binds to `192.168.137.1:53`.
- Any incoming RFC 1035 query (regardless of domain requested) generates an `A` record response containing IPv4 address `192.168.137.1` with a TTL of 60 seconds.
- Triggers operating system network probe heuristics (iOS CNA, Android Captive Portal, Windows NCSI).

### 5.2. HTTP Redirection (TCP `80`)
- Binds to `192.168.137.1:80`.
- Matches any incoming `GET` request.
- Immediately responds with:
  ```http
  HTTP/1.1 302 Found
  Location: http://192.168.137.1:8080/
  Connection: close
  Content-Length: 0
  ```
- Launches the native system captive portal sheet on connected phones, immediately displaying the We Share web app.
