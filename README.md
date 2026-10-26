# KKR Wireless Print — Phase 1: Windows Print Server

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![Windows](https://img.shields.io/badge/Windows-10%20%2F%2011-0078D6.svg)](https://microsoft.com/windows)
[![WPF](https://img.shields.io/badge/UI-WPF%20Desktop-purple.svg)](https://docs.microsoft.com/dotnet/desktop/wpf/)
[![Tests](https://img.shields.io/badge/Tests-29%20Passed%20(100%25)-brightgreen.svg)]()

**KKR Wireless Print** is a local network desktop printing system enabling authorized mobile applications (Android) on the same Wi-Fi network to submit documents (PDF, PNG, JPG, JPEG) to physical USB printers connected to a Windows laptop **without requiring internet connectivity or cloud print services**.

---

## 1. Physical Hardware & Installed Printers

On this Windows host, both physical USB printers were discovered, verified, and mapped:

| Printer Name | Connection / Port | Driver | Type | Color | Duplex | Hardware Status |
| :--- | :--- | :--- | :--- | :---: | :---: | :--- |
| **EPSON L3210 Series** | `USB001` | EPSON L3210 Series | Color InkTank Multifunction | Yes | No | **Online / Ready** |
| **Canon MF4800 Series UFRII LT** | `USB004` | Canon MF4800 Series UFRII LT | Monochrome Laser Multifunction | No | Yes | **Online / Ready** |

*Virtual printers also enumerated:* `Microsoft Print to PDF`, `Foxit PDF Editor Printer`, `Print to Evernote`.

---

## 2. Architecture & Solution Structure

The solution follows a clean modular architecture separating Core domain logic, Local API, WPF Desktop Dashboard, and automated test suites:

```
c:\Google Antigravity\KKR WIRELESS PRINT\
├── KKRWirelessPrint.sln               # Master solution file
├── Directory.Build.props              # Shared build settings (C# 12, .NET 8)
├── nuget.config                       # Isolated package repository configuration
├── scripts/
│   ├── Configure-Firewall.ps1         # Windows Firewall rule setup (Private profile only)
│   └── Run-Server.ps1                 # One-click desktop launcher
├── src/
│   ├── KKR.PrintServer.Core/          # Domain models, spooler integration, PDF/Image renderers
│   │   ├── Models/                    # PrinterInfo, PrintJobRecord, PrintJobRequest, Enums
│   │   ├── Services/
│   │   │   ├── PrinterDiscoveryService.cs  # System.Printing & Win32 winspool enumeration
│   │   │   ├── PrintProcessor.cs           # GDI+ / PrintDocument pipeline with exact geometry
│   │   │   ├── PdfRenderer.cs              # Windows.Data.Pdf native WinRT vector rendering
│   │   │   ├── ImageRenderer.cs            # EXIF orientation, Bicubic scaling (Fit/Fill/Actual)
│   │   │   ├── TestPageGenerator.cs        # 300 DPI high-res diagnostic test page generator
│   │   │   ├── JobQueueService.cs          # Thread-safe queue worker & cancellation manager
│   │   │   ├── SecurityService.cs          # PIN generator, HMAC token auth, deduplication, magic bytes
│   │   │   ├── CertificateService.cs       # X.509 self-signed TLS cert generator with SANs
│   │   │   └── NativePrintSpooler.cs       # Win32 winspool.drv P/Invoke (SetJob, CancelSpoolerJob)
│   │   └── Logging/
│   │       ├── AppLogger.cs                # Observable in-memory log sink + rotating file logs
│   │       └── LogEntry.cs                 # Structured log entity (no document content logged)
│   ├── KKR.PrintServer.Api/           # In-process Kestrel ASP.NET Core local network service
│   │   ├── Middleware/
│   │   │   └── TokenAuthenticationMiddleware.cs # Bearer token validator for protected routes
│   │   ├── Endpoints/
│   │   │   ├── DiscoveryEndpoints.cs       # /api/v1/discovery & /api/v1/health
│   │   │   ├── AuthEndpoints.cs            # /api/v1/auth/pair (PIN validation)
│   │   │   ├── PrinterEndpoints.cs         # /api/v1/printers (Status & Capabilities)
│   │   │   └── JobEndpoints.cs             # /api/v1/jobs (Multipart print submissions)
│   │   └── Server/
│   │       └── PrintApiHost.cs             # Dual HTTP/HTTPS Kestrel server binding
│   └── KKR.PrintServer.App/           # Modern WPF Desktop Dashboard
│       ├── App.xaml / App.xaml.cs     # Composition root & lifecycle
│       ├── MainWindow.xaml / .cs      # Dark-slate dashboard UI
│       └── ViewModels/
│           ├── MainViewModel.cs       # State manager, background timers, commands
│           ├── PrinterItemViewModel.cs # Per-printer card & direct test-print action
│           ├── JobItemViewModel.cs     # Real-time spooler queue item with cancel action
│           └── RelayCommand.cs         # MVVM command implementation
└── tests/
    └── KKR.PrintServer.Tests/         # 29 xUnit unit & integration tests
        ├── SecurityServiceTests.cs    # PIN & token authentication tests
        ├── FileValidationTests.cs     # Magic byte verification (PDF, PNG, JPEG)
        ├── PageRangeParserTests.cs    # Page range filtering ("1-3, 5, 8")
        ├── IdempotencyTests.cs        # Deduplication of identical requests
        ├── ApiIntegrationTests.cs     # End-to-end API test server verification
        └── HardwareVerificationTests.cs # Physical printer discovery & 300 DPI rendering tests
```

---

## 3. Security & Network Model

* **Zero Internet Requirement**: Operates strictly on private Wi-Fi (binds to active LAN IP, e.g. `10.92.81.221`).
* **Dynamic Pairing PIN**: Generates a cryptographically secure 6-digit random PIN displayed on the desktop dashboard. Expiration is enforced (default 15 minutes) with constant-time verification (`CryptographicOperations.FixedTimeEquals`).
* **Cryptographic Token Authorization**: When an Android device pairs with the correct PIN, the server issues a 256-bit cryptographically random Bearer token. Subsequent requests must include `Authorization: Bearer <token>`.
* **Private Network Firewall Rules**: Windows Firewall is never disabled. Run `scripts/Configure-Firewall.ps1` to open TCP ports `8443` and `8080` exclusively on the **Private** network profile.
* **HTTPS & Certificate Strategy**: The server automatically generates an RSA-2048, SHA-256 self-signed X.509 certificate with Subject Alternative Names (SANs) for `localhost`, the machine name, and all detected LAN IPs. Public certificates (`kkr-print-server.cer` and `.pem`) are exported to `certs/` for Android trust or certificate pinning.
* **Payload Validation & Safe Temp Files**:
  * **Magic Bytes Check**: Verifies file headers (`%PDF-`, `PNG`, `JPEG`) before spooling. Rejects executable or forged file extensions.
  * **Configurable Size Limits**: Rejects uploads exceeding 50 MB (configurable).
  * **Safe Temp Handling**: Files are written to unique GUID filenames in `temp_spool/` and deleted immediately upon completion or cancellation.
* **Idempotency & Deduplication**: Clients provide an `X-Idempotency-Key` or form parameter. Rapid double-taps return the original job without wasting physical ink or paper.
* **Zero Document Payload Logging**: Structured audit logs record timestamp, level, document name, size, page count, and status. Document contents are **never** logged to disk.

---

## 4. REST API Specification

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
  * `file`: Binary stream (PDF, JPG, JPEG, PNG).
  * `printerName`: Destination printer name.
  * `copies`: Integer copy count (default `1`).
  * `pageRange`: Optional range string (e.g. `"1-3, 5"` or `"all"`).
  * `orientation`: `"Auto"`, `"Portrait"`, or `"Landscape"`.
  * `paperSize`: Target paper name (e.g. `"A4"`, `"Letter"`).
  * `colorMode`: `"Auto"`, `"Color"`, or `"Grayscale"`.
  * `scalingMode`: `"FitPage"`, `"FillPage"`, or `"ActualSize"`.
  * `centerOnPage`: Boolean (default `true`).
  * `idempotencyKey`: Client UUID string.
  * Returns: HTTP `202 Accepted` with `jobId` and spooling status.
* **`GET /api/v1/jobs/{id}`**
  * Current status of print job: `Accepted`, `Queued`, `Processing`, `Completed`, `Failed`, or `Cancelled`.
* **`POST /api/v1/jobs/{id}/cancel`**
  * Cancels active job in queue or spooler.
* **`GET /api/v1/jobs`**
  * Returns recent job history for the authenticated client.

---

## 5. Build and Execution Instructions

### Prerequisites
* Windows 10/11 x64.
* .NET 8 SDK (installed at `C:\Users\kisha\.dotnet` or standard Program Files).

### Compiling the Solution
```powershell
$env:Path = "C:\Users\kisha\.dotnet;" + $env:Path
dotnet build KKRWirelessPrint.sln
```

### Running All Automated Tests
```powershell
$env:Path = "C:\Users\kisha\.dotnet;" + $env:Path
dotnet test tests\KKR.PrintServer.Tests\KKR.PrintServer.Tests.csproj --logger "console;verbosity=normal"
```

### Launching the Windows Desktop Application
```powershell
.\scripts\Run-Server.ps1
```
Or directly:
```powershell
& "src\KKR.PrintServer.App\bin\Debug\net8.0-windows10.0.19041.0\KKR.PrintServer.App.exe"
```

---

## 6. Test Results Summary

A total of **29 automated tests** were executed and passed with 100% success:

* **Security & Auth Tests (4/4)**: PIN generation, pairing authentication, invalid PIN rejection, client revocation.
* **File Validation Tests (6/6)**: PDF magic byte validation (`%PDF-`), PNG magic bytes, JPEG magic bytes, zero-byte rejection, size limit rejection, forged extension detection.
* **Page Range Parser Tests (7/7)**: Multi-range parsing (`"1-3, 5, 8"`), empty/all ranges, out-of-range bounds clipping, ascending ordering.
* **Idempotency Tests (1/1)**: Deduplication identification and existing job ID resolution.
* **Hardware & Rendering Tests (4/4)**:
  * Enumeration of physical USB printers (`EPSON L3210 Series` on `USB001`, `Canon MF4800 Series UFRII LT` on `USB004`).
  * Native PDF vector rendering at 300 DPI (`2550 x 3300` px) without distortion.
  * 300 DPI diagnostic test page generation (`2480 x 3508` px) with color calibration bars and alignment patterns.
  * High-quality bicubic aspect ratio scaling calculations (`FitPage`, `FillPage`, `ActualSize`).
* **API Integration Tests (7/7)**: Public discovery endpoint, health check, 401 Unauthorized rejection for missing tokens, successful PIN pair and token exchange, 400 Bad Request on invalid PIN, multipart PDF job submission (202 Accepted), idempotency duplicate detection.

---

## 7. Phase 2: Android App Implementation Roadmap

With Phase 1 complete and verified, Phase 2 will deliver the companion Android application:

1. **Discovery & Connection Module**:
   * mDNS / NSD (Network Service Discovery) or direct IP entry (`10.92.81.221`).
   * Ping `GET /api/v1/discovery` to verify Windows server presence.
2. **Pairing Flow**:
   * Screen prompting for the 6-digit PIN shown on the laptop dashboard.
   * `POST /api/v1/auth/pair` storing the received Bearer token in Android `EncryptedSharedPreferences`.
   * SSL Pinning using the exported `kkr-print-server.cer` or certificate thumbprint.
3. **Document Selection & Print Settings UI**:
   * Android Document Picker for PDFs and Gallery Picker for images.
   * Printer selection dropdown displaying `EPSON L3210` (Color) and `Canon MF4800` (Mono Duplex).
   * Settings: Copies, Page Range, Orientation (Auto/Portrait/Landscape), Color Mode, Scaling (Fit to Page / Fill Page).
4. **Job Submission & Polling**:
   * Background upload via Retrofit / OkHttp with `X-Idempotency-Key`.
   * Real-time progress bar polling `GET /api/v1/jobs/{id}` until spooler acceptance.
