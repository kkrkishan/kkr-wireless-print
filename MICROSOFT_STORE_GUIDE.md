# Microsoft Store (Windows App Store) Submission Guide & Metadata
## Print Ofo — Windows Print Server (v1.0.0)

This comprehensive guide contains all pre-formatted details, submission parameters, and graphic asset specifications required to publish **Print Ofo** on the **Microsoft Store** via [Microsoft Partner Center](https://partner.microsoft.com/dashboard).

---

## 1. Store Product Information & Classification

* **Product Name:** `Print Ofo`
* **Product Title in Store:** `Print Ofo — Windows Print Server`
* **Package Type:** `EXE` (Direct Win32 Unpackaged Application)
* **Primary Category:** `Utilities & Tools`
* **Secondary Category:** `Productivity`
* **Pricing & Availability:** `Free` (Worldwide / All Markets)
* **Target Architecture:** `x64`
* **Minimum OS Version:** `Windows 10, version 1809 (Build 17763) or higher`

---

## 2. Partner Center EXE Submission Parameters (Copy-Paste)

In Partner Center under **Packages** / **EXE or MSI app**:

| Parameter Field | Value to Enter |
|:---|:---|
| **Installer URL** | `https://github.com/kkrkishan/kkr-wireless-print/releases/download/v1.0.0/Print_Ofo_Setup.exe` |
| **Silent Install Arguments** | `/VERYSILENT /SUPPRESSMSGBOXES /NORESTART` |
| **Silent Uninstall Arguments** | `/VERYSILENT /SUPPRESSMSGBOXES /NORESTART` |
| **Install Architecture** | `x64` |
| **Minimum Operating System** | `Windows 10, version 1809 (10.0.17763.0)` |
| **Languages Supported** | `English (United States)`, `Hindi` |

---

## 3. Store Listing Content (English)

### A. Short Description (up to 270 characters):
> Print Ofo transforms your Windows PC into a high-speed wireless print server. Connect your Android smartphone over local Wi-Fi to print vector PDF documents and high-resolution photos directly to Canon, Epson, and any USB printer. 100% offline & secure.

### B. Full Description:
> **Print Ofo** is a fast, reliable, and privacy-first Windows Print Server application that allows Android smartphones and laptops on the same local Wi-Fi network to print directly to USB-connected printers without cloud servers or third-party drivers.
>
> 🚀 **KEY FEATURES:**
>
> 1. **High-Speed Mobile Wireless Printing:**
> • Connect Android phones instantly via dynamic desktop QR code or secure 6-digit PIN.
> • Print PDFs, images (JPG, JPEG, PNG), and scanned documents in seconds.
> • Integrated Android Share Sheet: Share files directly from WhatsApp, Gallery, or Files to Print Ofo.
>
> 2. **Native Windows Spooler Engine:**
> • Direct interop with Windows native spooler (`winspool.drv`).
> • Automatic dynamic discovery of physical printers: Canon MF4800 Laser series, EPSON L3210 InkTank series, HP, Brother, and all installed Windows drivers.
> • Supports full print customization: Copies, Page Range, Color / Monochrome, Duplex, Paper Size (A4, Letter, 4×6"), and Fit-to-Page scaling.
>
> 3. **Crystal-Clear Vector PDF & Photo Quality:**
> • Native Windows WinRT vector rasterization engine (`Windows.Data.Pdf`) ensures razor-sharp text and graphics with zero blurriness.
> • GDI+ high-DPI image pipeline delivers photographic clarity.
>
> 4. **Live Spooler Queue & Job Tracking:**
> • Real-time job status monitoring from mobile and PC.
> • Instant feedback on spooling, printing, and completed states.
> • One-click job cancellation directly from phone or desktop.
> • Built-in 300 DPI diagnostic test page generator.
>
> 🔒 **100% PRIVATE, LOCAL & SECURE:**
> All communication takes place strictly within your local Wi-Fi network over high-performance Kestrel HTTP (port 8080) and TLS HTTPS (port 8443). Zero documents, photos, or metadata are ever uploaded to any cloud server. Works completely offline.

### C. Search Terms / Keywords (up to 7 tags):
1. `print server`
2. `wireless print`
3. `wifi print`
4. `android print`
5. `canon printer`
6. `epson printer`
7. `print ofo`

---

## 4. Mandatory Links for Partner Center

* **Privacy Policy URL:** `https://github.com/kkrkishan/kkr-wireless-print/blob/main/README.md`
* **Support Contact / URL:** `https://github.com/kkrkishan/kkr-wireless-print/issues`
* **Publisher Display Name:** `Print Ofo` (or your registered Developer Name)

---

## 5. Visual Assets Prepared for Microsoft Store

All assets are generated in exact Microsoft Store specifications in your local folder:  
📂 **`C:\Google Antigravity\KKR WIRELESS PRINT\store_assets`**

| Asset Name | Resolution | Format | Purpose |
|:---|:---:|:---:|:---|
| **`StoreLogo.png`** | **300 × 300** | PNG | Main Microsoft Store App Icon |
| **`Square150x150Logo.png`** | **150 × 150** | PNG | Medium App Tile |
| **`Square44x44Logo.png`** | **44 × 44** | PNG | Small Taskbar / List Icon |
| **`Wide310x150Logo.png`** | **310 × 150** | PNG | Wide Start Menu Tile |
| **`LargeTile310x310Logo.png`** | **310 × 310** | PNG | Large Start Menu Tile |
| **`SplashScreen620x300.png`** | **620 × 300** | PNG | Splash Screen Tile |
| **`BoxArt_1080x1080.png`** | **1080 × 1080** | PNG | 1:1 Square Box Art for Store Listing |
| **`BoxArt_2160x2160.png`** | **2160 × 2160** | PNG | High-Res 1:1 Box Art |
| **`Poster_720x1080.png`** | **720 × 1080** | PNG | 2:3 Vertical Poster Art |
| **`Poster_1440x2160.png`** | **1440 × 2160** | PNG | High-Res 2:3 Vertical Poster Art |
| **`Screenshot_1_Dashboard_1920x1080.png`** | **1920 × 1080** | PNG | Main Dashboard: Wi-Fi IP, QR Code & Spooler |
| **`Screenshot_2_Printers_1920x1080.png`** | **1920 × 1080** | PNG | Connected Hardware: Canon & Epson Printers |
| **`Screenshot_3_Pairing_Security_1920x1080.png`** | **1920 × 1080** | PNG | Secure Mobile Pairing & PIN Authentication |
| **`Screenshot_4_SpoolerQueue_1920x1080.png`** | **1920 × 1080** | PNG | Live Queue & Spooler Monitor |
