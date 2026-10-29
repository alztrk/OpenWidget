using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OpenWidget.Services;

public static class ScratchpadService
{
    private static readonly string FilePath;
    private static readonly SemaphoreSlim _fileLock = new(1, 1);

    static ScratchpadService()
    {
        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "OpenWidget");
            Directory.CreateDirectory(dir);
            FilePath = Path.Combine(dir, "scratchpad.txt");
        }
        catch
        {
            FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scratchpad.txt");
        }
    }

    public static async Task<string> LoadAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            if (File.Exists(FilePath))
            {
                return await File.ReadAllTextAsync(FilePath);
            }
        }
        catch { }
        finally
        {
            _fileLock.Release();
        }
        return string.Empty;
    }

    public static async Task SaveAsync(string text)
    {
        await _fileLock.WaitAsync();
        try
        {
            await File.WriteAllTextAsync(FilePath, text);
        }
        catch { }
        finally
        {
            _fileLock.Release();
        }
    }
}
