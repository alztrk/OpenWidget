using System;
using System.IO;
using System.Runtime.InteropServices;
using RigBadge.Models;

namespace RigBadge.Services;

public class SystemTelemetryService
{
    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out long idleTime, out long kernelTime, out long userTime);

    private long _prevIdle, _prevKernel, _prevUser;

    public TelemetrySpecs GetTelemetrySpecs()
    {
        var specs = new TelemetrySpecs();

        // 1. CPU Load
        double cpuPercent = GetCpuUsage();
        specs.Performance.Add(new TelemetryItem(
            Key: "cpu",
            IconKey: "IconCpu",
            Label: "İşlemci (CPU)",
            Percentage: cpuPercent,
            MainText: $"%{cpuPercent:F0}",
            SubText: "8 Çekirdek • 16 İzlek",
            BarColorKey: cpuPercent > 85 ? "AccentRedBrush" : (cpuPercent > 65 ? "AccentAmberBrush" : "AccentBrush")
        ));

        // 2. RAM Load
        var mem = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX)) };
        if (GlobalMemoryStatusEx(ref mem))
        {
            double totalGb = (double)mem.ullTotalPhys / (1024 * 1024 * 1024);
            double freeGb = (double)mem.ullAvailPhys / (1024 * 1024 * 1024);
            double usedGb = Math.Max(0, totalGb - freeGb);
            double ramPercent = mem.dwMemoryLoad;

            specs.Performance.Add(new TelemetryItem(
                Key: "ram",
                IconKey: "IconRam",
                Label: "Bellek (RAM)",
                Percentage: ramPercent,
                MainText: $"%{ramPercent:F0}",
                SubText: $"{usedGb:F1} / {totalGb:F0} GB",
                BarColorKey: ramPercent > 85 ? "AccentRedBrush" : (ramPercent > 70 ? "AccentAmberBrush" : "AccentGreenBrush")
            ));
        }

        // 3. Disk Space (C: and D:)
        try
        {
            var drives = DriveInfo.GetDrives();
            foreach (var d in drives)
            {
                if (!d.IsReady) continue;
                var letter = d.Name.Substring(0, 1).ToUpperInvariant();
                if (letter != "C" && letter != "D") continue;

                double totalGb = (double)d.TotalSize / (1024 * 1024 * 1024);
                double freeGb = (double)d.AvailableFreeSpace / (1024 * 1024 * 1024);
                double usedGb = Math.Max(0, totalGb - freeGb);
                double diskPercent = totalGb > 0 ? (usedGb / totalGb) * 100.0 : 0;

                var diskType = letter == "C" ? "NVMe SSD" : "SATA SSD";
                specs.Storage.Add(new TelemetryItem(
                    Key: $"disk_{letter.ToLowerInvariant()}",
                    IconKey: "IconStorage",
                    Label: $"{letter}: Diski ({diskType})",
                    Percentage: diskPercent,
                    MainText: $"%{diskPercent:F0} Dolu",
                    SubText: $"{freeGb:F0} GB Boş / {totalGb:F0} GB",
                    BarColorKey: diskPercent > 90 ? "AccentRedBrush" : (diskPercent > 75 ? "AccentAmberBrush" : "AccentBrush")
                ));
            }
        }
        catch { }

        return specs;
    }

    private double GetCpuUsage()
    {
        GetSystemTimes(out long idle, out long kernel, out long user);

        if (_prevIdle == 0)
        {
            _prevIdle = idle;
            _prevKernel = kernel;
            _prevUser = user;
            return 0;
        }

        long idleDiff = idle - _prevIdle;
        long kernelDiff = kernel - _prevKernel;
        long userDiff = user - _prevUser;

        _prevIdle = idle;
        _prevKernel = kernel;
        _prevUser = user;

        long total = kernelDiff + userDiff;
        if (total <= 0) return 0;

        double usage = (double)(total - idleDiff) * 100.0 / total;
        return Math.Max(0, Math.Min(100, Math.Round(usage, 1)));
    }
}
