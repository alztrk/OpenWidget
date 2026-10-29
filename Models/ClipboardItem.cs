using System;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace OpenWidget.Models;

public class ClipboardItem : INotifyPropertyChanged
{
    private bool _isJustCopied;

    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string FullText { get; set; } = string.Empty;
    public string PreviewText { get; set; } = string.Empty;
    public string ItemType { get; set; } = "Text"; // "Text", "Image", "File"
    public string? ImageBase64 { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string FormattedTime => Timestamp.ToString("HH:mm");

    [JsonIgnore]
    public string IconKey => ItemType switch
    {
        "Image" => "GeoMonitor",
        "File" => "GeoStorage",
        _ => "GeoCopy"
    };

    [JsonIgnore]
    public bool IsImage => ItemType == "Image" && !string.IsNullOrEmpty(ImageBase64);

    [JsonIgnore]
    public bool IsJustCopied
    {
        get => _isJustCopied;
        set
        {
            if (_isJustCopied != value)
            {
                _isJustCopied = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsJustCopied)));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
