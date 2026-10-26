# KKR Wireless Print — Phase 1 & Phase 2: Complete System

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![Flutter](https://img.shields.io/badge/Flutter-3.47.8-02569B.svg)](https://flutter.dev/)
[![Android](https://img.shields.io/badge/Android-8.0%2B%20(API%2023%2B)-3DDC84.svg)](https://developer.android.com/)
[![Windows](https://img.shields.io/badge/Windows-10%20%2F%2011-0078D6.svg)](https://microsoft.com/windows)
[![Tests](https://img.shields.io/badge/Windows%20Tests-29%20Passed-brightgreen.svg)]()
[![Tests](https://img.shields.io/badge/Flutter%20Tests-6%20Passed-brightgreen.svg)]()

**KKR Wireless Print** is an end-to-end local network wireless printing system. It enables Android mobile phones connected to a private Wi-Fi network to discover, pair with, and print documents (PDF, JPG, JPEG, PNG) directly to physical USB printers attached to a Windows laptop **without internet access, cloud services, or third-party print relays**.

---

## 1. Physical Hardware & Installed Printers

The Windows Print Server enumerates and manages physical USB printers:

| Printer Name | Connection / Port | Driver | Type | Color | Duplex | Hardware Status |
| :--- | :--- | :--- | :--- | :---: | :---: | :--- |
| **EPSON L3210 Series** | `USB001` | EPSON L3210 Series | Color InkTank Multifunction | Yes | No | **Online / Ready** |
| **Canon MF4800 Series UFRII LT** | `USB004` | Canon MF4800 Series UFRII LT | Monochrome Laser Multifunction | No | Yes | **Online / Ready** |

*Virtual printers also enumerated:* `Microsoft Print to PDF`, `Foxit PDF Editor Printer`, `Print to Evernote`.

---

## 2. System Architecture

```
 c:\Google Antigravity\KKR WIRELESS PRINT\
 ├── KKRWirelessPrint.sln               # Master Windows solution
 ├── KKR_Wireless_Print_APK/            # Standalone Android APK distributions
 │   ├── kkr-wireless-print-release.apk # Production Release APK (67.5 MB)
 │   ├── kkr-wireless-print-debug.apk   # Development Debug APK (172 MB)
 │   └── Install-APK.bat                # 1-Click ADB installer script
 ├── android_client/                    # Complete Flutter & Dart mobile application
 │   ├── lib/
 │   │   ├── models/                    # ServerConnection, PrinterInfo, PrintJob, PrintOptions
 │   │   ├── services/                  # ApiService (Dio, TLS, Auth), StorageService
 │   │   └── ui/
 │   │       ├── screens/               # HomeScreen, ConnectScreen (QR & Manual), PreviewAndSettingsScreen
 │   │       ├── widgets/               # JobTrackingDialog, StatusBadge, PrinterCard
 │   │       └── theme/                 # Dark slate theme matching Windows UI
 │   └── test/                          # Unit & integration tests for models and serialization
 ├── src/
 │   ├── KKR.PrintServer.Core/          # Spooler integration, PDF/Image renderers, PIN auth
 │   ├── KKR.PrintServer.Api/           # Dual HTTP/HTTPS Kestrel ASP.NET Core endpoints
 │   └── KKR.PrintServer.App/           # Modern WPF Desktop Dashboard with dynamic QR code
 └── tests/
     └── KKR.PrintServer.Tests/         # 29 xUnit automated tests
```

---

## 3. Android Application Features

* **Quick Pairing (2 Methods)**:
  1. **Scan QR Code**: Point camera at the scannable QR code on the Windows desktop dashboard to automatically import IP, ports, TLS thumbprint, and active 6-digit PIN.
  2. **Manual IP & PIN Setup**: Enter the laptop's private LAN IPv4 address (e.g. `10.92.81.221`), port (`8080` / `8443`), and the 6-digit dashboard PIN.
* **Document Picker & Android System Share**:
  * Browse device storage for PDF documents and images (JPG, JPEG, PNG).
  * Receive files shared directly from any app (WhatsApp, Chrome, File Manager, Gallery) via the Android System Share sheet (`ACTION_SEND`).
* **Visual Document Preview & Page Count**:
  * Real-time PDF page rendering using native vector graphics.
  * Image dimension and file size inspection before printing.
* **Printer & Capabilities Routing**:
  * Real-time printer discovery from Windows API displaying exact driver capabilities.
  * Color mode validation: Disables color options and informs user when `Canon MF4800` (Monochrome) is selected.
* **Granular Print Settings**:
  * Copies stepper (1 to max driver limit).
  * Page range selection (`"all"`, `"1-3, 5"`).
  * Paper size selection (`A4`, `Letter`, `Legal`, etc.).
  * Orientation (`Auto`, `Portrait`, `Landscape`).
  * Scaling modes (`Fit to Page`, `Fill Page`, `Actual Size`).
  * Center on page toggle.
* **Live Spooler Timeline & Progress**:
  * Real-time upload percentage with byte counter.
  * Live Windows spooler polling: `Accepted` → `Queued` → `Processing` → `Completed`.
  * Safe job cancellation directly from mobile while queued in Windows spooler.
* **Deduplication & Security**:
  * Client-generated `X-Idempotency-Key` prevents double-printing on network retries.
  * Encrypted storage for pairing credentials (`SharedPreferences`).
  * Custom TLS adapter handling self-signed private LAN certificates.

---

## 4. How to Install the Android App

### Method A: One-Click USB Install (Recommended if phone has USB Debugging)
1. Connect your Android phone to the laptop via USB cable.
2. Ensure **USB Debugging** is enabled in *Settings → Developer Options*.
3. Open `c:\Google Antigravity\KKR WIRELESS PRINT\KKR_Wireless_Print_APK\` and double-click `Install-APK.bat`.
4. Tap **Allow** on your phone screen if prompted.

### Method B: Direct File Transfer (No Developer Options required)
1. Copy `kkr-wireless-print-release.apk` from `KKR_Wireless_Print_APK\` to your phone via:
   * USB file transfer (MTP).
   * Local Wi-Fi sharing, Bluetooth, WhatsApp, or Google Drive.
2. On your phone, tap `kkr-wireless-print-release.apk` in Files / Downloads.
3. When prompted, toggle **Allow from this source** (Install unknown apps).
4. Tap **Install** and open **KKR Wireless Print**.

---

## 5. How to Connect Phone to Windows Print Server

1. **Start the Windows Print Server**:
   * Double-click `run-server.bat` in the repository root or run `.\scripts\Run-Server.ps1`.
   * The WPF desktop application opens, showing the active Wi-Fi IP (e.g. `10.92.81.221`), active port (`8080`), a dynamic 6-digit PIN, and a **QR Code**.
2. **Ensure Both Devices are on the Same Wi-Fi**:
   * Both your laptop and Android phone must be connected to the same local Wi-Fi router or laptop mobile hotspot.
3. **Open KKR Wireless Print on Phone**:
   * Tap **Connect to Computer** (or tap the connection status pill).
   * **Option 1 (Fastest)**: Tap the **Scan QR Code** tab, point your camera at the laptop screen QR code. Pairing happens instantly!
   * **Option 2**: Tap **Manual Setup**, enter the Laptop IP, Port, and 6-digit PIN, then tap **Pair with Server**.
4. **Select File & Print**:
   * Tap **Print Document** or share a document from your phone's file manager.
   * Verify print settings, select either **EPSON L3210 Series** or **Canon MF4800 Series**, and tap **Send Print Job**.
   * Track upload and Windows spooler progress in real time!

---

## 6. REST API Specification

### Public Endpoints (No Token Required)
* **`GET /api/v1/discovery`**
  * Returns server metadata, version, active printer names, and TLS thumbprint.
* **`GET /api/v1/health`**
  * Health check returning online printer counts and queued job counts.
* **`POST /api/v1/auth/pair`**
  * Body: `{"pin": "123456", "deviceName": "Pixel 8", "clientType": "Android"}`
  * Returns: `{"success": true, "token": "...", "expiresAtUtc": "...", "clientId": "..."}`

### Protected Endpoints (`Authorization: Bearer <token>`)
* **`GET /api/v1/printers`**
  * Returns array of installed printers with real-time status (`Online`, `OutOfPaper`, `PaperJam`, `Error`) and full driver capabilities (`SupportedPaperSizes`, `SupportsColor`, `CanDuplex`, `MaxCopies`).
* **`GET /api/v1/printers/{name}`**
  * Detailed info for a specific printer.
* **`POST /api/v1/jobs`** (Multipart `multipart/form-data`)
  * Headers: `X-Idempotency-Key: <guid>`
  * Fields: `file` (stream), `printerName`, `copies`, `pageRange`, `orientation`, `paperSize`, `colorMode`, `scalingMode`, `centerOnPage`, `idempotencyKey`.
  * Returns: HTTP `202 Accepted` with `jobId` and spooling status.
* **`GET /api/v1/jobs/{id}`**
  * Real-time status: `Accepted`, `Queued`, `Processing`, `Completed`, `Failed`, or `Cancelled`.
* **`POST /api/v1/jobs/{id}/cancel`**
  * Cancels active job in queue or spooler.
* **`GET /api/v1/jobs`**
  * Returns recent job history for the authenticated client.

---

## 7. Verification & Automated Test Results

### Windows Print Server Test Suite
```powershell
$env:Path = "C:\Users\kisha\.dotnet;" + $env:Path
dotnet test tests\KKR.PrintServer.Tests\KKR.PrintServer.Tests.csproj --logger "console;verbosity=normal"
```
* **Result**: **29/29 Passed (100%)**
  * Security & Auth (PIN generation, validation, rejection, token revocation): 4/4
  * File Validation (PDF `%PDF-`, PNG, JPEG, zero-byte, size limit, forged extensions): 6/6
  * Page Range Parsing (`1-3, 5, 8`, bounds checking, empty/all): 7/7
  * Request Idempotency & Deduplication: 1/1
  * Physical Hardware & Vector PDF/Image Rendering (300 DPI, EPSON L3210 & Canon MF4800): 4/4
  * Full End-to-End API Integration (Pairing, 401 Unauthorized, 202 Accepted Spooling): 7/7

### Android Mobile Test Suite
```powershell
Set-Location "android_client"
flutter test
```
* **Result**: **6/6 Passed (100%)**
  * ServerConnection model serialization and deserialization
  * PrinterInfo Epson color capability parsing
  * PrinterInfo Canon monochrome capability parsing
  * PrintJob status transitions and terminal states
  * Human-readable file size formatting
  * PrintOptions multipart form data conversion
