# RigBadge

Windows masaüstü için doğrudan masaüstü katmanına gömülü, minimalist ve şık donanım gadgetı.

Uygulama penceresi hissi veren başlık çubukları, kapatma/ayar butonları ve kalın çerçeveler barındırmaz. Doğrudan masaüstü arka planı seviyesinde (`HWND_BOTTOM`) çalışarak, açık pencerelerin arkasında doğal bir masaüstü bilgi panosu olarak konumlanır.

---

## Özellikler

* **Masaüstüne Gömülü (Desktop-Pinned Gadget):**
  * Pencerelerin en alt katmanında (`HWND_BOTTOM`) durur; tarayıcı veya diğer uygulamalar açıldığında onların arkasında kalır.
  * `WS_EX_TOOLWINDOW` ve `WS_EX_NOACTIVATE` sayesinde görev çubuğunda veya Alt+Tab listesinde yer kaplamaz, odağı çalmaz.
* **Minimalist Gadget Tasarımı:**
  * Çerçevesiz, yarı saydam koyu zemin (`#0D1117` %88 opaklık), ince ayrım çizgileri ve sade iki sütunlu donanım satırları.
  * Buton veya pencere kontrolü bulunmaz; sağ tıklandığında hafif bir bağlam menüsü (Yenile, Ayarları Aç, Kapat) sunar.
  * Sol tık basılı tutularak masaüstünde istenen yere taşınabilir.
* **Otomatik Donanım Taraması:**
  * WMI ve Windows PnP üzerinden CPU, GPU, Anakart, RAM hızı/kapasitesi, takılı SSD'ler ve monitör bilgileri otomatik taranır.
  * PSU ve soğutucu gibi yazılımla doğrudan modeli okunamayan parçalar `hardware-config.json` dosyasından okunur.

---

## Gösterilen Donanımlar

1. **SİSTEM**
   * **İşlemci:** AMD Ryzen 7 5700X (`8C / 16T`)
   * **Ekran Kartı:** GeForce RTX 3060 (`PCIe`)
   * **Anakart:** MSI B550M PRO-VDH WIFI (`AM4`)
   * **RAM:** 48 GB Kingston (`3600 MHz`)
2. **DEPOLAMA**
   * **M.2 SSD:** Kingston NV2 NVMe (`1 TB M.2`)
   * **SATA SSD:** SATA III SSD (`512 GB`)
3. **ÇEVRE BİRİMLERİ**
   * **Monitör:** MSI MAG 27CQ6F (`180Hz 1440p`)
   * **Klavye:** Corsair K55 RGB PRO XT (`Klavye`)
   * **Fare:** ROG Harpe Ace Aim Lab (`Fare`)
   * **Kulaklık:** Razer Kraken Kitty V2 BT (`Kulaklık`)
   * **Mikrofon:** Razer Seiren V3 Chroma (`Mikrofon`)
4. **SOĞUTMA VE GÜÇ**
   * **Soğutucu:** Corsair iCUE LINK TITAN 240 RX (`240mm AIO`)
   * **PSU:** MSI MPG A750GF White (`750W Gold`)

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
    "mouse": "ROG Harpe Ace Aim Lab",
    "headset": "Razer Kraken Kitty V2 BT",
    "microphone": "Razer Seiren V3 Chroma"
  },
  "widget": {
    "opacity": 0.88,
    "offsetX": 20,
    "offsetY": 20,
    "width": 310,
    "alwaysOnTop": false,
    "isLocked": false
  }
}
```

---

## Derleme ve Çalıştırma

```powershell
cd D:\projects\RigBadge
dotnet build -c Release
& "D:\projects\RigBadge\bin\x64\Release\net10.0-windows\RigBadge.exe"
```
