using System;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using OpenWidget.Models;

namespace OpenWidget.Services;

public class SystemTelemetryService : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }

    [DllImport("kernel32.dll")]
    public static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpSystemPowerStatus);

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

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetTemperature")]
    private static extern int NvmlDeviceGetTemperature(IntPtr device, int sensorType, out uint temp);

    [DllImport("nvml.dll", EntryPoint = "nvmlShutdown")]
    private static extern int NvmlShutdown();

    private long _prevIdle, _prevKernel, _prevUser;
    private bool _nvmlInitialized;
    private bool _nvmlFailed;
    private IntPtr _nvmlDevice = IntPtr.Zero;
    private string _gpuName = "RTX 3060";

    // Network stats tracking
    private long _prevBytesReceived, _prevBytesSent;
    private DateTime _prevNetTime = DateTime.UtcNow;
    private int _lastPingMs = 9;
    private DateTime _lastPingTime = DateTime.MinValue;

    // Sparkline history tracking
    private readonly Queue<double> _cpuHistory = new();
    private readonly Queue<double> _gpuHistory = new();
    private const int MaxHistoryPoints = 20;

    private static PointCollection BuildSparklinePoints(Queue<double> history, double width, double height)
    {
        var pc = new PointCollection();
        if (history.Count < 2)
        {
            pc.Add(new Point(0, height - 2));
            pc.Add(new Point(width, height - 2));
            pc.Freeze();
            return pc;
        }

        var list = history.ToArray();
        double step = width / (list.Length - 1);
        for (int i = 0; i < list.Length; i++)
        {
            double x = i * step;
            double normalized = Math.Clamp(list[i] / 100.0, 0.0, 1.0);
            double y = height - (normalized * (height - 3)) - 1.5;
            pc.Add(new Point(x, y));
        }
        pc.Freeze();
        return pc;
    }

    public SystemTelemetryService()
    {
        TryInitNvml();
        InitNetworkBaselines();
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

    private void InitNetworkBaselines()
    {
        try
        {
            GetTotalNetworkBytes(out _prevBytesReceived, out _prevBytesSent);
            _prevNetTime = DateTime.UtcNow;
            UpdatePingAsync();
        }
        catch { }
    }

    public TelemetrySpecs GetTelemetrySpecs()
    {
        var specs = new TelemetrySpecs();

        // 0. System Uptime
        var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
        var isEn = LocalizationService.CurrentLanguage.Equals("en", StringComparison.OrdinalIgnoreCase);
        specs.UptimeText = uptime.Days > 0
            ? (isEn ? $"{uptime.Days}d {uptime.Hours}h" : $"{uptime.Days}g {uptime.Hours}s")
            : (isEn ? $"{uptime.Hours}h {uptime.Minutes}m" : $"{uptime.Hours}s {uptime.Minutes}dk");

        // 1. CPU Load
        double cpuPercent = GetCpuUsage();
        _cpuHistory.Enqueue(cpuPercent);
        while (_cpuHistory.Count > MaxHistoryPoints) _cpuHistory.Dequeue();
        var cpuSparkline = BuildSparklinePoints(_cpuHistory, 44, 14);

        specs.Performance.Add(new TelemetryItem(
            Key: "cpu",
            IconKey: "IconCpu",
            Label: LocalizationService.Get("Cpu"),
            Percentage: cpuPercent,
            MainText: $"%{cpuPercent:F0}",
            SubText: isEn ? "8 Cores • 16 Threads" : "8 Çekirdek • 16 İzlek",
            BarColorKey: cpuPercent > 85 ? "AccentRedBrush" : (cpuPercent > 65 ? "AccentAmberBrush" : "AccentBrush"),
            IsAlert: cpuPercent > 85,
            SparklinePoints: cpuSparkline,
            HasSparkline: true
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
                Label: LocalizationService.Get("Ram"),
                Percentage: ramPercent,
                MainText: $"%{ramPercent:F0}",
                SubText: $"{usedGb:F1} / {totalGb:F0} GB",
                BarColorKey: ramPercent > 85 ? "AccentRedBrush" : (ramPercent > 70 ? "AccentAmberBrush" : "AccentGreenBrush"),
                IsAlert: ramPercent > 85
            ));
        }

        // 3. GPU & VRAM Load (NVIDIA NVML with live temp)
        var gpuData = GetGpuUsage();
        if (gpuData.HasValue)
        {
            var (gpuPercent, vramPercent, vramUsedGb, vramTotalGb, gpuTemp) = gpuData.Value;

            string gpuSubtitle = gpuTemp > 0 ? $"{_gpuName} • {gpuTemp}°C" : _gpuName;
            bool isGpuAlert = gpuTemp > 80 || gpuPercent > 90;

            _gpuHistory.Enqueue(gpuPercent);
            while (_gpuHistory.Count > MaxHistoryPoints) _gpuHistory.Dequeue();
            var gpuSparkline = BuildSparklinePoints(_gpuHistory, 44, 14);

            specs.Performance.Add(new TelemetryItem(
                Key: "gpu",
                IconKey: "IconGpu",
                Label: LocalizationService.Get("Gpu"),
                Percentage: gpuPercent,
                MainText: $"%{gpuPercent:F0}",
                SubText: gpuSubtitle,
                BarColorKey: isGpuAlert ? "AccentRedBrush" : (gpuPercent > 65 ? "AccentAmberBrush" : "AccentBrush"),
                IsAlert: isGpuAlert,
                SparklinePoints: gpuSparkline,
                HasSparkline: true
            ));

            specs.Performance.Add(new TelemetryItem(
                Key: "vram",
                IconKey: "IconRam",
                Label: LocalizationService.Get("Vram"),
                Percentage: vramPercent,
                MainText: $"%{vramPercent:F0}",
                SubText: $"{vramUsedGb:F1} / {vramTotalGb:F0} GB",
                BarColorKey: vramPercent > 85 ? "AccentRedBrush" : (vramPercent > 70 ? "AccentAmberBrush" : "AccentGreenBrush"),
                IsAlert: vramPercent > 85
            ));
        }

        // 4. Network Traffic & Ping Quality Hub
        try
        {
            GetTotalNetworkBytes(out long currRx, out long currTx);
            var now = DateTime.UtcNow;
            double elapsedSec = Math.Max(0.5, (now - _prevNetTime).TotalSeconds);

            double downBps = Math.Max(0, (currRx - _prevBytesReceived) / elapsedSec);
            double upBps = Math.Max(0, (currTx - _prevBytesSent) / elapsedSec);

            _prevBytesReceived = currRx;
            _prevBytesSent = currTx;
            _prevNetTime = now;

            UpdatePingAsync();

            string statusBadge;
            string statusBadgeBrush;
            if (_lastPingMs <= 35) { statusBadge = isEn ? "Great" : "Harika"; statusBadgeBrush = "AccentGreenBrush"; }
            else if (_lastPingMs <= 70) { statusBadge = isEn ? "Good" : "İyi"; statusBadgeBrush = "AccentGreenBrush"; }
            else if (_lastPingMs <= 110) { statusBadge = isEn ? "Normal" : "Normal"; statusBadgeBrush = "AccentAmberBrush"; }
            else { statusBadge = isEn ? "High" : "Yüksek"; statusBadgeBrush = "AccentRedBrush"; }

            double netPercent = Math.Min(100.0, Math.Max(2.0, (downBps * 8.0 / 100_000_000.0) * 100.0));
            specs.Performance.Add(new TelemetryItem(
                Key: "network",
                IconKey: "IconNetwork",
                Label: LocalizationService.Get("Network"),
                Percentage: netPercent,
                MainText: $"{_lastPingMs} ms",
                SubText: $"↓ {FormatSpeed(downBps)}  ↑ {FormatSpeed(upBps)}",
                BarColorKey: _lastPingMs > 80 ? "AccentRedBrush" : (_lastPingMs > 40 ? "AccentAmberBrush" : "AccentGreenBrush"),
                IsAlert: _lastPingMs > 100,
                StatusBadge: statusBadge,
                StatusBadgeBrush: statusBadgeBrush
            ));
        }
        catch { }

        // 5. Battery & Power Status (Laptops & Desktop Power)
        try
        {
            if (GetSystemPowerStatus(out var powerStatus))
            {
                bool hasBattery = powerStatus.BatteryFlag != 128 && powerStatus.BatteryLifePercent <= 100;
                if (hasBattery)
                {
                    bool isCharging = (powerStatus.BatteryFlag & 8) != 0 || powerStatus.ACLineStatus == 1;
                    int pct = powerStatus.BatteryLifePercent;
                    string fullOrRem = "";
                    if (isCharging)
                    {
                        fullOrRem = isEn ? "Plugged in / Charging" : "Prizde / Şarj Oluyor";
                    }
                    else if (powerStatus.BatteryLifeTime > 0)
                    {
                        int mins = powerStatus.BatteryLifeTime / 60;
                        fullOrRem = isEn ? $"{mins / 60}h {mins % 60}m remaining" : $"{mins / 60}s {mins % 60}dk kaldı";
                    }
                    else
                    {
                        fullOrRem = isEn ? "On Battery Power" : "Pilde Çalışıyor";
                    }

                    string batteryLabel = isEn ? "Battery" : "Batarya & Pil";
                    specs.Performance.Add(new TelemetryItem(
                        Key: "battery",
                        IconKey: "IconBattery",
                        Label: batteryLabel,
                        Percentage: pct,
                        MainText: $"%{pct}",
                        SubText: fullOrRem,
                        BarColorKey: pct <= 20 ? "AccentRedBrush" : (pct <= 40 ? "AccentAmberBrush" : "AccentGreenBrush"),
                        IsAlert: pct <= 15 && !isCharging
                    ));
                }
                else
                {
                    string powerLabel = isEn ? "Power Mode" : "Güç Durumu";
                    string powerSub = isEn ? "Desktop • Constant AC" : "Masaüstü • Sürekli Şebeke";
                    specs.Performance.Add(new TelemetryItem(
                        Key: "power",
                        IconKey: "IconBattery",
                        Label: powerLabel,
                        Percentage: 100,
                        MainText: "AC Priz",
                        SubText: powerSub,
                        BarColorKey: "AccentBrush"
                    ));
                }
            }
        }
        catch { }

        // 5. Disk Space (C: and D:)
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
                string fullStr = isEn ? "Used" : "Dolu";
                string freeStr = isEn ? "Free" : "Boş";
                string driveStr = isEn ? "Drive" : "Diski";

                specs.Storage.Add(new TelemetryItem(
                    Key: $"disk_{letter.ToLowerInvariant()}",
                    IconKey: "IconStorage",
                    Label: $"{letter}: {driveStr} ({diskType})",
                    Percentage: diskPercent,
                    MainText: $"%{diskPercent:F0} {fullStr}",
                    SubText: $"{freeGb:F0} GB {freeStr} / {totalGb:F0} GB",
                    BarColorKey: diskPercent > 90 ? "AccentRedBrush" : (diskPercent > 75 ? "AccentAmberBrush" : "AccentBrush"),
                    IsAlert: diskPercent > 90
                ));
            }
        }
        catch { }

        return specs;
    }

    private (double gpuPercent, double vramPercent, double vramUsedGb, double vramTotalGb, uint temp)? GetGpuUsage()
    {
        TryInitNvml();
        if (!_nvmlInitialized || _nvmlDevice == IntPtr.Zero) return null;

        try
        {
            var util = new NvmlUtilization();
            if (NvmlDeviceGetUtilizationRates(_nvmlDevice, ref util) != 0) return null;

            var mem = new NvmlMemory();
            if (NvmlDeviceGetMemoryInfo(_nvmlDevice, ref mem) != 0) return null;

            uint temp = 0;
            try { NvmlDeviceGetTemperature(_nvmlDevice, 0, out temp); } catch { }

            double usedGb = (double)mem.Used / (1024 * 1024 * 1024);
            double totalGb = (double)mem.Total / (1024 * 1024 * 1024);
            double vramPercent = totalGb > 0 ? (usedGb / totalGb) * 100.0 : 0;

            return (util.Gpu, vramPercent, usedGb, totalGb, temp);
        }
        catch
        {
            return null;
        }
    }

    private void UpdatePingAsync()
    {
        if ((DateTime.UtcNow - _lastPingTime).TotalSeconds < 6) return;
        _lastPingTime = DateTime.UtcNow;

        Task.Run(async () =>
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync("1.1.1.1", 500);
                if (reply.Status == IPStatus.Success)
                {
                    _lastPingMs = (int)reply.RoundtripTime;
                }
            }
            catch { }
        });
    }

    private static void GetTotalNetworkBytes(out long rx, out long tx)
    {
        rx = 0;
        tx = 0;
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var ni in interfaces)
            {
                if (ni.OperationalStatus == OperationalStatus.Up &&
                    ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                {
                    var stats = ni.GetIPStatistics();
                    rx += stats.BytesReceived;
                    tx += stats.BytesSent;
                }
            }
        }
        catch { }
    }

    private static string FormatSpeed(double bytesPerSec)
    {
        double bitsPerSec = bytesPerSec * 8.0;
        if (bitsPerSec >= 1_000_000_000) return $"{bitsPerSec / 1_000_000_000:F1} Gbps";
        if (bitsPerSec >= 1_000_000) return $"{bitsPerSec / 1_000_000:F1} Mbps";
        if (bitsPerSec >= 1_000) return $"{bitsPerSec / 1_000:F0} Kbps";
        return $"{bitsPerSec:F0} bps";
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
