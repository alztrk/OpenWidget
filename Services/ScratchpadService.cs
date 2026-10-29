using System;
using System.IO;
using System.Threading.Tasks;

namespace OpenWidget.Services;

public static class ScratchpadService
{
    private static readonly string FilePath;

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
        try
        {
            if (File.Exists(FilePath))
            {
                return await File.ReadAllTextAsync(FilePath);
            }
        }
        catch { }
        return string.Empty;
    }

    public static async Task SaveAsync(string text)
    {
        try
        {
            await File.WriteAllTextAsync(FilePath, text);
        }
        catch { }
    }
}
