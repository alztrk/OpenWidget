# OpenWidget

OpenWidget, Windows 10 ve Windows 11 için geliştirilmiş, masaüstünde doğrudan çalışan, modern akrilik arayüze sahip bağımsız bir sistem ve bilgi widget'ıdır. Sistem telemetrisi, hava durumu, pano geçmişi, medya denetimi, finans verileri ve donanım envanterini tek bir panelde bir araya getirir.

---

## Genel Özellikler

* **Masaüstü Entegrasyonu:** Diğer pencerelerin üzerine çıkmadan arka planda masaüstü düzeyinde çalışır (`Topmost = false`, `WS_EX_TOOLWINDOW`). Görev çubuğunda simge oluşturmaz ve odak çalmaz (`WS_EX_NOACTIVATE`).
* **Dinamik Çözünürlük ve Taşma Koruması:** Ekran çalışma alanı yüksekliğine göre otomatik azami boyut hesaplar. Çok sayıda kart açık olduğunda modern dikey kaydırma çubuğu devreye girer.
* **Kompakt HUD Modu:** Tek satırlık mini çubuk görünümüne geçerek ekran alanını korur.
* **Oyun Modu:** Tam ekran oyun veya uygulama algılandığında arka plan telemetri sorgularını otomatik olarak rölantiye alır ve RAM kullanımını düşürür.
* **Kalıcı Yapılandırma:** Ayarlar hem `%APPDATA%\OpenWidget\hardware-config.json` altında hem de taşınabilir dizinde senkronize saklanır; derleme veya yeniden başlatmalarda sıfırlanmaz.

---

## Modüller ve Kartlar

1. **Saat ve Sistem Durumu (Header)**
   * Dijital saat ve tarih göstergesi.
   * Windows çalışma süresi (Uptime).
   * Oyun modu bildirim rozeti.

2. **Detaylı Hava Durumu (Weather)**
   * Open-Meteo API entegrasyonu (API anahtarı gerektirmez).
   * Anlık sıcaklık, hissedilen sıcaklık ve durum ikonu.
   * Nem oranı, rüzgar hızı ve yağış miktarı metrikleri.
   * 3 günlük hava durumu tahmini.
   * Ayarlar üzerinden dünya çapında şehir seçimi veya IP tabanlı otomatik konum.

3. **Sistem Telemetrisi (Telemetry)**
   * CPU, GPU ve RAM anlık kullanım oranları.
   * Donanım sıcaklıkları ve fan devirleri.
   * Disk ve ağ etkinlik göstergeleri.

4. **Canlı Piyasa ve Finans (Finance)**
   * Dolar (USD/TRY), Euro (EUR/TRY), Gram Altın (GA/TRY).
   * Borsa İstanbul (BIST 100).
   * Kripto para birimleri (Bitcoin, Ethereum).
   * 60 saniyelik yerel önbellek ile anlık piyasa takibi.

5. **Medya Oynatıcı ve Ses Denetimi (Media)**
   * Windows Media Session (SMTC) entegrasyonu (Spotify, Chrome, Firefox, VLC vb.).
   * Şarkı/video başlığı, sanatçı bilgisi ve albüm kapağı.
   * Oynat, duraklat, önceki/sonraki parça kontrolleri.
   * Gerçek zamanlı şarkı süresi ve arama (seek) çubuğu.
   * Oynatılan tarayıcıya/uygulamaya özel bağımsız ses denetimi kaydırıcısı ve sessize alma.

6. **Pano Geçmişi (Clipboard)**
   * Windows Pano Olay Dinleyicisi (`WM_CLIPBOARDUPDATE`).
   * Kopyalanan son metinlerin otomatik listelenmesi.
   * Tek tıkla ilgili metni panoya yeniden yükleme.
   * Pano temizleme butonu ve öğe sayacı.

7. **Donanım Bileşenleri ve Fiyatlandırma (Hardware)**
   * WMI ve Windows API tabanlı donanım tespiti (CPU, GPU, Anakart, RAM, SSD, Çevre Birimleri).
   * Günlük takip edilen parça piyasa fiyatları ve toplam sistem değeri özeti.

8. **Hızlı Araçlar (Quick Actions)**
   * Görev Yöneticisi, Aygıt Yöneticisi ve Ağ Ayarları kısayolları.
   * Tek tıkla çalışma kümesi RAM temizleme (`EmptyWorkingSet`).
   * Windows ses karıştırıcısı hızlı erişimi.

---

## Sistem Gereksinimleri

* **İşletim Sistemi:** Windows 10 (Sürüm 2004 / Derleme 19041 veya üstü) ya da Windows 11 (x64)
* **Çalışma Zamanı:** .NET 10.0 Windows Masaüstü Çalışma Zamanı (Taşınabilir derlemede dahili gelir)

---

## Yapılandırma Dosyaları

* `%APPDATA%\OpenWidget\hardware-config.json`: Kullanıcı tercihleri, aktif kartlar, tema, şehir ve konum ayarları.
* `hardware-prices.json`: Donanım bileşenlerinin takip edilen referans fiyat listesi.
* `finance-cache.json`: Finans verilerinin çevrimdışı ve hızlı yükleme önbelleği.
* `weather-cache.json`: Hava durumu verilerinin son güncel önbelleği.

---

## Derleme ve Yayınlama

### Geliştirme Derlemesi

```powershell
cd D:\projects\OpenWidget
dotnet build -c Release
```

### Tek Dosya Taşınabilir (Self-Contained Portable) Paketleme

Tüm bağımlılıkları içeren, kuruluma ihtiyaç duymayan tek bir `.exe` üretmek için:

```powershell
powershell -ExecutionPolicy Bypass -File "D:\projects\OpenWidget\scripts\publish-portable.ps1"
```

Çıktı `D:\projects\OpenWidget\dist\OpenWidget.exe` adresinde oluşturulur.

---

## Lisans

Bu proje [MIT Lisansı](LICENSE) altında lisanslanmıştır.
