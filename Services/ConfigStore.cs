using System;
using System.IO;
using OpenWidget.Models;

namespace OpenWidget.Services;

public interface IConfigStore
{
    HardwareConfig Load(string path, Func<HardwareConfig>? migrationFactory = null);
    void Save(string path, HardwareConfig config);
}

public sealed class JsonConfigStore : IConfigStore
{
    public HardwareConfig Load(string path, Func<HardwareConfig>? migrationFactory = null)
    {
        if (File.Exists(path))
        {
            return HardwareConfig.Load(path);
        }

        return migrationFactory?.Invoke() ?? new HardwareConfig();
    }

    public void Save(string path, HardwareConfig config)
    {
        config.Save(path);
    }
}
