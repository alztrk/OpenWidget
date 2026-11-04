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
    private string? _invalidImageBase64;

    [JsonPropertyName("ImageBase64")]
    [JsonConverter(typeof(Base64ByteArrayJsonConverter))]
    public byte[]? ImageData { get; set; }

    [JsonIgnore]
    public string? ImageBase64
    {
        get => ImageData != null ? Convert.ToBase64String(ImageData) : _invalidImageBase64;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                ImageData = null;
                _invalidImageBase64 = null;
                return;
            }

            var bytes = new byte[(value.Length / 4 * 3) + 2];
            if (Convert.TryFromBase64String(value, bytes, out var bytesWritten))
            {
                Array.Resize(ref bytes, bytesWritten);
                ImageData = bytes;
                _invalidImageBase64 = null;
            }
            else
            {
                ImageData = null;
                _invalidImageBase64 = value;
            }
        }
    }
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
    public bool IsImage => ItemType == "Image"
        && ((ImageData?.Length ?? 0) > 0 || !string.IsNullOrEmpty(_invalidImageBase64));

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
