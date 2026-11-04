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
    private const int MaxHistoryItems = 15;
    private static readonly JsonSerializerOptions HistoryJsonOptions = new() { WriteIndented = true };

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

    public async Task InitializeAsync(IntPtr hwnd)
    {
        _hwnd = hwnd;

        await LoadHistoryAsync();

        if (_hwnd != IntPtr.Zero)
        {
            try
            {
                AddClipboardFormatListener(_hwnd);
            }
            catch { }
        }

        // Capture whatever is currently on the clipboard upon launch.
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
                        var imageData = EncodeThumbnail(img);
                        AddOrPromoteItemWithImageData("image://" + Guid.NewGuid().ToString("N"), "Image", $"Görsel ({img.PixelWidth}x{img.PixelHeight})", imageData);
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
        byte[]? imageData = null;
        if (!string.IsNullOrEmpty(imageBase64))
        {
            var image = new ClipboardItem { ImageBase64 = imageBase64 };
            imageData = image.ImageData;
        }

        AddOrPromoteItemWithImageData(content, itemType, customPreview, imageData);
    }

    private void AddOrPromoteItemWithImageData(string content, string itemType, string? customPreview, byte[]? imageData)
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
                    ImageData = imageData,
                    Timestamp = DateTime.Now
                };

                Items.Insert(0, item);

                // Cap items at 15 to keep widget fast and lightweight
                while (Items.Count > MaxHistoryItems)
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

            if (item.ItemType == "Image")
            {
                if (item.ImageData is not { Length: > 0 } imageData)
                {
                    _isSelfCopy = false;
                    return;
                }

                using var ms = new MemoryStream(imageData);
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

            var tempPath = _cachePath + ".tmp";
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    JsonSerializer.Serialize(stream, Items, HistoryJsonOptions);
                }

                File.Move(tempPath, _cachePath, overwrite: true);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }
        catch { }
    }

    private async Task LoadHistoryAsync()
    {
        try
        {
            if (File.Exists(_cachePath))
            {
                await using var stream = new FileStream(
                    _cachePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 4096,
                    useAsync: true);

                var items = new List<ClipboardItem>(MaxHistoryItems);
                var recordsRead = 0;
                await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<ClipboardItem>(stream))
                {
                    recordsRead++;
                    if (item != null)
                    {
                        items.Add(item);
                    }

                    if (recordsRead == MaxHistoryItems)
                    {
                        break;
                    }
                }

                Items.Clear();
                foreach (var item in items)
                {
                    Items.Add(item);
                }
            }
        }
        catch { }
    }

    private static byte[]? EncodeThumbnail(BitmapSource bmp)
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
            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }
}
