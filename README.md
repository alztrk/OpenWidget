# OpenWidget

OpenWidget, Windows 10 ve Windows 11 için geliştirilmiş, masaüstünde doğrudan çalışan, modern akrilik arayüze sahip bağımsız bir sistem ve bilgi widget'ıdır. Sistem telemetrisi, hava durumu, kalıcı pano geçmişi, çoklu oturumlu medya denetimi, finans verileri ve donanım envanterini tek bir panelde bir araya getirir.

---

## Proje Yapısı

* `Views/Widgets/`: Ana panelde gösterilen widget görünümleri.
* `Views/Windows/`: Uygulama içindeki bağımsız pencere görünümleri.
* `Resources/Styles/`: Paylaşılan WPF stilleri ve tema kaynakları.
* `Models/`: Uygulama veri modelleri.
* `Services/`: Donanım, sistem, ağ ve uygulama servisleri.
* `Converters/`: XAML değer dönüştürücüleri.
* `Utils/`: Ortak yardımcı sınıflar.
* `tests/`: Birim testleri.

---

## Genel Özellikler

* **Masaüstü Entegrasyonu:** Diğer pencerelerin üzerine çıkmadan arka planda masaüstü düzeyinde çalışır (`Topmost = false`, `WS_EX_TOOLWINDOW`). Görev çubuğunda simge oluşturmaz ve odak çalmaz (`WS_EX_NOACTIVATE`).
* **Global Kısayol Tuşu (`Ctrl + Shift + W`):** Herhangi bir tam ekran uygulama veya oyun çalışırken widget'ı tek tuşla ekranda gizler ya da tekrar görünür kılar.
* **Çoklu Monitör ve Dinamik DPI Uyumluluğu:** Çoklu ekran kurulumlarında pencerenin hangi monitörde olduğunu Win32 `MonitorFromPoint` ile algılar, `WM_DPICHANGED` mesajlarını yakalayarak çözünürlük veya ölçek değiştiğinde ekrandan taşmayı önler.
* **Tek Kopya (Single Instance) Koruması:** Sistem genelinde adlandırılmış `Mutex` koruması sayesinde uygulamanın kazara birden fazla kez açılması ve kaynak çakışmaları engellenir.
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
   * CPU çekirdek sayısı ve anlık saat hızı (GHz) tespiti.
   * GPU ve VRAM anlık kullanım oranları ile GPU sıcaklığı.
   * RAM kullanım oranı ve boş bellek miktarı.
   * Disk (NVMe/SATA SSD) ve ağ (anlık gecikme ping ve indirme/yükleme hızları) etkinlik göstergeleri.

4. **Canlı Piyasa ve Finans (Finance)**
   * Dolar (USD/TRY), Euro (EUR/TRY), Gram Altın (GA/TRY), Çeyrek Altın.
   * Kripto para birimleri (Bitcoin, Ethereum).
   * 60 saniyelik yerel önbellek ile anlık piyasa takibi; ağ yoksa sahte veri yerine açık yüklenme/çevrimdışı durumu.

5. **Medya Oynatıcı ve Ses Denetimi (Media)**
   * Windows Media Session (SMTC) entegrasyonu (Spotify, Chrome, Firefox, VLC vb.).
   * Çoklu oturum yöneticisi: Birden fazla oynatıcı açıkken önceki/sonraki butonlarıyla oturumlar arasında anında geçiş.
   * Şarkı/video başlığı, sanatçı bilgisi ve albüm kapağı.
   * Oynat, duraklat, parça geçiş kontrolleri ve gerçek zamanlı şarkı süresi çubuğu.
   * Oynatılan tarayıcıya/uygulamaya özel bağımsız ses denetimi kaydırıcısı ve sessize alma.

6. **Kalıcı Pano Geçmişi (Clipboard)**
   * Windows Pano Olay Dinleyicisi (`WM_CLIPBOARDUPDATE`).
   * Kopyalanan son metinlerin otomatik listelenmesi ve `%APPDATA%\OpenWidget\clipboard-history.json` üzerinde kalıcı saklanması.
   * Metinlerin yanı sıra kopyalanan görseller için küçük resim (thumbnail) ve kopyalanan dosyalar için dosya adı/ikonu desteği.
   * Tek tıkla ilgili metni panoya yeniden kopyalama ve pano temizleme butonu.

7. **Donanım Bileşenleri ve Fiyatlandırma (Hardware)**
   * WMI ve Windows API tabanlı donanım tespiti (CPU, GPU, Anakart, RAM, SSD, Çevre Birimleri).
   * Günlük takip edilen referans parça piyasa fiyatları ve toplam sistem değeri özeti.

8. **Hızlı Araçlar (Quick Actions)**
   * Görev Yöneticisi, Aygıt Yöneticisi ve Ağ Ayarları kısayolları.
   * Asenkron kilit korumalı kalıcı hızlı not defteri (Scratchpad).
   * Windows ses karıştırıcısı hızlı erişimi.

---

## Sistem Gereksinimleri

* **İşletim Sistemi:** Windows 10 (Sürüm 2004 / Derleme 19041 veya üstü) ya da Windows 11 (x64)
* **Çalışma Zamanı:** .NET 10.0 Windows Masaüstü Çalışma Zamanı (Taşınabilir derlemede dahili gelir)

---

## Kullanım

### GitHub Release ile Çalıştırma

1. [Releases sayfasından](https://github.com/alztrk/OpenWidget/releases) en son `OpenWidget-windows-x64.zip` dosyasını indirin.
2. ZIP dosyasını Windows üzerinde boş bir klasöre çıkarın.
3. `OpenWidget.exe` dosyasını çalıştırın. Kurulum gerekmez; uygulama taşınabilir olarak çalışır.
4. Widget görünmüyorsa sistem tepsisinde ayrı bir simge aramayın; pencere masaüstünde sağ üstte açılır. Varsayılan global kısayol `Ctrl + Shift + W` ile görünürlüğü açıp kapatabilirsiniz.
5. Widget'a sağ tıklayarak kartları, konumu, tema ayarlarını, otomatik başlatmayı ve masaüstü davranışını değiştirebilirsiniz.

Windows SmartScreen uyarısı yayıncı sertifikası bulunmadığı için gösterilebilir. Dosyayı yalnızca bu depodaki resmi [Releases](https://github.com/alztrk/OpenWidget/releases) sayfasından indirdiğinizden emin olun.

### Kaynaktan Çalıştırma

`.NET 10 SDK` yüklü bir Windows ortamında:

```powershell
dotnet run --project OpenWidget.csproj
```

Bağımsız taşınabilir `.exe` üretmek için:

```powershell
powershell -ExecutionPolicy Bypass -File ".\scripts\publish-portable.ps1"
```

Çıktı `dist\OpenWidget.exe` konumuna yazılır.

---

## Yapılandırma ve Veri Dosyaları

* `%APPDATA%\OpenWidget\hardware-config.json`: Kullanıcı tercihleri, aktif kartlar, tema, şehir ve konum ayarları.
* `%APPDATA%\OpenWidget\clipboard-history.json`: Kalıcı pano geçmişi kayıtları.
* `%APPDATA%\OpenWidget\scratchpad.txt`: Hızlı not defteri içeriği.
* `%APPDATA%\OpenWidget\crash.log`: Olası beklenmeyen çökme ve hata kayıtları.
* `hardware-prices.json`: Donanım bileşenlerinin takip edilen referans fiyat listesi.
* `finance-cache.json`: Finans verilerinin yerel önbelleği.
* `weather-cache.json`: Hava durumu verilerinin son güncel önbelleği.

---

## Derleme, Test ve Paketleme

### Birim Testlerini Çalıştırma

Projede yapılandırma, şehir dizini, fiyatlandırma servisi, pano modelleri, pencere yerleşimi ve sürüm denetimi için 23 adet xUnit birim testi yer alır:

```powershell
dotnet test OpenWidget.sln
```

### Geliştirme Derlemesi

```powershell
dotnet build OpenWidget.sln -c Release
```

### Tek Dosya Taşınabilir (Self-Contained Portable) Paketleme

Tüm bağımlılıkları içeren, kuruluma ihtiyaç duymayan tek bir `.exe` üretmek için:

```powershell
powershell -ExecutionPolicy Bypass -File "D:\projects\OpenWidget\scripts\publish-portable.ps1"
```

Çıktı `D:\projects\OpenWidget\dist\OpenWidget.exe` adresinde oluşturulur.

---

## CI / CD İş Akışı

Depo ana dalına (`master` / `main`) yapılan her gönderimde (push) veya çekme isteğinde (PR) [GitHub Actions](.github/workflows/build.yml) devreye girerek:
1. .NET 10 ortamını hazırlar.
2. Bağımlılıkları geri yükler ve çözümü derler.
3. Birim testlerini (`dotnet test`) çalıştırır.
4. Tek dosya taşınabilir `OpenWidget.exe` dosyasını derleyip yapay nesne (artifact) olarak sunar.

---

## Lisans

Bu proje [MIT Lisansı](LICENSE) altında lisanslanmıştır.
