using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using RigBadge.Models;

namespace RigBadge.Services;

public class SystemTelemetryService : IDisposable
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

    // NVML (NVIDIA Management Library) P/Invoke
    [DllImport("nvml.dll", EntryPoint = "nvmlInit_v2")]
    private static extern int NvmlInit();

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetHandleByIndex_v2")]
    private static extern int NvmlDeviceGetHandleByIndex(uint index, out IntPtr device);

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetName")]
    private static extern int NvmlDeviceGetName(IntPtr device, byte[] name, uint length);

    [StructLayout(LayoutKind.Sequential)]
    private struct NvmlUtilization
    {
        public uint Gpu;
        public uint Memory;
    }

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetUtilizationRates")]
    private static extern int NvmlDeviceGetUtilizationRates(IntPtr device, ref NvmlUtilization utilization);

    [StructLayout(LayoutKind.Sequential)]
    private struct NvmlMemory
    {
        public ulong Total;
        public ulong Free;
        public ulong Used;
    }

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetMemoryInfo")]
    private static extern int NvmlDeviceGetMemoryInfo(IntPtr device, ref NvmlMemory memory);

    [DllImport("nvml.dll", EntryPoint = "nvmlShutdown")]
    private static extern int NvmlShutdown();

    private long _prevIdle, _prevKernel, _prevUser;
    private bool _nvmlInitialized;
    private bool _nvmlFailed;
    private IntPtr _nvmlDevice = IntPtr.Zero;
    private string _gpuName = "RTX 3060";

    public SystemTelemetryService()
    {
        TryInitNvml();
    }

    private void TryInitNvml()
    {
        if (_nvmlInitialized || _nvmlFailed) return;
        try
        {
            if (NvmlInit() == 0)
            {
                if (NvmlDeviceGetHandleByIndex(0, out _nvmlDevice) == 0)
                {
                    _nvmlInitialized = true;
                    var nameBuf = new byte[64];
                    if (NvmlDeviceGetName(_nvmlDevice, nameBuf, 64) == 0)
                    {
                        var raw = Encoding.ASCII.GetString(nameBuf).TrimEnd('\0');
                        if (!string.IsNullOrWhiteSpace(raw))
                        {
                            _gpuName = raw.Replace("NVIDIA GeForce ", "").Trim();
                        }
                    }
                    return;
                }
            }
            _nvmlFailed = true;
        }
        catch
        {
            _nvmlFailed = true;
        }
    }

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

        // 3. GPU & VRAM Load (NVIDIA NVML)
        var gpuData = GetGpuUsage();
        if (gpuData.HasValue)
        {
            var (gpuPercent, vramPercent, vramUsedGb, vramTotalGb) = gpuData.Value;

            specs.Performance.Add(new TelemetryItem(
                Key: "gpu",
                IconKey: "IconGpu",
                Label: "Ekran Kartı (GPU)",
                Percentage: gpuPercent,
                MainText: $"%{gpuPercent:F0}",
                SubText: _gpuName,
                BarColorKey: gpuPercent > 85 ? "AccentRedBrush" : (gpuPercent > 65 ? "AccentAmberBrush" : "AccentBrush")
            ));

            specs.Performance.Add(new TelemetryItem(
                Key: "vram",
                IconKey: "IconRam",
                Label: "Video Belleği (VRAM)",
                Percentage: vramPercent,
                MainText: $"%{vramPercent:F0}",
                SubText: $"{vramUsedGb:F1} / {vramTotalGb:F0} GB",
                BarColorKey: vramPercent > 85 ? "AccentRedBrush" : (vramPercent > 70 ? "AccentAmberBrush" : "AccentGreenBrush")
            ));
        }

        // 4. Disk Space (C: and D:)
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

    private (double gpuPercent, double vramPercent, double vramUsedGb, double vramTotalGb)? GetGpuUsage()
    {
        TryInitNvml();
        if (!_nvmlInitialized || _nvmlDevice == IntPtr.Zero) return null;

        try
        {
            var util = new NvmlUtilization();
            if (NvmlDeviceGetUtilizationRates(_nvmlDevice, ref util) != 0) return null;

            var mem = new NvmlMemory();
            if (NvmlDeviceGetMemoryInfo(_nvmlDevice, ref mem) != 0) return null;

            double usedGb = (double)mem.Used / (1024 * 1024 * 1024);
            double totalGb = (double)mem.Total / (1024 * 1024 * 1024);
            double vramPercent = totalGb > 0 ? (usedGb / totalGb) * 100.0 : 0;

            return (util.Gpu, vramPercent, usedGb, totalGb);
        }
        catch
        {
            return null;
        }
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

    public void Dispose()
    {
        if (_nvmlInitialized)
        {
            try
            {
                NvmlShutdown();
            }
            catch { }
            _nvmlInitialized = false;
        }
    }
}
