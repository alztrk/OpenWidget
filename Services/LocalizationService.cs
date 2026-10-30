using System;
using System.Collections.Generic;

namespace OpenWidget.Services;

public static class LocalizationService
{
    private static string _currentLanguage = "tr";

    public static string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            var normalized = value?.ToLowerInvariant() == "en" ? "en" : "tr";
            if (_currentLanguage != normalized)
            {
                _currentLanguage = normalized;
                LanguageChanged?.Invoke();
            }
        }
    }

    public static event Action? LanguageChanged;

    private static readonly Dictionary<string, (string TR, string EN)> Strings = new()
    {
        // Headers & Sections
        ["OpenTitle"] = ("OpenWidget", "OpenWidget"),
        ["Telemetry"] = ("Telemetri", "Telemetry"),
        ["Market"] = ("Piyasa & Kripto", "Market & Crypto"),
        ["Hardware"] = ("Donanım", "Hardware"),
        ["QuickTools"] = ("Hızlı Araçlar", "Quick Tools"),

        // Status & Details
        ["Uptime"] = ("Açık", "Uptime"),
        ["GameMode"] = ("Oyun Modu", "Game Mode"),
        ["Status"] = ("Sistem Durumu", "System Status"),

        // Toolbar Buttons
        ["TaskMgr"] = ("Görev Yöneticisi", "Task Manager"),
        ["Volume"] = ("Ses Karıştırıcı", "Volume Mixer"),
        ["Notes"] = ("Hızlı Notlar", "Quick Notes"),
        ["Terminal"] = ("Terminal", "Terminal"),
        ["TaskMgrTooltip"] = ("Görev Yöneticisi (Ctrl+Shift+Esc)", "Task Manager (Ctrl+Shift+Esc)"),
        ["VolumeTooltip"] = ("Ses Düzeyi Karıştırıcısı", "Volume Mixer"),
        ["NotesTooltip"] = ("Masaüstü Hızlı Not Defteri", "Desktop Quick Scratchpad"),
        ["TerminalTooltip"] = ("PowerShell Terminali", "PowerShell Terminal"),

        // Context Menu
        ["WidgetSelection"] = ("Widget Seçimi", "Widget Selection"),
        ["MenuHeader"] = ("Saat & Sistem Durumu", "Time & Status Bar"),
        ["MenuWeather"] = ("Hava Durumu Detayları", "Weather Forecast"),
        ["MenuClipboard"] = ("Pano Geçmişi", "Clipboard History"),
        ["MenuTelemetry"] = ("Sistem Telemetrisi", "System Telemetry"),
        ["MenuFinance"] = ("Canlı Piyasa & Kripto", "Live Market & Crypto"),
        ["MenuMedia"] = ("Medya Oynatıcı", "Now Playing Media"),
        ["MenuHardware"] = ("Donanım Bileşenleri", "Hardware Specs"),
        ["MenuQuickActions"] = ("Hızlı Araçlar", "Quick Tools"),
        ["MenuCompactMode"] = ("Kompakt HUD Modu", "Compact HUD Mode"),
        ["MenuLockPosition"] = ("Konumu Kilitle", "Lock Position"),
        ["MenuPinToDesktop"] = ("Masaüstüne Sabitle (En Arkada)", "Pin to Desktop (Behind Windows)"),
        ["MenuAutoStart"] = ("Windows ile Birlikte Başlat", "Start with Windows"),
        ["MenuAlwaysOnTop"] = ("Her Zaman Üstte", "Always on Top"),
        ["MenuGameMode"] = ("Oyun Modu (Otomatik Rölanti)", "Game Mode (Auto Idle)"),
        ["MenuRefresh"] = ("Yenile", "Refresh"),
        ["MenuResetPos"] = ("Konumu Varsayılana Sıfırla", "Reset Position"),
        ["MenuSettings"] = ("Ayarlar", "Settings"),
        ["MenuPrices"] = ("Fiyatlar (config.json)", "Prices (config.json)"),
        ["MenuExit"] = ("Kapat", "Exit"),

        // Telemetry Labels
        ["Cpu"] = ("İşlemci (CPU)", "Processor (CPU)"),
        ["Gpu"] = ("Ekran Kartı (GPU)", "Graphics Card (GPU)"),
        ["Ram"] = ("Bellek (RAM)", "Memory (RAM)"),
        ["Vram"] = ("Video Belleği (VRAM)", "Video Memory (VRAM)"),
        ["Network"] = ("Ağ Trafiği", "Network Traffic"),
        ["Disk"] = ("Diski", "Drive"),
        ["Full"] = ("Dolu", "Used"),
        ["Free"] = ("Boş", "Free"),

        // Crypto & Currencies
        ["Dollar"] = ("Dolar (USD)", "US Dollar (USD)"),
        ["Euro"] = ("Euro (EUR)", "Euro (EUR)"),
        ["GramGold"] = ("Gram Altın", "Gram Gold"),
        ["QuarterGold"] = ("Çeyrek Altın", "Quarter Gold"),
        ["Bitcoin"] = ("Bitcoin (BTC)", "Bitcoin (BTC)"),
        ["Ethereum"] = ("Ethereum (ETH)", "Ethereum (ETH)"),

        // Weather Conditions
        ["WeatherTitle"] = ("Hava Durumu", "Weather"),
        ["FeelsLike"] = ("Hissedilen", "Feels Like"),
        ["Humidity"] = ("Nem", "Humidity"),
        ["Wind"] = ("Rüzgar", "Wind"),
        ["Precipitation"] = ("Yağış", "Precipitation"),
        ["Today"] = ("Bugün", "Today"),
        ["Tomorrow"] = ("Yarın", "Tomorrow"),
        ["ClipboardTitle"] = ("Pano Geçmişi", "Clipboard History"),
        ["ClipboardEmpty"] = ("Kopyalanan metinler burada listelenecek", "Copied texts will appear here"),
        ["ClipboardCopied"] = ("Kopyalandı!", "Copied!"),
        ["ClipboardClear"] = ("Temizle", "Clear"),
        ["WeatherClear"] = ("Açık", "Clear"),
        ["WeatherMainlyClear"] = ("Az Bulutlu", "Mainly Clear"),
        ["WeatherPartlyCloudy"] = ("Parçalı Bulutlu", "Partly Cloudy"),
        ["WeatherOvercast"] = ("Bulutlu", "Overcast"),
        ["WeatherFoggy"] = ("Sisli", "Foggy"),
        ["WeatherDrizzle"] = ("Çisenti", "Drizzle"),
        ["WeatherRain"] = ("Yağmurlu", "Rain"),
        ["WeatherSnow"] = ("Karlı", "Snow"),
        ["WeatherShowers"] = ("Sağanak", "Showers"),
        ["WeatherThunderstorm"] = ("Gök Gürültülü", "Thunderstorm"),

        // Settings Dialog
        ["SettingsTitle"] = ("OpenWidget • Ayarlar", "OpenWidget • Settings"),
        ["GeneralTab"] = ("Genel", "General"),
        ["WidgetsTab"] = ("Widgetlar", "Widgets"),
        ["PerformanceTab"] = ("Performans", "Performance"),
        ["LanguageLabel"] = ("Arayüz Dili", "Interface Language"),
        ["CityLabel"] = ("Hava Durumu Konumu", "Weather Location"),
        ["CityAutoPlaceholder"] = ("Otomatik (IP Konumu)", "Auto (IP Geolocation)"),
        ["CityHelp"] = ("Şehir adı girin veya boş bırakarak otomatik algılatın.", "Type a city name or leave blank for auto IP detection."),
        ["OpacityLabel"] = ("Pencere Şeffaflığı / Opaklık", "Window Transparency / Opacity"),
        ["AccentThemeLabel"] = ("Vurgu Rengi Teması", "Accent Color Theme"),
        ["WidgetsGroupLabel"] = ("Görüntülenecek Bölümler", "Visible Sections"),
        ["ShowHeaderLabel"] = ("Saat & Sistem Durumu Çubuğu", "Time & System Status Bar"),
        ["ShowWeatherLabel"] = ("Detaylı Hava Durumu Kartı", "Detailed Weather Forecast Card"),
        ["ShowClipboardLabel"] = ("Windows Pano Geçmişi Kartı", "Windows Clipboard History Card"),
        ["ShowTelemetryLabel"] = ("Sistem Telemetrisi (CPU / GPU / RAM)", "System Telemetry (CPU / GPU / RAM)"),
        ["ShowFinanceLabel"] = ("Canlı Piyasa & Kripto Kurları", "Live Market & Crypto Rates"),
        ["ShowMediaLabel"] = ("Medya Oynatıcı (Spotify / Tarayıcı)", "Media Player (Spotify / Browser)"),
        ["ShowHardwareLabel"] = ("Donanım Özellikleri & Fiyatlar", "Hardware Specs & Price Summary"),
        ["ShowQuickActionsLabel"] = ("Hızlı Araç Çubuğu (Kısayollar)", "Quick Tools Bar (Shortcuts)"),
        ["HardwareCategoryAccessories"] = ("Aksesuar", "Accessories"),
        ["HardwareCategoryCoreSystem"] = ("Sistem", "System"),
        ["HardwareCategoryStorage"] = ("Depolama", "Storage"),
        ["HardwareCategoryPeripherals"] = ("Çevre Birimleri", "Peripherals"),
        ["HardwareCategoryCoolingAndPower"] = ("Soğutma ve Güç", "Cooling & Power"),
        ["HardwareSourceDetected"] = ("Mevcut", "Available"),
        ["HardwareSourceConfig"] = ("Config", "Config"),
        ["HardwareCategoriesLabel"] = ("Donanım kategorileri", "Hardware categories"),
        ["CurrentHardwareLabel"] = ("Mevcut donanım", "Current hardware"),
        ["CurrentHardwareHelp"] = ("Algılanan ve config.json içinde kayıtlı donanımlar", "Detected hardware and items saved in config.json"),
        ["AddHardwareLabel"] = ("Donanım ekle", "Add hardware"),
        ["AddHardwareHelp"] = ("Şimdilik yalnızca Amazon.com.tr bağlantıları desteklenir.", "Only Amazon.com.tr links are supported for now."),
        ["UpdateVersionLabel"] = ("OpenWidget v{0}", "OpenWidget v{0}"),
        ["PerformanceGroupLabel"] = ("Sistem Seçenekleri", "System Options"),
        ["AutoStartSettingLabel"] = ("Windows ile Birlikte Başlat", "Start Automatically with Windows"),
        ["AlwaysOnTopSettingLabel"] = ("Her Zaman Üstte Tut", "Keep Window Always on Top"),
        ["LockPositionSettingLabel"] = ("Widget Konumunu Kilitle (Sürüklemeyi Engelle)", "Lock Position (Prevent Dragging)"),
        ["PinToDesktopSettingLabel"] = ("Masaüstü Arkasına Sabitle (En Altta)", "Pin to Desktop (Keep Behind Windows)"),
        ["CompactModeSettingLabel"] = ("Kompakt HUD Modu (Yalnızca Üst Şerit)", "Compact HUD Mode (Top Bar Only)"),
        ["GameModeSettingLabel"] = ("Oyun Modu (Tam Ekran Oyunlarda Rölantiye Al)", "Game Mode (Throttle Polling in Fullscreen Games)"),
        ["SaveAndApplyBtn"] = ("Kaydet ve Uygula", "Save & Apply"),
        ["ResetDefaultsBtn"] = ("Varsayılana Sıfırla", "Reset Defaults"),
        ["CloseBtn"] = ("Kapat", "Close"),
        ["SavedNotification"] = ("Ayarlar başarıyla uygulandı.", "Settings successfully applied.")
    };

    public static string Get(string key)
    {
        if (Strings.TryGetValue(key, out var val))
        {
            return _currentLanguage.Equals("en", StringComparison.OrdinalIgnoreCase) ? val.EN : val.TR;
        }
        return key;
    }
}