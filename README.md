# RigBadge

Windows masaüstü için doğrudan masaüstü kabuğuna gömülü (`Progman`), Windows 11 DWM akrilik cam (Glassmorphism) efektli ve günlük donanım piyasa fiyatlarını sergileyen minimalist donanım gadgetı.

---

## Özellikler

* **Glassmorphism & Modern DWM Akrilik Efekti:**
  * Windows 11 yerel DWM akrilik arka planı (`DWMWA_SYSTEMBACKDROP_TYPE = Acrylic`).
  * İnce ışık yansımalı beyaz cam kenarlığı (`GlassBorderBrush`), yumuşak ortam gölgesi ve yarı saydam koyu taban.
* **Günlük Piyasa Fiyatları ve Toplam Değer:**
  * Her donanımın güncel piyasa fiyatı (TL cinsinden) satır hizasında listelenir.
  * Kartın altında tüm sistemin toplam değerini gösteren `TOPLAM DONANIM DEĞERİ` özet rozeti yer alır.
  * Fiyatlar `hardware-prices.json` dosyasında saklanır ve her gün (24 saatlik döngüde) otomatik olarak tazelenir.
* **Windows ile Birlikte Otomatik Başlama:**
  * Bilgisayar her açıldığında widget doğrudan masaüstünde otomatik olarak başlar (`HKCU\...\Run` kaydı).
  * Sağ tık menüsünden `🚀 Windows ile Birlikte Başlat` seçeneğiyle istendiğinde açılıp kapatılabilir.
* **Masaüstü Kabuğuna Sabitleme (`Progman` Mülkiyeti):**
  * Widget masaüstü kabuğunun (`Progman`) mülkiyetine bağlıdır; masaüstüne tıklandığında veya `Win + D` yapıldığında duvar kâğıdının arkasına düşmez.
  * `WS_EX_TOOLWINDOW` ve `WS_EX_NOACTIVATE` sayesinde görev çubuğunda yer kaplamaz, odağı çalmaz.
  * Sol tık ile masaüstünde serbestçe sürüklenebilir.

---

## Donanım ve Fiyat Listesi

1. **SİSTEM**
   * **İşlemci:** AMD Ryzen 7 5700X (`8C / 16T`) • `8.250 ₺`
   * **Ekran Kartı:** GeForce RTX 3060 (`PCIe`) • `12.800 ₺`
   * **Anakart:** MSI B550M PRO-VDH WIFI (`AM4`) • `5.200 ₺`
   * **RAM:** 48 GB Kingston (`3600 MHz`) • `5.550 ₺`
2. **DEPOLAMA**
   * **M.2 SSD:** Kingston NV2 NVMe (`1 TB M.2`) • `2.650 ₺`
   * **SATA SSD:** SATA III SSD (`512 GB`) • `1.250 ₺`
3. **ÇEVRE BİRİMLERİ**
   * **Monitör:** MSI MAG 27CQ6F (`180Hz 1440p`) • `14.320 ₺`
   * **Klavye:** Corsair K55 RGB PRO XT • `2.450 ₺`
   * **Fare:** ROG Harpe Ace Aim Lab • `3.570 ₺`
   * **Kulaklık:** Razer Kraken Kitty V2 BT • `4.250 ₺`
   * **Mikrofon:** Razer Seiren V3 Chroma • `5.600 ₺`
4. **SOĞUTMA VE GÜÇ**
   * **Soğutucu:** Corsair iCUE LINK TITAN 240 RX • `5.200 ₺`
   * **PSU:** MSI MPG A750GF White (`750W Gold`) • `7.490 ₺`
5. **ÖZET**
   * **Toplam Sistem Değeri:** `~78.580 ₺`

---

## Yapılandırma Dosyaları

* `hardware-config.json`: Konum, boyut, opaklık ve manuel donanım modelleri.
* `hardware-prices.json`: Günlük takip edilen donanım parça fiyatları ve son güncelleme tarihi.

---

## Derleme ve Çalıştırma

```powershell
cd D:\projects\RigBadge
dotnet build -c Release
& "D:\projects\RigBadge\bin\x64\Release\net10.0-windows\RigBadge.exe"
```
