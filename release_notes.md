# KKR Wireless Print — v1.0.0 Release

High-Speed Local Wi-Fi Printing System for Windows & Android Mobile Client.

---

### 📦 Applications Included in this Release

#### 🖥️ 1. Windows Desktop Application & Print Server
* **`KKR_Wireless_Print_Setup.exe`** (58.5 MB): Complete Windows installer with Start Menu & Desktop shortcuts and automated Windows Firewall configuration.
* **`KKR_Wireless_Print_Full_Offline_Setup.exe`** (58.5 MB): Offline installer designed for air-gapped target computers without internet access.
* **`KKR_Wireless_Print_Portable_v1.0.0.zip`** (83.8 MB): Zero-installation portable edition runnable directly from any folder or USB flash drive.
* **Key Features:**
  - Self-contained .NET 8 runtime (zero SDK or external dependencies required).
  - High-performance vector PDF rendering via Windows WinRT (`Windows.Data.Pdf`).
  - High-resolution image rendering for photos and documents.
  - Native Win32 `winspool.drv` spooler tracking and cancellation.
  - Dynamic discovery of USB printers (verified with Canon MF4800 Series and Epson L3210 Series).
  - Automated Windows Firewall rule creation for ports 8080 (HTTP) & 8443 (HTTPS).
  - Single-instance process takeover and dynamic Wi-Fi IP binding.

#### 📱 2. Android Mobile Application
* **`kkr-wireless-print-release.apk`** (67.5 MB): Production release Android APK.
* **Key Features:**
  - Quick pairing via Windows server QR code scanner or manual IPv4/PIN entry.
  - Browse and select PDF documents and image files (JPG, JPEG, PNG).
  - Direct sharing from Android Gallery, WhatsApp, File Manager via Android Share Sheet.
  - Real-time printer discovery from Windows server with online/offline status.
  - Comprehensive print options: Copies, Color / Grayscale, Portrait / Landscape, Page Range, Fit-to-Page scaling.
  - Live print spooler job status tracking dialog with progress indicators and cancel support.

---

### 🛡️ SHA-256 Checksums
```text
227f07a31a5f300984da3c9848adb1502d7a4274cd123818dbf07424621f3ec8  kkr-wireless-print-release.apk
55d2ae4751c64a89ef2f24536b935f2ff0d5628dcee86d2f3e5fe90fee6b0877  KKR_Wireless_Print_Full_Offline_Setup.exe
edff71b4b150d6b4358bd67b4b459b3c065155a927f1161e3ca41065c591d99d  KKR_Wireless_Print_Portable_v1.0.0.zip
55d2ae4751c64a89ef2f24536b935f2ff0d5628dcee86d2f3e5fe90fee6b0877  KKR_Wireless_Print_Setup.exe
```
