using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
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
    private readonly string _cachePath;

    public ObservableCollection<ClipboardItem> Items { get; } = new();

    public ClipboardService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _cachePath = Path.Combine(appData, "OpenWidget", "clipboard-history.json");
    }

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

        // 1. Load persistent clipboard items from disk
        LoadHistory();

        // 2. Capture whatever is currently on the clipboard upon launch
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
                        AddOrPromoteItem(text.Trim(), "Text");
                        break;
                    }
                }
                else if (Clipboard.ContainsFileDropList())
                {
                    var files = Clipboard.GetFileDropList();
                    if (files != null && files.Count > 0 && !string.IsNullOrEmpty(files[0]))
                    {
                        var first = files[0]!;
                        var name = Path.GetFileName(first);
                        var preview = files.Count > 1 ? $"{name} (+{files.Count - 1} dosya)" : name;
                        AddOrPromoteItem(first, "File", preview);
                        break;
                    }
                }
                else if (Clipboard.ContainsImage())
                {
                    var img = Clipboard.GetImage();
                    if (img != null)
                    {
                        var base64 = EncodeThumbnail(img);
                        AddOrPromoteItem("image://" + Guid.NewGuid().ToString("N"), "Image", $"Görsel ({img.PixelWidth}x{img.PixelHeight})", base64);
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
                    AddOrPromoteItem(text.Trim(), "Text");
                }
            }
        }
        catch { }
    }

    public void AddOrPromoteItem(string content, string itemType = "Text", string? customPreview = null, string? imageBase64 = null)
    {
        Application.Current?.Dispatcher?.Invoke(() =>
        {
            try
            {
                // Check if already in list
                for (int i = 0; i < Items.Count; i++)
                {
                    if (string.Equals(Items[i].FullText, content, StringComparison.Ordinal))
                    {
                        var existing = Items[i];
                        existing.Timestamp = DateTime.Now;
                        if (i > 0)
                        {
                            Items.Move(i, 0);
                        }
                        SaveHistory();
                        return;
                    }
                }

                string preview = customPreview ?? "";
                if (string.IsNullOrEmpty(preview))
                {
                    preview = Regex.Replace(content, @"\s+", " ").Trim();
                    if (preview.Length > 90)
                    {
                        preview = preview.Substring(0, 87) + "...";
                    }
                }

                var item = new ClipboardItem
                {
                    FullText = content,
                    PreviewText = preview,
                    ItemType = itemType,
                    ImageBase64 = imageBase64,
                    Timestamp = DateTime.Now
                };

                Items.Insert(0, item);

                // Cap items at 15 to keep widget fast and lightweight
                while (Items.Count > 15)
                {
                    Items.RemoveAt(Items.Count - 1);
                }

                SaveHistory();
            }
            catch { }
        });
    }

    public async Task CopyToClipboardAsync(ClipboardItem item)
    {
        if (item == null) return;

        try
        {
            _isSelfCopy = true;

            if (item.ItemType == "Image" && !string.IsNullOrEmpty(item.ImageBase64))
            {
                var bytes = Convert.FromBase64String(item.ImageBase64);
                using var ms = new MemoryStream(bytes);
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.StreamSource = ms;
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                Clipboard.SetImage(bmp);
            }
            else if (item.ItemType == "File" && File.Exists(item.FullText))
            {
                var files = new System.Collections.Specialized.StringCollection { item.FullText };
                Clipboard.SetFileDropList(files);
            }
            else
            {
                Clipboard.SetText(item.FullText);
            }

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
            SaveHistory();
        });
    }

    private void SaveHistory()
    {
        try
        {
            var dir = Path.GetDirectoryName(_cachePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var list = new List<ClipboardItem>(Items);
            var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_cachePath, json);
        }
        catch { }
    }

    private void LoadHistory()
    {
        try
        {
            if (File.Exists(_cachePath))
            {
                var json = File.ReadAllText(_cachePath);
                var list = JsonSerializer.Deserialize<List<ClipboardItem>>(json);
                if (list != null)
                {
                    Items.Clear();
                    foreach (var item in list)
                    {
                        Items.Add(item);
                    }
                }
            }
        }
        catch { }
    }

    private static string? EncodeThumbnail(BitmapSource bmp)
    {
        try
        {
            var targetWidth = Math.Min(120, bmp.PixelWidth);
            var scale = (double)targetWidth / bmp.PixelWidth;
            var targetHeight = (int)(bmp.PixelHeight * scale);

            var transformed = new TransformedBitmap(bmp, new System.Windows.Media.ScaleTransform(scale, scale));
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(transformed));

            using var ms = new MemoryStream();
            encoder.Save(ms);
            return Convert.ToBase64String(ms.ToArray());
        }
        catch
        {
            return null;
        }
    }
}
