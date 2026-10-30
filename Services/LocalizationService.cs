using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace OpenWidget.Services;

public static class LocalizationService
{
    public sealed record LanguageOption(string Code, string NativeName);

    public static IReadOnlyList<LanguageOption> SupportedLanguages { get; } =
    [
        new("tr", "Türkçe"),
        new("en", "English"),
        new("de", "Deutsch"),
        new("fr", "Français"),
        new("es", "Español"),
        new("it", "Italiano"),
        new("pt", "Português"),
        new("ja", "日本語"),
        new("ru", "Русский")
    ];

    private static string _currentLanguage = "tr";

    public static string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            var normalized = NormalizeLanguage(value);
            if (_currentLanguage != normalized)
            {
                _currentLanguage = normalized;
                LanguageChanged?.Invoke();
            }
        }
    }

    public static event Action? LanguageChanged;

    public static string NormalizeLanguage(string? language)
    {
        var normalized = language?.Trim().ToLowerInvariant();
        return SupportedLanguages.Any(option => option.Code == normalized) ? normalized! : "en";
    }

    public static string GetLanguageName(string code) =>
        SupportedLanguages.FirstOrDefault(option => option.Code == NormalizeLanguage(code))?.NativeName
        ?? code;

    public static CultureInfo GetCulture()
    {
        var cultureName = NormalizeLanguage(_currentLanguage) switch
        {
            "tr" => "tr-TR",
            "de" => "de-DE",
            "fr" => "fr-FR",
            "es" => "es-ES",
            "it" => "it-IT",
            "pt" => "pt-PT",
            "ja" => "ja-JP",
            "ru" => "ru-RU",
            _ => "en-US"
        };

        return CultureInfo.GetCultureInfo(cultureName);
    }

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
        ["Cancel"] = ("Vazgeç", "Cancel"),
        ["AutoDetectIp"] = ("Otomatik Algıla (IP)", "Auto Detect (IP)"),
        ["InvalidAmazonProduct"] = ("Ürün adı ve geçerli bir Amazon.com.tr bağlantısı girin.", "Enter a product name and a valid Amazon.com.tr link."),
        ["InvalidAmazonLink"] = ("Geçerli bir Amazon.com.tr bağlantısı girin.", "Enter a valid Amazon.com.tr link."),
        ["DownloadUpdate"] = ("İndir ↗", "Download ↗"),
        ["UpdateInProgress"] = ("Güncelleniyor...", "Updating..."),
        ["UpdateAvailable"] = ("Yeni sürüm mevcut.", "A new version is available."),
        ["UpdateUnavailable"] = ("Güncelleme bilgisi alınamadı.", "Update information is unavailable."),
        ["CheckingUpdates"] = ("Güncellemeler denetleniyor...", "Checking for updates..."),
        ["LatestVersionAvailable"] = ("Yeni sürüm mevcut: v{0}!", "New version available: v{0}!"),
        ["LatestVersionInstalled"] = ("En güncel sürümü kullanıyorsunuz.", "You have the latest version installed."),
        ["UpdateCheckFailed"] = ("Bağlantı kurulamadı: {0}", "Check failed: {0}"),
        ["Offline"] = ("Çevrimdışı", "Offline"),
        ["WeatherUpdating"] = ("Hava durumu...", "Weather updating..."),

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
        ["ClipboardItem"] = ("öğe", "item"),
        ["ClipboardItems"] = ("öğe", "items"),
        ["Updating"] = ("Güncelleniyor...", "Updating..."),
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
        ["PriceStatusLive"] = ("Güncel", "Live"),
        ["PriceStatusStale"] = ("Eski fiyat", "Stale"),
        ["PriceStatusEstimated"] = ("Tahmini", "Estimated"),
        ["PriceStatusManual"] = ("Manuel", "Manual"),
        ["PriceStatusUnavailable"] = ("Fiyat yok", "Unavailable"),
        ["HardwareCategoriesLabel"] = ("Donanım kategorileri", "Hardware categories"),
        ["CurrentHardwareLabel"] = ("Mevcut donanım", "Current hardware"),
        ["CurrentHardwareHelp"] = ("Algılanan ve config.json içinde kayıtlı donanımlar", "Detected hardware and items saved in config.json"),
        ["AddHardwareLabel"] = ("Donanım ekle", "Add hardware"),
        ["AddHardwareHelp"] = ("Şimdilik yalnızca Amazon.com.tr bağlantıları desteklenir.", "Only Amazon.com.tr links are supported for now."),
        ["HardwareNameLabel"] = ("Ürün adı", "Product name"),
        ["HardwareCategoryLabel"] = ("Kategori", "Category"),
        ["HardwareUrlLabel"] = ("Amazon.com.tr bağlantısı", "Amazon.com.tr link"),
        ["HardwarePriceLabel"] = ("Fiyat (TL)", "Price (TRY)"),
        ["HardwareUpdateLinkButton"] = ("Güncelle", "Update"),
        ["HardwareRemoveButton"] = ("Sil", "Remove"),
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

    private static readonly Dictionary<string, Dictionary<string, string>> AdditionalTranslations = new()
    {
        ["de"] = new()
        {
            ["Hardware"] = "Hardware", ["QuickTools"] = "Schnellwerkzeuge", ["GameMode"] = "Spielmodus",
            ["SettingsTitle"] = "OpenWidget • Einstellungen", ["GeneralTab"] = "Allgemein",
            ["WidgetsTab"] = "Widgets", ["PerformanceTab"] = "Leistung", ["LanguageLabel"] = "Oberflächensprache",
            ["CityLabel"] = "Wetterstandort", ["SaveAndApplyBtn"] = "Speichern und anwenden",
            ["ResetDefaultsBtn"] = "Standardwerte", ["CloseBtn"] = "Schließen", ["Cancel"] = "Abbrechen",
            ["MenuSettings"] = "Einstellungen", ["MenuRefresh"] = "Aktualisieren", ["MenuExit"] = "Beenden"
        },
        ["fr"] = new()
        {
            ["Hardware"] = "Matériel", ["QuickTools"] = "Outils rapides", ["GameMode"] = "Mode jeu",
            ["SettingsTitle"] = "OpenWidget • Paramètres", ["GeneralTab"] = "Général",
            ["WidgetsTab"] = "Widgets", ["PerformanceTab"] = "Performances", ["LanguageLabel"] = "Langue de l’interface",
            ["CityLabel"] = "Lieu météo", ["SaveAndApplyBtn"] = "Enregistrer et appliquer",
            ["ResetDefaultsBtn"] = "Valeurs par défaut", ["CloseBtn"] = "Fermer", ["Cancel"] = "Annuler",
            ["MenuSettings"] = "Paramètres", ["MenuRefresh"] = "Actualiser", ["MenuExit"] = "Quitter"
        },
        ["es"] = new()
        {
            ["Hardware"] = "Hardware", ["QuickTools"] = "Herramientas rápidas", ["GameMode"] = "Modo juego",
            ["SettingsTitle"] = "OpenWidget • Ajustes", ["GeneralTab"] = "General",
            ["WidgetsTab"] = "Widgets", ["PerformanceTab"] = "Rendimiento", ["LanguageLabel"] = "Idioma de la interfaz",
            ["CityLabel"] = "Ubicación meteorológica", ["SaveAndApplyBtn"] = "Guardar y aplicar",
            ["ResetDefaultsBtn"] = "Valores predeterminados", ["CloseBtn"] = "Cerrar", ["Cancel"] = "Cancelar",
            ["MenuSettings"] = "Ajustes", ["MenuRefresh"] = "Actualizar", ["MenuExit"] = "Salir"
        },
        ["it"] = new()
        {
            ["Hardware"] = "Hardware", ["QuickTools"] = "Strumenti rapidi", ["GameMode"] = "Modalità gioco",
            ["SettingsTitle"] = "OpenWidget • Impostazioni", ["GeneralTab"] = "Generale",
            ["WidgetsTab"] = "Widget", ["PerformanceTab"] = "Prestazioni", ["LanguageLabel"] = "Lingua dell’interfaccia",
            ["CityLabel"] = "Posizione meteo", ["SaveAndApplyBtn"] = "Salva e applica",
            ["ResetDefaultsBtn"] = "Valori predefiniti", ["CloseBtn"] = "Chiudi", ["Cancel"] = "Annulla",
            ["MenuSettings"] = "Impostazioni", ["MenuRefresh"] = "Aggiorna", ["MenuExit"] = "Esci"
        },
        ["pt"] = new()
        {
            ["Hardware"] = "Hardware", ["QuickTools"] = "Ferramentas rápidas", ["GameMode"] = "Modo de jogo",
            ["SettingsTitle"] = "OpenWidget • Definições", ["GeneralTab"] = "Geral",
            ["WidgetsTab"] = "Widgets", ["PerformanceTab"] = "Desempenho", ["LanguageLabel"] = "Idioma da interface",
            ["CityLabel"] = "Localização meteorológica", ["SaveAndApplyBtn"] = "Guardar e aplicar",
            ["ResetDefaultsBtn"] = "Predefinições", ["CloseBtn"] = "Fechar", ["Cancel"] = "Cancelar",
            ["MenuSettings"] = "Definições", ["MenuRefresh"] = "Atualizar", ["MenuExit"] = "Sair"
        },
        ["ja"] = new()
        {
            ["Hardware"] = "ハードウェア", ["QuickTools"] = "クイックツール", ["GameMode"] = "ゲームモード",
            ["SettingsTitle"] = "OpenWidget • 設定", ["GeneralTab"] = "一般",
            ["WidgetsTab"] = "ウィジェット", ["PerformanceTab"] = "パフォーマンス", ["LanguageLabel"] = "インターフェース言語",
            ["CityLabel"] = "天気の場所", ["SaveAndApplyBtn"] = "保存して適用",
            ["ResetDefaultsBtn"] = "デフォルトに戻す", ["CloseBtn"] = "閉じる", ["Cancel"] = "キャンセル",
            ["MenuSettings"] = "設定", ["MenuRefresh"] = "更新", ["MenuExit"] = "終了"
        },
        ["ru"] = new()
        {
            ["OpenTitle"] = "OpenWidget", ["Telemetry"] = "Телеметрия", ["Market"] = "Рынок и крипто",
            ["Hardware"] = "Оборудование", ["QuickTools"] = "Быстрые инструменты",
            ["Uptime"] = "Работает", ["GameMode"] = "Игровой режим", ["Status"] = "Состояние системы",
            ["TaskMgr"] = "Диспетчер задач", ["Volume"] = "Микшер громкости", ["Notes"] = "Быстрые заметки",
            ["Terminal"] = "Терминал", ["SettingsTitle"] = "OpenWidget • Настройки",
            ["GeneralTab"] = "Общие", ["WidgetsTab"] = "Виджеты", ["PerformanceTab"] = "Производительность",
            ["LanguageLabel"] = "Язык интерфейса", ["CityLabel"] = "Местоположение погоды",
            ["CityHelp"] = "Введите город или оставьте поле пустым для автоматического определения.",
            ["OpacityLabel"] = "Прозрачность / непрозрачность окна", ["AccentThemeLabel"] = "Цвет темы",
            ["MenuHeader"] = "Время и состояние", ["MenuWeather"] = "Прогноз погоды",
            ["MenuClipboard"] = "История буфера обмена", ["MenuTelemetry"] = "Системная телеметрия",
            ["MenuFinance"] = "Рынок и крипто", ["MenuMedia"] = "Медиаплеер",
            ["MenuHardware"] = "Компоненты оборудования", ["MenuQuickActions"] = "Быстрые инструменты",
            ["MenuCompactMode"] = "Компактный режим HUD", ["MenuLockPosition"] = "Заблокировать положение",
            ["MenuPinToDesktop"] = "Закрепить на рабочем столе", ["MenuAutoStart"] = "Запускать с Windows",
            ["MenuAlwaysOnTop"] = "Поверх окон", ["MenuGameMode"] = "Игровой режим",
            ["MenuRefresh"] = "Обновить", ["MenuResetPos"] = "Сбросить положение",
            ["MenuSettings"] = "Настройки", ["MenuPrices"] = "Цены (config.json)", ["MenuExit"] = "Выход",
            ["Cancel"] = "Отмена", ["AutoDetectIp"] = "Определять автоматически (IP)",
            ["CheckingUpdates"] = "Проверка обновлений...", ["LatestVersionInstalled"] = "Установлена последняя версия",
            ["DownloadUpdate"] = "Скачать ↗", ["Offline"] = "Нет подключения",
            ["WeatherUpdating"] = "Обновление погоды...", ["Updating"] = "Обновление...",
            ["WeatherTitle"] = "Погода", ["FeelsLike"] = "Ощущается как", ["Humidity"] = "Влажность",
            ["Wind"] = "Ветер", ["Precipitation"] = "Осадки", ["Today"] = "Сегодня", ["Tomorrow"] = "Завтра",
            ["ClipboardTitle"] = "История буфера обмена", ["ClipboardEmpty"] = "Скопированный текст появится здесь",
            ["ClipboardClear"] = "Очистить", ["ClipboardItem"] = "элемент", ["ClipboardItems"] = "элементов",
            ["WeatherClear"] = "Ясно", ["WeatherRain"] = "Дождь", ["WeatherSnow"] = "Снег",
            ["SaveAndApplyBtn"] = "Сохранить и применить", ["ResetDefaultsBtn"] = "Сбросить настройки",
            ["CloseBtn"] = "Закрыть", ["HardwareCategoriesLabel"] = "Категории оборудования",
            ["CurrentHardwareLabel"] = "Текущее оборудование", ["AddHardwareLabel"] = "Добавить оборудование",
            ["HardwareNameLabel"] = "Название товара", ["HardwareCategoryLabel"] = "Категория",
            ["HardwarePriceLabel"] = "Цена", ["HardwareUpdateLinkButton"] = "Обновить",
            ["HardwareRemoveButton"] = "Удалить"
        }
    };

    public static string Get(string key)
    {
        if (AdditionalTranslations.TryGetValue(_currentLanguage, out var translations) &&
            translations.TryGetValue(key, out var translation))
        {
            return translation;
        }

        if (Strings.TryGetValue(key, out var val))
        {
            return _currentLanguage.Equals("tr", StringComparison.OrdinalIgnoreCase) ? val.TR : val.EN;
        }
        return key;
    }
}