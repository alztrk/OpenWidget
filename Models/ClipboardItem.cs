using System;
using System.ComponentModel;

namespace OpenWidget.Models;

public class ClipboardItem : INotifyPropertyChanged
{
    private bool _isJustCopied;

    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string FullText { get; set; } = string.Empty;
    public string PreviewText { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string FormattedTime => Timestamp.ToString("HH:mm");

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
