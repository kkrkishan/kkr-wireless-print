# KKR Wireless Print — Microsoft Store (Windows App Store) Publishing Guide

यह गाइड आपको **KKR Wireless Print** को **Microsoft Store (Windows App Store)** पर पब्लिश करने के आसान और आधिकारिक स्टेप्स बताता है।

Microsoft Store अब सामान्य Win32 `.exe` इंस्टॉलर्स को सीधे सपोर्ट करता है, जिससे किसी जटिल पैकेजिंग की आवश्यकता नहीं होती।

---

## 1. आवश्यक पूर्व-आवश्यकताएं (Prerequisites)

1. **Microsoft Developer Account:**
   * [Microsoft Partner Center](https://partner.microsoft.com/dashboard) पर जाएं।
   * "Windows & Xbox" डेवलपर अकाउंट में साइन इन / रजिस्टर करें।
   * (व्यक्तिगत/Individual अकाउंट के लिए लगभग ₹1,500 - ₹1,600 का एकमुश्त/One-time रजिस्ट्रेशन शुल्क होता है)।

2. **सार्वजनिक डाउनलोड लिंक (Public HTTPS URL):**
   * हमारी ऐप का GitHub Release इंस्टॉलर लिंक पहले से तैयार है:
     ```text
     https://github.com/kkrkishan/kkr-wireless-print/releases/download/v1.0.0/KKR_Wireless_Print_Setup.exe
     ```

3. **लोगो और इमेजेस (Store Branding Assets):**
   * आपके प्रोजेक्ट के `store_assets/` फ़ोल्डर में सभी आवश्यक लोगो तैयार हैं:
     - `StoreLogo.png` (300 x 300)
     - `Square150x150Logo.png` (150 x 150)
     - `Square44x44Logo.png` (44 x 44)
     - `Wide310x150Logo.png` (310 x 150)

---

## 2. Microsoft Partner Center में ऐप सबमिट करने के स्टेप्स

### स्टेप 1: ऐप का नाम रिज़र्व करें (Reserve Name)
1. Partner Center डैशबोर्ड खोलें: `https://partner.microsoft.com/dashboard/apps-and-games/overview`
2. **"New product"** -> **"MSIX or PWA app"** / **"EXE or MSI app"** चुनें।
3. **App name** दर्ज करें: `KKR Wireless Print`
4. **"Reserve product name"** पर क्लिक करें।

---

### स्टेप 2: इंस्टॉलर और पैकेज सेटिंग्स (Installer Details)
1. **Package type** में **"EXE"** चुनें।
2. **Download URL:**
   ```text
   https://github.com/kkrkishan/kkr-wireless-print/releases/download/v1.0.0/KKR_Wireless_Print_Setup.exe
   ```
3. **Silent install parameters:**
   ```text
   /VERYSILENT /SUPPRESSMSGBOXES /NORESTART
   ```
4. **Silent uninstall parameters:**
   ```text
   /VERYSILENT /SUPPRESSMSGBOXES /NORESTART
   ```
5. **Install architecture:** `x64`
6. **Minimum OS version:** `Windows 10, version 1809 (Build 17763) or higher`

---

### स्टेप 3: स्टोर लिस्टिंग और विवरण (Store Listing)

* **Product Title:** KKR Wireless Print — Windows Print Server
* **Short Description:**
  High-speed wireless local print server. Connect your Android phone to your Windows PC over local Wi-Fi and print PDF documents and high-resolution images directly to USB printers (Canon, Epson, etc.).
* **Full Description:**
  KKR Wireless Print transforms your Windows PC into a high-performance local wireless print server.
  
  Key Features:
  - Print from Android smartphones to locally connected USB printers (Canon MF4800, Epson L3210, and all Windows printers).
  - High-resolution vector PDF rendering with Windows WinRT.
  - Crystal clear photo and image printing (JPG, PNG).
  - QR Code one-scan instant pairing.
  - Real-time print spooler tracking and job management.
  - 100% offline and secure local network operation (no cloud dependency).
* **Category:** Utilities & tools > Productivity
* **Keywords / Search Terms:**
  `print`, `wireless print`, `print server`, `wifi print`, `android print`, `canon`, `epson`, `kkr`
* **Support Contact:** `https://github.com/kkrkishan/kkr-wireless-print/issues`
* **Privacy Policy URL:** `https://github.com/kkrkishan/kkr-wireless-print/blob/main/README.md`

---

### स्टेप 4: स्क्रीनशॉट और लोगो अपलोड करें (Screenshots & Logos)
1. **Store Logo:** `store_assets/StoreLogo.png` अपलोड करें।
2. **Screenshots:**
   - ऐप चालू करें (`Run-Server.bat` से)।
   - कीबोर्ड पर `Win + Shift + S` दबाकर ऐप विंडो का स्क्रीनशॉट लें।
   - स्क्रीनशॉट को Partner Center में अपलोड करें (कम से कम 1 स्क्रीनशॉट आवश्यक है)।

---

### स्टेप 5: सबमिट और सर्टिफिकेशन (Submit for Certification)
1. **Pricing and availability:** "Free" चुनें और "Worldwide" सेलेक्ट करें।
2. **Age rating:** Questionnaire भरें (यह एक यूटिलिटी टूल है, रेटिंग "All Ages" आएगी)।
3. **"Submit to the Store"** पर क्लिक करें।

माइक्रोसॉफ्ट की टीम 24 से 48 घंटों में ऐप की जांच (Certification) करती है और ऐप **Microsoft Store** पर लाइव हो जाएगी!
