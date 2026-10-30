# Google Play Store Submission Guide & Assets
## Print Ofo — Mobile Wireless Print Client (v1.0.0)

इस गाइड में आपके **Print Ofo (Android Edition)** को **Google Play Store (Google Play Console)** पर लाइव करने की पूरी जानकारी, डिजिटल कीस्टोर क्रेडेंशियल्स, और तैयार ग्राफिक एसेट्स दिए गए हैं।

---

## 📦 1. तैयार Android पैकेज फाइल्स (Release Files)

सभी फाइल्स आपके कंप्यूटर के इस फोल्डर में सुरक्षित रखी गई हैं:  
📂 **`C:\Google Antigravity\KKR WIRELESS PRINT\release\android`**  
और  
📂 **`C:\Google Antigravity\KKR WIRELESS PRINT\dist`**

| फाइल का नाम | प्रारूप (Format) | उद्देश्य (Purpose) | साइज़ |
|:---|:---:|:---|:---:|
| **`PrintOfo.aab`** | **.AAB** | **Google Play Console पर अपलोड करने के लिए** (Google Play Store के लिए अनिवार्य डिजिटल हस्ताक्षरित बंडल) | ~62.3 MB |
| **`PrintOfo.apk`** | **.APK** | **अपने या किसी भी Android फोन पर डायरेक्ट इंस्टॉल करने के लिए** (Direct Testing / Sideloading) | ~72.1 MB |
| **`printofo_release.keystore`** | Keystore | **ऐप का डिजिटल हस्ताक्षर सर्टिफिकेट** (Play Store App Signing Key) | ~2.69 KB |

---

## 🔑 2. Keystore Signing Credentials (सुरक्षित रखें)

यह कीस्टोर आपके ऐप को डिजिटली साइन करने के लिए तैयार किया गया है:
* **Keystore File:** `android_client/android/app/printofo_release.keystore` (बैकअप: `release/android/printofo_release.keystore`)
* **Alias Name:** `printofo`
* **Keystore Password:** `PrintOfo2026`
* **Key Password:** `PrintOfo2026`
* **Validity:** 10,000 Days (~27 वर्ष)

---

## 🎨 3. तैयार Play Store ग्राफिक एसेट्स (Visual Assets)

सभी आवश्यक छवियां आपके फोल्डर में उपलब्ध हैं:  
📂 **`C:\Google Antigravity\KKR WIRELESS PRINT\release\playstore_assets`**

| एसेट का नाम | रेज़ोल्यूशन (Resolution) | विवरण |
|:---|:---:|:---|
| **`playstore_icon_512x512.png`** | **512 × 512** | आधिकारिक 32-बिट सॉलिड बैकग्राउंड Play Store ऐप आइकन |
| **`playstore_feature_graphic_1024x500.png`** | **1024 × 500** | Play Store लिस्टिंग का मुख्य फीचर बैनर (Feature Graphic) |
| **`Phone_Screenshot_1_Home_1080x2400.png`** | **1080 × 2400** | स्क्रीनशॉट 1: होम स्क्रीन — सर्वर स्टेटस, प्रिंटर लिस्ट व रीसेंट जॉब्स |
| **`Phone_Screenshot_2_Scanner_Connect_1080x2400.png`** | **1080 × 2400** | स्क्रीनशॉट 2: QR कोड स्कैनर व मैन्युअल IPv4 / PIN पेयरिंग |
| **`Phone_Screenshot_3_Preview_Settings_1080x2400.png`** | **1080 × 2400** | स्क्रीनशॉट 3: डॉक्यूमेंट व फोटो प्रीव्यू, कॉपीज़, कलर व पेपर सेटिंग्स |
| **`Phone_Screenshot_4_Spooler_Tracking_1080x2400.png`** | **1080 × 2400** | स्क्रीनशॉट 4: लाइव स्पूलर मॉनिटर व प्रोग्रेस डायलॉग |

---

## 📱 4. Play Store Listing Content (सीधे कॉपी-पेस्ट करें)

### A. App Name / Title (अधिकतम 30 अक्षर):
```text
Print Ofo
```

### B. Short Description (अधिकतम 80 अक्षर):
```text
Wireless local mobile printing to USB Canon, Epson & PC printers over Wi-Fi.
```

### C. Full Description (विस्तृत विवरण):
```text
Print Ofo is a fast, reliable, and 100% offline mobile printing application that connects your Android smartphone to your Windows PC over local Wi-Fi, allowing you to print PDF documents and high-resolution photos directly to USB printers.

🚀 POWERFUL MOBILE PRINTING FEATURES:

1. ONE-SCAN QR CODE PAIRING
• Connect to your Windows PC instantly by scanning the dynamic desktop QR code.
• Or pair using a secure 6-digit PIN and local IPv4 address.
• Zero complicated network configuration or Bluetooth pairing headaches.

2. PRINT ANY DOCUMENT OR PHOTO
• PDF Vector Documents: Crisp, sharp text and vector rendering.
• Photos & Images: Direct printing of JPG, JPEG, and PNG images.
• Android Share Sheet Integration: Share files directly to Print Ofo from WhatsApp, Android Gallery, File Manager, or Chrome.

3. HARDWARE PRINTER SUPPORT
• Works with all printers connected to your Windows computer:
  - Canon MF4800, MF3010, LBP series and laser printers.
  - EPSON L3210, L3150, EcoTank color series.
  - HP LaserJet & DeskJet series.
  - Brother, Pantum, and all Windows-compatible printer drivers.

4. FULL PRINT CUSTOMIZATION
• Copies count selector.
• Page Range selection (e.g. 1-5, 8).
• Color vs Monochrome (Black & White).
• Portrait and Landscape orientation.
• Fit to Page and scaling options.

5. REAL-TIME SPOOLER TRACKING
• Watch live print job progress on your mobile screen.
• Instant notification upon job completion.
• Cancel print jobs directly from your phone.

🔒 100% PRIVACY & SECURITY GUARANTEE:
All data transfer occurs strictly within your local Wi-Fi router / mobile hotspot. No documents, photos, or passwords are ever sent to external cloud servers. Operates fully offline without an active internet connection.
```

---

## 🛡️ 5. Data Safety (डेटा सुरक्षा) फॉर्म के उत्तर

Google Play Console पर जब "Data safety" फॉर्म आएगा:
* **Does your app collect or share any user data?** ➔ **`No`**
* **Does your app use third-party analytics/trackers?** ➔ **`No`**
* **Is all data processed locally on the user's device?** ➔ **`Yes`**

---

## 📋 6. Google Play Console पर अपलोड करने के चरण (Step-by-Step):

1. **[play.google.com/console](https://play.google.com/console)** पर लॉगिन करें।
2. **"Create app"** पर क्लिक करें:
   * **App name:** `Print Ofo`
   * **Default language:** `English (United States)`
   * **App or game:** `App`
   * **Free or paid:** `Free`
3. **Dashboard ➔ Set up your app:**
   * **Privacy policy URL:** `https://github.com/kkrkishan/kkr-wireless-print/blob/main/README.md`
   * **App access:** `All functionality is available without special access`
   * **Ads:** `No, my app does not contain ads`
   * **Content rating:** Questionnaire भरें (Utility app, "All Ages" / "Everyone" रेटिंग आएगी)।
   * **Target audience:** `13 and above` (All ages).
4. **Store listing ➔ Upload Graphics:**
   * **App icon:** `release/playstore_assets/playstore_icon_512x512.png`
   * **Feature graphic:** `release/playstore_assets/playstore_feature_graphic_1024x500.png`
   * **Phone screenshots:** 4x स्क्रीनशॉट्स अपलोड करें (`release/playstore_assets/Phone_Screenshot_...`).
5. **Production ➔ Releases:**
   * **"Create new release"** पर क्लिक करें।
   * `release/android/PrintOfo.aab` फ़ाइल को ड्रैग और ड्रॉप करें।
   * Release name: `1.0.0`
   * **"Review release"** ➔ **"Start rollout to Production"** पर क्लिक करें!
