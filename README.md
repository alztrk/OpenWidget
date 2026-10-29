# RigBadge

Windows masaüstü için hafif, çerçevesiz ve modern donanım kimlik widgetı.

Bilgisayarınıza bağlı olan çekirdek bileşenleri (CPU, GPU, Anakart, RAM), depolama birimlerini (SSD), çevre birimlerini (Monitör, Klavye, Fare, Kulaklık, Mikrofon) otomatik olarak algılar; yazılımla raporlanmayan parçaları (PSU, Sıvı Soğutma) ise `hardware-config.json` dosyasından okuyarak masaüstünüzün sağ üst köşesinde derli toplu bir şekilde sergiler.

---

## Özellikler

* **Otomatik Donanım Algılama:** WMI ve Windows PnP üzerinden CPU, GPU, Anakart, bellek kapasite/hızları, takılı SSD'ler ve monitör bilgileri dinamik taranır.
* **Manuel Bileşen Desteği:** PSU ve CPU soğutucu gibi sistemden modeli doğrudan okunamayan donanımlar için `hardware-config.json` desteği.
* **Modern Koyu Tema:** Çerçevesiz pencere (`WindowStyle=None`), yarı saydam koyu akrilik arka plan, özel minimalist kaydırma çubuğu ve donanım rozetleri.
* **Masaüstü Entegrasyonu:**
  * Ekranın sağ üst köşesinde görev çubuğunu ve pencereleri engellemeyecek şekilde konumlanır.
  * Sol tık ile masaüstünde istenen yere sürüklenebilir.
  * Sağ tık menüsü veya üst bardaki iğne simgesi ile konumu kilitlenebilir (`IsLocked`).
  * Sağ tık menüsünden doğrudan yapılandırma dosyasını açma (`hardware-config.json`), bilgileri yenileme ve kapatma seçenekleri.

---

## Gösterilen Donanımlar

1. **Sistem Bileşenleri:**
   * **İşlemci (CPU):** AMD Ryzen 7 5700X (8 Çekirdek / 16 İzlek)
   * **Ekran Kartı (GPU):** NVIDIA GeForce RTX 3060 (Ayrık Grafik Kartı)
   * **Anakart:** MSI B550M PRO-VDH WIFI (AM4 Platform)
   * **Bellek (RAM):** 48 GB DDR4 (3x 16 GB Kingston @ 3600 MHz)
2. **Depolama Birimleri:**
   * **SSD 1:** Kingston NV2 NVMe SSD (1 TB • M.2 PCIe 4.0)
   * **SSD 2:** SATA III High-Speed SSD (512 GB • 2.5" SSD)
3. **Çevre Birimleri:**
   * **Monitör:** MSI MAG 27CQ6F (Curved Gaming Display)
   * **Klavye:** Corsair K55 RGB PRO XT (RGB Gaming Keyboard)
   * **Fare:** ASUS ROG Harpe Ace Aim Lab Edition (Wireless Ultralight 54g)
   * **Kulaklık:** Razer Kraken Kitty V2 BT (7.1 Surround Sound)
   * **Mikrofon:** Razer Seiren V3 Chroma (USB Kondenser & Chroma RGB)
4. **Soğutma ve Güç:**
   * **İşlemci Soğutucu:** CORSAIR iCUE LINK TITAN 240 RX RGB AIO
   * **Güç Kaynağı (PSU):** MSI MPG A750GF White 750W 80+ Gold

---

## Yapılandırma (`hardware-config.json`)

```json
{
  "coolingAndPower": {
    "cpuCooler": "CORSAIR iCUE LINK TITAN 240 RX RGB AIO",
    "psu": "MSI MPG A750GF White 750W 80+ Gold"
  },
  "peripherals": {
    "keyboard": "Corsair K55 RGB PRO XT",
    "mouse": "ROG Harpe Ace Aim Lab Edition",
    "headset": "Razer Kraken Kitty V2 BT",
    "microphone": "Razer Seiren V3 Chroma"
  },
  "widget": {
    "opacity": 0.94,
    "offsetX": 24,
    "offsetY": 24,
    "width": 380,
    "height": 780,
    "alwaysOnTop": false,
    "isLocked": false
  }
}
```

---

## Derleme ve Çalıştırma

```powershell
# Proje dizinine geçiş
cd D:\projects\RigBadge

# Derleme
dotnet build -c Release

# Çalıştırma
& "D:\projects\RigBadge\bin\x64\Release\net10.0-windows\RigBadge.exe"
```
