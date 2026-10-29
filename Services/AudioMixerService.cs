using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenWidget.Services;

public class AudioSessionModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private float _volume;
    private bool _isMuted;
    private string _displayName = "";
    private ImageSource? _appIcon;

    public int ProcessId { get; set; }

    public string DisplayName
    {
        get => _displayName;
        set
        {
            if (_displayName != value)
            {
                _displayName = value;
                OnPropertyChanged();
            }
        }
    }

    public ImageSource? AppIcon
    {
        get => _appIcon;
        set
        {
            if (_appIcon != value)
            {
                _appIcon = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AppIconVisibility));
                OnPropertyChanged(nameof(FallbackIconVisibility));
            }
        }
    }

    public Visibility AppIconVisibility => _appIcon != null ? Visibility.Visible : Visibility.Collapsed;
    public Visibility FallbackIconVisibility => _appIcon != null ? Visibility.Collapsed : Visibility.Visible;

    public float Volume
    {
        get => _volume;
        set
        {
            var clamped = Math.Clamp(value, 0f, 100f);
            if (Math.Abs(_volume - clamped) > 0.05f)
            {
                _volume = clamped;
                OnPropertyChanged();
                OnPropertyChanged(nameof(VolumePercentText));
                if (SimpleVolume != null)
                {
                    try
                    {
                        var guid = Guid.Empty;
                        SimpleVolume.SetMasterVolume(_volume / 100f, ref guid);
                    }
                    catch { }
                }
            }
        }
    }

    public bool IsMuted
    {
        get => _isMuted;
        set
        {
            if (_isMuted != value)
            {
                _isMuted = value;
                OnPropertyChanged();
                if (SimpleVolume != null)
                {
                    try
                    {
                        var guid = Guid.Empty;
                        SimpleVolume.SetMute(_isMuted, ref guid);
                    }
                    catch { }
                }
            }
        }
    }

    public string VolumePercentText => $"{(int)Math.Round(_volume)}%";

    internal ISimpleAudioVolume? SimpleVolume { get; set; }

    public void ToggleMute()
    {
        IsMuted = !IsMuted;
    }
}

public class MasterVolumeModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private float _volume;
    private bool _isMuted;
    private string _deviceName = "Kulaklık / Hoparlör";

    public string DeviceName
    {
        get => _deviceName;
        set
        {
            if (_deviceName != value)
            {
                _deviceName = value;
                OnPropertyChanged();
            }
        }
    }

    public float Volume
    {
        get => _volume;
        set
        {
            var clamped = Math.Clamp(value, 0f, 100f);
            if (Math.Abs(_volume - clamped) > 0.05f)
            {
                _volume = clamped;
                OnPropertyChanged();
                OnPropertyChanged(nameof(VolumePercentText));
                if (EndpointVolume != null)
                {
                    try
                    {
                        var guid = Guid.Empty;
                        EndpointVolume.SetMasterVolumeLevelScalar(_volume / 100f, ref guid);
                    }
                    catch { }
                }
            }
        }
    }

    public bool IsMuted
    {
        get => _isMuted;
        set
        {
            if (_isMuted != value)
            {
                _isMuted = value;
                OnPropertyChanged();
                if (EndpointVolume != null)
                {
                    try
                    {
                        var guid = Guid.Empty;
                        EndpointVolume.SetMute(_isMuted, ref guid);
                    }
                    catch { }
                }
            }
        }
    }

    public string VolumePercentText => $"{(int)Math.Round(_volume)}%";

    internal IAudioEndpointVolume? EndpointVolume { get; set; }

    public void ToggleMute()
    {
        IsMuted = !IsMuted;
    }
}

public static class AudioMixerService
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private const uint SHGFI_ICON = 0x000000100;
    private const uint SHGFI_SMALLICON = 0x000000001;

    private static readonly ConcurrentDictionary<string, ImageSource> _iconCache = new(StringComparer.OrdinalIgnoreCase);

    public static ImageSource? GetProcessIcon(string exePath)
    {
        if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath)) return null;

        if (_iconCache.TryGetValue(exePath, out var cached))
        {
            return cached;
        }

        try
        {
            var shinfo = new SHFILEINFO();
            IntPtr res = SHGetFileInfo(exePath, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_SMALLICON);
            if (res != IntPtr.Zero && shinfo.hIcon != IntPtr.Zero)
            {
                var bmp = Imaging.CreateBitmapSourceFromHIcon(
                    shinfo.hIcon,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                bmp.Freeze();
                DestroyIcon(shinfo.hIcon);
                _iconCache[exePath] = bmp;
                return bmp;
            }
        }
        catch { }

        return null;
    }

    private static readonly Guid IID_IAudioEndpointVolume = new("5CDF2C82-841E-4546-9722-0CF74078229A");
    private static readonly Guid IID_IAudioSessionManager2 = new("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F");
    private static readonly Guid IID_IPropertyStore = new("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99");
    private static readonly PropertyKey PKEY_Device_FriendlyName = new(new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"), 14);

    public static (MasterVolumeModel Master, List<AudioSessionModel> Sessions) GetAudioState()
    {
        var master = new MasterVolumeModel();
        var sessions = new List<AudioSessionModel>();

        try
        {
            var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
            if (enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out var device) != 0 || device == null)
            {
                return (master, sessions);
            }

            // 1. Get Device Name
            try
            {
                if (device.OpenPropertyStore(0, out var storePtr) == 0 && storePtr != IntPtr.Zero)
                {
                    var store = (IPropertyStore)Marshal.GetObjectForIUnknown(storePtr);
                    var key = PKEY_Device_FriendlyName;
                    if (store.GetValue(ref key, out var pv) == 0 && pv.pwszVal != IntPtr.Zero)
                    {
                        var name = Marshal.PtrToStringUni(pv.pwszVal);
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            master.DeviceName = name;
                        }
                    }
                    Marshal.Release(storePtr);
                }
            }
            catch { }

            // 2. Master Endpoint Volume
            try
            {
                var iidEndpoint = IID_IAudioEndpointVolume;
                if (device.Activate(ref iidEndpoint, 1 /* CLSCTX_INPROC_SERVER */, IntPtr.Zero, out var epObj) == 0 && epObj is IAudioEndpointVolume endpointVol)
                {
                    if (endpointVol.GetMasterVolumeLevelScalar(out var scalar) == 0)
                    {
                        master.Volume = (float)Math.Round(scalar * 100);
                    }
                    if (endpointVol.GetMute(out var isMuted) == 0)
                    {
                        master.IsMuted = isMuted;
                    }
                    master.EndpointVolume = endpointVol;
                }
            }
            catch { }

            // 3. Audio Sessions per Application
            try
            {
                var iidMgr = IID_IAudioSessionManager2;
                if (device.Activate(ref iidMgr, 1, IntPtr.Zero, out var mgrObj) == 0 && mgrObj is IAudioSessionManager2 sessionMgr)
                {
                    if (sessionMgr.GetSessionEnumerator(out var sessionEnum) == 0 && sessionEnum != null)
                    {
                        sessionEnum.GetCount(out var count);
                        var seenPids = new HashSet<int>();

                        for (int i = 0; i < count; i++)
                        {
                            if (sessionEnum.GetSession(i, out var sessObj) == 0 && sessObj is IAudioSessionControl sessionCtrl)
                            {
                                try
                                {
                                    sessionCtrl.GetState(out var state);
                                    if (state == AudioSessionState.AudioSessionStateExpired) continue;

                                    if (sessionCtrl is IAudioSessionControl2 ctl2)
                                    {
                                        ctl2.GetProcessId(out var pid);
                                        int procId = (int)pid;

                                        string name = "";
                                        string exePath = "";
                                        if (ctl2.IsSystemSoundsSession() == 0 || procId == 0)
                                        {
                                            name = "Sistem Sesleri";
                                            procId = 0;
                                        }
                                        else
                                        {
                                            if (seenPids.Contains(procId)) continue;

                                            try
                                            {
                                                var proc = Process.GetProcessById(procId);
                                                exePath = proc.MainModule?.FileName ?? "";
                                                name = !string.IsNullOrWhiteSpace(proc.MainWindowTitle)
                                                    ? proc.MainWindowTitle
                                                    : proc.ProcessName;

                                                // If main window title is long, use FileDescription or ProcessName
                                                if (name.Length > 24)
                                                {
                                                    try
                                                    {
                                                        name = proc.MainModule?.FileVersionInfo?.FileDescription ?? proc.ProcessName;
                                                    }
                                                    catch
                                                    {
                                                        name = proc.ProcessName;
                                                    }
                                                }
                                            }
                                            catch
                                            {
                                                sessionCtrl.GetDisplayName(out var dispName);
                                                name = !string.IsNullOrWhiteSpace(dispName) ? dispName : $"Uygulama ({procId})";
                                            }
                                        }

                                        seenPids.Add(procId);

                                        var item = new AudioSessionModel
                                        {
                                            ProcessId = procId,
                                            DisplayName = name,
                                            AppIcon = !string.IsNullOrEmpty(exePath) ? GetProcessIcon(exePath) : null
                                        };

                                        if (sessionCtrl is ISimpleAudioVolume simpleVol)
                                        {
                                            if (simpleVol.GetMasterVolume(out var vol) == 0)
                                            {
                                                item.Volume = (float)Math.Round(vol * 100);
                                            }
                                            if (simpleVol.GetMute(out var muted) == 0)
                                            {
                                                item.IsMuted = muted;
                                            }
                                            item.SimpleVolume = simpleVol;
                                        }

                                        sessions.Add(item);
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }
            }
            catch { }
        }
        catch { }

        return (master, sessions);
    }

    public static (float Volume, bool IsMuted) GetMasterVolume()
    {
        try
        {
            var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
            if (enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out var device) == 0 && device != null)
            {
                var iid = IID_IAudioEndpointVolume;
                if (device.Activate(ref iid, 1, IntPtr.Zero, out var epObj) == 0 && epObj is IAudioEndpointVolume ep)
                {
                    ep.GetMasterVolumeLevelScalar(out var scalar);
                    ep.GetMute(out var isMuted);
                    return ((float)Math.Round(scalar * 100), isMuted);
                }
            }
        }
        catch { }
        return (50f, false);
    }

    public static void SetMasterVolume(float volume)
    {
        try
        {
            var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
            if (enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out var device) == 0 && device != null)
            {
                var iid = IID_IAudioEndpointVolume;
                if (device.Activate(ref iid, 1, IntPtr.Zero, out var epObj) == 0 && epObj is IAudioEndpointVolume ep)
                {
                    var guid = Guid.Empty;
                    ep.SetMasterVolumeLevelScalar(Math.Clamp(volume / 100f, 0f, 1f), ref guid);
                }
            }
        }
        catch { }
    }

    public static bool ToggleMasterMute()
    {
        try
        {
            var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
            if (enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out var device) == 0 && device != null)
            {
                var iid = IID_IAudioEndpointVolume;
                if (device.Activate(ref iid, 1, IntPtr.Zero, out var epObj) == 0 && epObj is IAudioEndpointVolume ep)
                {
                    ep.GetMute(out var isMuted);
                    bool newMute = !isMuted;
                    var guid = Guid.Empty;
                    ep.SetMute(newMute, ref guid);
                    return newMute;
                }
            }
        }
        catch { }
        return false;
    }

    private static ISimpleAudioVolume? FindMediaAudioSession(string sourceApp)
    {
        try
        {
            var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
            if (enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out var device) != 0 || device == null)
            {
                return null;
            }

            var iidMgr = IID_IAudioSessionManager2;
            if (device.Activate(ref iidMgr, 1, IntPtr.Zero, out var mgrObj) != 0 || mgrObj is not IAudioSessionManager2 mgr)
            {
                return null;
            }

            if (mgr.GetSessionEnumerator(out var sessionEnum) != 0 || sessionEnum == null)
            {
                return null;
            }

            sessionEnum.GetCount(out int count);
            ISimpleAudioVolume? bestCandidate = null;

            string target = (sourceApp ?? "").Trim().ToLowerInvariant();
            if (target.EndsWith(".exe")) target = target[..^4];

            string[] knownMediaPlayers = ["librewolf", "chrome", "firefox", "msedge", "brave", "spotify", "vlc", "mpv", "opera", "vivaldi"];

            for (int i = 0; i < count; i++)
            {
                if (sessionEnum.GetSession(i, out var sessObj) == 0 && sessObj is ISimpleAudioVolume sav && sessObj is IAudioSessionControl2 ctl2)
                {
                    ctl2.GetState(out var state);
                    if (state == AudioSessionState.AudioSessionStateExpired) continue;

                    ctl2.GetProcessId(out uint pid);
                    if (pid == 0) continue;

                    string procName = "";
                    try
                    {
                        using var proc = Process.GetProcessById((int)pid);
                        procName = proc.ProcessName.ToLowerInvariant();
                    }
                    catch { }

                    bool matchesSource = !string.IsNullOrEmpty(target) && (procName.Contains(target) || target.Contains(procName));

                    if (matchesSource)
                    {
                        if (state == AudioSessionState.AudioSessionStateActive)
                        {
                            return sav;
                        }
                        bestCandidate ??= sav;
                    }
                    else if (state == AudioSessionState.AudioSessionStateActive)
                    {
                        foreach (var known in knownMediaPlayers)
                        {
                            if (procName.Contains(known))
                            {
                                bestCandidate ??= sav;
                                break;
                            }
                        }
                    }
                }
            }

            return bestCandidate;
        }
        catch { }

        return null;
    }

    public static (float Volume, bool IsMuted) GetMediaVolume(string sourceApp)
    {
        try
        {
            var sav = FindMediaAudioSession(sourceApp);
            if (sav != null)
            {
                sav.GetMasterVolume(out float vol);
                sav.GetMute(out bool isMuted);
                return ((float)Math.Round(vol * 100), isMuted);
            }
        }
        catch { }

        return GetMasterVolume();
    }

    public static void SetMediaVolume(string sourceApp, float volume)
    {
        try
        {
            var sav = FindMediaAudioSession(sourceApp);
            if (sav != null)
            {
                var guid = Guid.Empty;
                sav.SetMasterVolume(Math.Clamp(volume / 100f, 0f, 1f), ref guid);
                return;
            }
        }
        catch { }

        SetMasterVolume(volume);
    }

    public static bool ToggleMediaMute(string sourceApp)
    {
        try
        {
            var sav = FindMediaAudioSession(sourceApp);
            if (sav != null)
            {
                sav.GetMute(out bool isMuted);
                bool newMute = !isMuted;
                var guid = Guid.Empty;
                sav.SetMute(newMute, ref guid);
                return newMute;
            }
        }
        catch { }

        return ToggleMasterMute();
    }
}

#region COM Declarations

public enum EDataFlow
{
    eRender = 0,
    eCapture = 1,
    eAll = 2
}

public enum ERole
{
    eConsole = 0,
    eMultimedia = 1,
    eCommunications = 2
}

public enum AudioSessionState
{
    AudioSessionStateInactive = 0,
    AudioSessionStateActive = 1,
    AudioSessionStateExpired = 2
}

[StructLayout(LayoutKind.Sequential)]
public struct PropertyKey
{
    public Guid fmtid;
    public uint pid;

    public PropertyKey(Guid guid, uint id)
    {
        fmtid = guid;
        pid = id;
    }
}

[StructLayout(LayoutKind.Explicit)]
public struct PropVariant
{
    [FieldOffset(0)] public ushort vt;
    [FieldOffset(8)] public IntPtr pwszVal;
}

[ComImport]
[Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
public class MMDeviceEnumeratorComObject { }

[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMMDeviceEnumerator
{
    int EnumAudioEndpoints(EDataFlow dataFlow, uint dwStateMask, out IntPtr ppDevices);
    [PreserveSig]
    int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppDevice);
    int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string pwstrId, out IMMDevice ppDevice);
    int RegisterEndpointNotificationCallback(IntPtr pClient);
    int UnregisterEndpointNotificationCallback(IntPtr pClient);
}

[Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMMDevice
{
    [PreserveSig]
    int Activate(ref Guid iid, uint dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
    [PreserveSig]
    int OpenPropertyStore(uint stgmAccess, out IntPtr ppProperties);
    int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);
    int GetState(out uint pdwState);
}

[Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IPropertyStore
{
    int GetCount(out uint cProps);
    int GetAt(uint iProp, out PropertyKey pkey);
    [PreserveSig]
    int GetValue(ref PropertyKey key, out PropVariant pv);
    int SetValue(ref PropertyKey key, ref PropVariant pv);
    int Commit();
}

[Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IAudioEndpointVolume
{
    int RegisterControlChangeNotify(IntPtr pNotify);
    int UnregisterControlChangeNotify(IntPtr pNotify);
    int GetChannelCount(out uint pnChannelCount);
    int SetMasterVolumeLevel(float fLevelDB, ref Guid pguidEventContext);
    [PreserveSig]
    int SetMasterVolumeLevelScalar(float fLevel, ref Guid pguidEventContext);
    int GetMasterVolumeLevel(out float pfLevelDB);
    [PreserveSig]
    int GetMasterVolumeLevelScalar(out float pfLevel);
    int SetChannelVolumeLevel(uint nChannel, float fLevelDB, ref Guid pguidEventContext);
    int SetChannelVolumeLevelScalar(uint nChannel, float fLevel, ref Guid pguidEventContext);
    int GetChannelVolumeLevel(uint nChannel, out float pfLevelDB);
    int GetChannelVolumeLevelScalar(uint nChannel, out float pfLevel);
    [PreserveSig]
    int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, ref Guid pguidEventContext);
    [PreserveSig]
    int GetMute([MarshalAs(UnmanagedType.Bool)] out bool pbMute);
    int GetVolumeStepInfo(out uint pnStep, out uint pnStepCount);
    int VolumeStepUp(ref Guid pguidEventContext);
    int VolumeStepDown(ref Guid pguidEventContext);
    int QueryHardwareSupport(out uint pdwHardwareSupportMask);
    int GetVolumeRange(out float pflVolumeMindB, out float pflVolumeMaxdB, out float pflVolumeIncrementdB);
}

[Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IAudioSessionManager2
{
    int GetAudioSessionControl(ref Guid AudioSessionGuid, uint StreamFlags, out IntPtr SessionControl);
    int GetSimpleAudioVolume(ref Guid AudioSessionGuid, uint StreamFlags, out ISimpleAudioVolume AudioVolume);
    [PreserveSig]
    int GetSessionEnumerator(out IAudioSessionEnumerator SessionEnum);
    int RegisterSessionNotification(IntPtr NewSessionNotifications);
    int UnregisterSessionNotification(IntPtr NewSessionNotifications);
    int RegisterDuckNotification(string sessionID, IntPtr duckNotification);
    int UnregisterDuckNotification(IntPtr duckNotification);
}

[Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IAudioSessionEnumerator
{
    [PreserveSig]
    int GetCount(out int SessionCount);
    [PreserveSig]
    int GetSession(int SessionIndex, [MarshalAs(UnmanagedType.IUnknown)] out object Session);
}

[Guid("F4B1A599-7266-4319-A8CA-E70ACB11E8CD"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IAudioSessionControl
{
    [PreserveSig]
    int GetState(out AudioSessionState pRetVal);
    [PreserveSig]
    int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
    int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string Value, ref Guid EventContext);
    [PreserveSig]
    int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
    int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string Value, ref Guid EventContext);
    int GetGroupingParam(out Guid pRetVal);
    int SetGroupingParam(ref Guid Override, ref Guid EventContext);
    int RegisterAudioSessionNotification(IntPtr NewNotifications);
    int UnregisterAudioSessionNotification(IntPtr NewNotifications);
}

[Guid("bfb7ff88-7239-4fc9-8fa2-07c950be9c6d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IAudioSessionControl2 : IAudioSessionControl
{
    [PreserveSig] new int GetState(out AudioSessionState pRetVal);
    [PreserveSig] new int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
    new int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string Value, ref Guid EventContext);
    [PreserveSig] new int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
    new int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string Value, ref Guid EventContext);
    new int GetGroupingParam(out Guid pRetVal);
    new int SetGroupingParam(ref Guid Override, ref Guid EventContext);
    new int RegisterAudioSessionNotification(IntPtr NewNotifications);
    new int UnregisterAudioSessionNotification(IntPtr NewNotifications);

    [PreserveSig]
    int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
    [PreserveSig]
    int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
    [PreserveSig]
    int GetProcessId(out uint pRetVal);
    [PreserveSig]
    int IsSystemSoundsSession();
    int SetDuckingPreference([MarshalAs(UnmanagedType.Bool)] bool optOut);
}

[Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface ISimpleAudioVolume
{
    [PreserveSig]
    int SetMasterVolume(float fLevel, ref Guid EventContext);
    [PreserveSig]
    int GetMasterVolume(out float pfLevel);
    [PreserveSig]
    int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, ref Guid EventContext);
    [PreserveSig]
    int GetMute([MarshalAs(UnmanagedType.Bool)] out bool pbMute);
}

#endregion
