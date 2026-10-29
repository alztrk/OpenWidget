using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using OpenWidget.Models;

namespace OpenWidget.Services;

public class ClipboardService
{
    public const int WM_CLIPBOARDUPDATE = 0x031D;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

    private IntPtr _hwnd = IntPtr.Zero;
    private bool _isSelfCopy = false;

    public ObservableCollection<ClipboardItem> Items { get; } = new();

    public void Initialize(IntPtr hwnd)
    {
        _hwnd = hwnd;
        if (_hwnd != IntPtr.Zero)
        {
            try
            {
                AddClipboardFormatListener(_hwnd);
            }
            catch { }
        }

        // Capture whatever is currently on the clipboard upon launch
        ReadCurrentClipboardInitial();
    }

    public void Cleanup(IntPtr hwnd)
    {
        var target = hwnd != IntPtr.Zero ? hwnd : _hwnd;
        if (target != IntPtr.Zero)
        {
            try
            {
                RemoveClipboardFormatListener(target);
            }
            catch { }
        }
    }

    public async void HandleClipboardUpdate()
    {
        if (_isSelfCopy)
        {
            _isSelfCopy = false;
            return;
        }

        // Small delay to allow the source application to close clipboard handle
        await Task.Delay(80);

        for (int retry = 0; retry < 3; retry++)
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    var text = Clipboard.GetText();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        AddOrPromoteItem(text.Trim());
                        break;
                    }
                }
            }
            catch (COMException)
            {
                await Task.Delay(60);
            }
            catch
            {
                break;
            }
        }
    }

    private void ReadCurrentClipboardInitial()
    {
        try
        {
            if (Clipboard.ContainsText())
            {
                var text = Clipboard.GetText();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    AddOrPromoteItem(text.Trim());
                }
            }
        }
        catch { }
    }

    public void AddOrPromoteItem(string text)
    {
        Application.Current?.Dispatcher?.Invoke(() =>
        {
            try
            {
                // Check if already in list
                for (int i = 0; i < Items.Count; i++)
                {
                    if (string.Equals(Items[i].FullText, text, StringComparison.Ordinal))
                    {
                        var existing = Items[i];
                        existing.Timestamp = DateTime.Now;
                        if (i > 0)
                        {
                            Items.Move(i, 0);
                        }
                        return;
                    }
                }

                // Generate clean single-line preview
                var preview = Regex.Replace(text, @"\s+", " ").Trim();
                if (preview.Length > 90)
                {
                    preview = preview.Substring(0, 87) + "...";
                }

                var item = new ClipboardItem
                {
                    FullText = text,
                    PreviewText = preview,
                    Timestamp = DateTime.Now
                };

                Items.Insert(0, item);

                // Cap items at 10 to keep widget fast and lightweight
                while (Items.Count > 10)
                {
                    Items.RemoveAt(Items.Count - 1);
                }
            }
            catch { }
        });
    }

    public async Task CopyToClipboardAsync(ClipboardItem item)
    {
        if (item == null || string.IsNullOrEmpty(item.FullText)) return;

        try
        {
            _isSelfCopy = true;
            Clipboard.SetText(item.FullText);

            item.IsJustCopied = true;
            await Task.Delay(1400);
            item.IsJustCopied = false;
        }
        catch
        {
            _isSelfCopy = false;
        }
    }

    public void Clear()
    {
        Application.Current?.Dispatcher?.Invoke(() =>
        {
            Items.Clear();
        });
    }
}
