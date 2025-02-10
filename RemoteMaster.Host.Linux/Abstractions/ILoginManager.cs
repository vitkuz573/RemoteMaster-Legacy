// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Tmds.DBus;

namespace RemoteMaster.Host.Linux.Abstractions;

[DBusInterface("org.freedesktop.login1.Manager")]
public interface ILoginManager : IDBusObject
{
    Task<ObjectPath> GetSessionAsync(string SessionId);
    
    Task<ObjectPath> GetSessionByPIDAsync(uint Pid);
    
    Task<ObjectPath> GetUserAsync(uint Uid);
    
    Task<ObjectPath> GetUserByPIDAsync(uint Pid);
    
    Task<ObjectPath> GetSeatAsync(string SeatId);
    
    Task<(string, uint, string, string, ObjectPath)[]> ListSessionsAsync();
    
    Task<(uint, string, ObjectPath)[]> ListUsersAsync();
    
    Task<(string, ObjectPath)[]> ListSeatsAsync();
    
    Task<(string, string, string, string, uint, uint)[]> ListInhibitorsAsync();
    
    Task<(string sessionId, ObjectPath objectPath, string runtimePath, CloseSafeHandle fifoFd, uint uid, string seatId, uint vtnr, bool existing)> CreateSessionAsync(uint Uid, uint Pid, string Service, string Type, string Class, string Desktop, string SeatId, uint Vtnr, string Tty, string Display, bool Remote, string RemoteUser, string RemoteHost, (string, object)[] Properties);
    
    Task<(string sessionId, ObjectPath objectPath, string runtimePath, CloseSafeHandle fifoFd, uint uid, string seatId, uint vtnr, bool existing)> CreateSessionWithPIDFDAsync(uint Uid, CloseSafeHandle Pidfd, string Service, string Type, string Class, string Desktop, string SeatId, uint Vtnr, string Tty, string Display, bool Remote, string RemoteUser, string RemoteHost, ulong Flags, (string, object)[] Properties);
    
    Task ReleaseSessionAsync(string SessionId);
    
    Task ActivateSessionAsync(string SessionId);
    
    Task ActivateSessionOnSeatAsync(string SessionId, string SeatId);
    
    Task LockSessionAsync(string SessionId);
    
    Task UnlockSessionAsync(string SessionId);
    
    Task LockSessionsAsync();
    
    Task UnlockSessionsAsync();
    
    Task KillSessionAsync(string SessionId, string Who, int SignalNumber);
    
    Task KillUserAsync(uint Uid, int SignalNumber);
    
    Task TerminateSessionAsync(string SessionId);
    
    Task TerminateUserAsync(uint Uid);
    
    Task TerminateSeatAsync(string SeatId);
    
    Task SetUserLingerAsync(uint Uid, bool Enable, bool Interactive);
    
    Task AttachDeviceAsync(string SeatId, string SysfsPath, bool Interactive);
    
    Task FlushDevicesAsync(bool Interactive);
    
    Task PowerOffAsync(bool Interactive);
    
    Task PowerOffWithFlagsAsync(ulong Flags);
    
    Task RebootAsync(bool Interactive);
    
    Task RebootWithFlagsAsync(ulong Flags);
    
    Task HaltAsync(bool Interactive);
    
    Task HaltWithFlagsAsync(ulong Flags);
    
    Task SuspendAsync(bool Interactive);
    
    Task SuspendWithFlagsAsync(ulong Flags);
    
    Task HibernateAsync(bool Interactive);
    
    Task HibernateWithFlagsAsync(ulong Flags);
    
    Task HybridSleepAsync(bool Interactive);
    
    Task HybridSleepWithFlagsAsync(ulong Flags);
    
    Task SuspendThenHibernateAsync(bool Interactive);
    
    Task SuspendThenHibernateWithFlagsAsync(ulong Flags);
    
    Task<string> CanPowerOffAsync();
    
    Task<string> CanRebootAsync();
    
    Task<string> CanHaltAsync();
    
    Task<string> CanSuspendAsync();
    
    Task<string> CanHibernateAsync();
    
    Task<string> CanHybridSleepAsync();
    
    Task<string> CanSuspendThenHibernateAsync();
    
    Task ScheduleShutdownAsync(string Type, ulong Usec);
    
    Task<bool> CancelScheduledShutdownAsync();
    
    Task<CloseSafeHandle> InhibitAsync(string What, string Who, string Why, string Mode);
    
    Task<string> CanRebootParameterAsync();
    
    Task SetRebootParameterAsync(string Parameter);
    
    Task<string> CanRebootToFirmwareSetupAsync();
    
    Task SetRebootToFirmwareSetupAsync(bool Enable);
    
    Task<string> CanRebootToBootLoaderMenuAsync();
    
    Task SetRebootToBootLoaderMenuAsync(ulong Timeout);
    
    Task<string> CanRebootToBootLoaderEntryAsync();
    
    Task SetRebootToBootLoaderEntryAsync(string BootLoaderEntry);
    
    Task SetWallMessageAsync(string WallMessage, bool Enable);
    
    Task<IDisposable> WatchSessionNewAsync(Action<(string sessionId, ObjectPath objectPath)> handler, Action<Exception> onError = null);
    
    Task<IDisposable> WatchSessionRemovedAsync(Action<(string sessionId, ObjectPath objectPath)> handler, Action<Exception> onError = null);
    
    Task<IDisposable> WatchUserNewAsync(Action<(uint uid, ObjectPath objectPath)> handler, Action<Exception> onError = null);
    
    Task<IDisposable> WatchUserRemovedAsync(Action<(uint uid, ObjectPath objectPath)> handler, Action<Exception> onError = null);
    
    Task<IDisposable> WatchSeatNewAsync(Action<(string seatId, ObjectPath objectPath)> handler, Action<Exception> onError = null);
    
    Task<IDisposable> WatchSeatRemovedAsync(Action<(string seatId, ObjectPath objectPath)> handler, Action<Exception> onError = null);
    
    Task<IDisposable> WatchPrepareForShutdownAsync(Action<bool> handler, Action<Exception> onError = null);
    
    Task<IDisposable> WatchPrepareForShutdownWithMetadataAsync(Action<(bool start, IDictionary<string, object> metadata)> handler, Action<Exception> onError = null);
    
    Task<IDisposable> WatchPrepareForSleepAsync(Action<bool> handler, Action<Exception> onError = null);
    
    Task<T> GetAsync<T>(string prop);
    
    Task<LoginManagerProperties> GetAllAsync();
    
    Task SetAsync(string prop, object val);
    
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
}

[Dictionary]
public class LoginManagerProperties
{
    private bool _EnableWallMessages = default;

    public bool EnableWallMessages
    {
        get
        {
            return _EnableWallMessages;
        }

        set
        {
            _EnableWallMessages = (value);
        }
    }

    private string _WallMessage = default;

    public string WallMessage
    {
        get
        {
            return _WallMessage;
        }

        set
        {
            _WallMessage = (value);
        }
    }

    private uint _NAutoVTs = default;

    public uint NAutoVTs
    {
        get
        {
            return _NAutoVTs;
        }

        set
        {
            _NAutoVTs = (value);
        }
    }

    private string[] _KillOnlyUsers = default;

    public string[] KillOnlyUsers
    {
        get
        {
            return _KillOnlyUsers;
        }

        set
        {
            _KillOnlyUsers = (value);
        }
    }

    private string[] _KillExcludeUsers = default;

    public string[] KillExcludeUsers
    {
        get
        {
            return _KillExcludeUsers;
        }

        set
        {
            _KillExcludeUsers = (value);
        }
    }

    private bool _KillUserProcesses = default;

    public bool KillUserProcesses
    {
        get
        {
            return _KillUserProcesses;
        }

        set
        {
            _KillUserProcesses = (value);
        }
    }

    private string _RebootParameter = default;

    public string RebootParameter
    {
        get
        {
            return _RebootParameter;
        }

        set
        {
            _RebootParameter = (value);
        }
    }

    private bool _RebootToFirmwareSetup = default;

    public bool RebootToFirmwareSetup
    {
        get
        {
            return _RebootToFirmwareSetup;
        }

        set
        {
            _RebootToFirmwareSetup = (value);
        }
    }

    private ulong _RebootToBootLoaderMenu = default;

    public ulong RebootToBootLoaderMenu
    {
        get
        {
            return _RebootToBootLoaderMenu;
        }

        set
        {
            _RebootToBootLoaderMenu = (value);
        }
    }

    private string _RebootToBootLoaderEntry = default;

    public string RebootToBootLoaderEntry
    {
        get
        {
            return _RebootToBootLoaderEntry;
        }

        set
        {
            _RebootToBootLoaderEntry = (value);
        }
    }

    private string[] _BootLoaderEntries = default;

    public string[] BootLoaderEntries
    {
        get
        {
            return _BootLoaderEntries;
        }

        set
        {
            _BootLoaderEntries = (value);
        }
    }

    private bool _IdleHint = default;

    public bool IdleHint
    {
        get
        {
            return _IdleHint;
        }

        set
        {
            _IdleHint = (value);
        }
    }

    private ulong _IdleSinceHint = default;

    public ulong IdleSinceHint
    {
        get
        {
            return _IdleSinceHint;
        }

        set
        {
            _IdleSinceHint = (value);
        }
    }

    private ulong _IdleSinceHintMonotonic = default;

    public ulong IdleSinceHintMonotonic
    {
        get
        {
            return _IdleSinceHintMonotonic;
        }

        set
        {
            _IdleSinceHintMonotonic = (value);
        }
    }

    private string _BlockInhibited = default;

    public string BlockInhibited
    {
        get
        {
            return _BlockInhibited;
        }

        set
        {
            _BlockInhibited = (value);
        }
    }

    private string _DelayInhibited = default;

    public string DelayInhibited
    {
        get
        {
            return _DelayInhibited;
        }

        set
        {
            _DelayInhibited = (value);
        }
    }

    private ulong _InhibitDelayMaxUSec = default;

    public ulong InhibitDelayMaxUSec
    {
        get
        {
            return _InhibitDelayMaxUSec;
        }

        set
        {
            _InhibitDelayMaxUSec = (value);
        }
    }

    private ulong _UserStopDelayUSec = default;

    public ulong UserStopDelayUSec
    {
        get
        {
            return _UserStopDelayUSec;
        }

        set
        {
            _UserStopDelayUSec = (value);
        }
    }

    private string _HandlePowerKey = default;

    public string HandlePowerKey
    {
        get
        {
            return _HandlePowerKey;
        }

        set
        {
            _HandlePowerKey = (value);
        }
    }

    private string _HandlePowerKeyLongPress = default;

    public string HandlePowerKeyLongPress
    {
        get
        {
            return _HandlePowerKeyLongPress;
        }

        set
        {
            _HandlePowerKeyLongPress = (value);
        }
    }

    private string _HandleRebootKey = default;

    public string HandleRebootKey
    {
        get
        {
            return _HandleRebootKey;
        }

        set
        {
            _HandleRebootKey = (value);
        }
    }

    private string _HandleRebootKeyLongPress = default;

    public string HandleRebootKeyLongPress
    {
        get
        {
            return _HandleRebootKeyLongPress;
        }

        set
        {
            _HandleRebootKeyLongPress = (value);
        }
    }

    private string _HandleSuspendKey = default;

    public string HandleSuspendKey
    {
        get
        {
            return _HandleSuspendKey;
        }

        set
        {
            _HandleSuspendKey = (value);
        }
    }

    private string _HandleSuspendKeyLongPress = default;

    public string HandleSuspendKeyLongPress
    {
        get
        {
            return _HandleSuspendKeyLongPress;
        }

        set
        {
            _HandleSuspendKeyLongPress = (value);
        }
    }

    private string _HandleHibernateKey = default;

    public string HandleHibernateKey
    {
        get
        {
            return _HandleHibernateKey;
        }

        set
        {
            _HandleHibernateKey = (value);
        }
    }

    private string _HandleHibernateKeyLongPress = default;

    public string HandleHibernateKeyLongPress
    {
        get
        {
            return _HandleHibernateKeyLongPress;
        }

        set
        {
            _HandleHibernateKeyLongPress = (value);
        }
    }

    private string _HandleLidSwitch = default;

    public string HandleLidSwitch
    {
        get
        {
            return _HandleLidSwitch;
        }

        set
        {
            _HandleLidSwitch = (value);
        }
    }

    private string _HandleLidSwitchExternalPower = default;

    public string HandleLidSwitchExternalPower
    {
        get
        {
            return _HandleLidSwitchExternalPower;
        }

        set
        {
            _HandleLidSwitchExternalPower = (value);
        }
    }

    private string _HandleLidSwitchDocked = default;

    public string HandleLidSwitchDocked
    {
        get
        {
            return _HandleLidSwitchDocked;
        }

        set
        {
            _HandleLidSwitchDocked = (value);
        }
    }

    private ulong _HoldoffTimeoutUSec = default;

    public ulong HoldoffTimeoutUSec
    {
        get
        {
            return _HoldoffTimeoutUSec;
        }

        set
        {
            _HoldoffTimeoutUSec = (value);
        }
    }

    private string _IdleAction = default;

    public string IdleAction
    {
        get
        {
            return _IdleAction;
        }

        set
        {
            _IdleAction = (value);
        }
    }

    private ulong _IdleActionUSec = default;

    public ulong IdleActionUSec
    {
        get
        {
            return _IdleActionUSec;
        }

        set
        {
            _IdleActionUSec = (value);
        }
    }

    private bool _PreparingForShutdown = default;

    public bool PreparingForShutdown
    {
        get
        {
            return _PreparingForShutdown;
        }

        set
        {
            _PreparingForShutdown = (value);
        }
    }

    private bool _PreparingForSleep = default;

    public bool PreparingForSleep
    {
        get
        {
            return _PreparingForSleep;
        }

        set
        {
            _PreparingForSleep = (value);
        }
    }

    private (string, ulong) _ScheduledShutdown = default;

    public (string, ulong) ScheduledShutdown
    {
        get
        {
            return _ScheduledShutdown;
        }

        set
        {
            _ScheduledShutdown = (value);
        }
    }

    private bool _Docked = default;

    public bool Docked
    {
        get
        {
            return _Docked;
        }

        set
        {
            _Docked = (value);
        }
    }

    private bool _LidClosed = default;

    public bool LidClosed
    {
        get
        {
            return _LidClosed;
        }

        set
        {
            _LidClosed = (value);
        }
    }

    private bool _OnExternalPower = default;

    public bool OnExternalPower
    {
        get
        {
            return _OnExternalPower;
        }

        set
        {
            _OnExternalPower = (value);
        }
    }

    private bool _RemoveIPC = default;

    public bool RemoveIPC
    {
        get
        {
            return _RemoveIPC;
        }

        set
        {
            _RemoveIPC = (value);
        }
    }

    private ulong _RuntimeDirectorySize = default;

    public ulong RuntimeDirectorySize
    {
        get
        {
            return _RuntimeDirectorySize;
        }

        set
        {
            _RuntimeDirectorySize = (value);
        }
    }

    private ulong _RuntimeDirectoryInodesMax = default;

    public ulong RuntimeDirectoryInodesMax
    {
        get
        {
            return _RuntimeDirectoryInodesMax;
        }

        set
        {
            _RuntimeDirectoryInodesMax = (value);
        }
    }

    private ulong _InhibitorsMax = default;

    public ulong InhibitorsMax
    {
        get
        {
            return _InhibitorsMax;
        }

        set
        {
            _InhibitorsMax = (value);
        }
    }

    private ulong _NCurrentInhibitors = default;

    public ulong NCurrentInhibitors
    {
        get
        {
            return _NCurrentInhibitors;
        }

        set
        {
            _NCurrentInhibitors = (value);
        }
    }

    private ulong _SessionsMax = default;

    public ulong SessionsMax
    {
        get
        {
            return _SessionsMax;
        }

        set
        {
            _SessionsMax = (value);
        }
    }

    private ulong _NCurrentSessions = default;

    public ulong NCurrentSessions
    {
        get
        {
            return _NCurrentSessions;
        }

        set
        {
            _NCurrentSessions = (value);
        }
    }

    private ulong _StopIdleSessionUSec = default;

    public ulong StopIdleSessionUSec
    {
        get
        {
            return _StopIdleSessionUSec;
        }

        set
        {
            _StopIdleSessionUSec = (value);
        }
    }
}

public static class LoginManagerExtensions
{
    public static Task<bool> GetEnableWallMessagesAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>("EnableWallMessages");
    }
    
    public static Task<string> GetWallMessageAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("WallMessage");
    }
    
    public static Task<uint> GetNAutoVTsAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>("NAutoVTs");
    }
    
    public static Task<string[]> GetKillOnlyUsersAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>("KillOnlyUsers");
    }
    
    public static Task<string[]> GetKillExcludeUsersAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>("KillExcludeUsers");
    }
    
    public static Task<bool> GetKillUserProcessesAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>("KillUserProcesses");
    }
    
    public static Task<string> GetRebootParameterAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("RebootParameter");
    }

    public static Task<bool> GetRebootToFirmwareSetupAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>("RebootToFirmwareSetup");
    }

    public static Task<ulong> GetRebootToBootLoaderMenuAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("RebootToBootLoaderMenu");
    }

    public static Task<string> GetRebootToBootLoaderEntryAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("RebootToBootLoaderEntry");
    }

    public static Task<string[]> GetBootLoaderEntriesAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>("BootLoaderEntries");
    }

    public static Task<bool> GetIdleHintAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>("IdleHint");
    }

    public static Task<ulong> GetIdleSinceHintAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("IdleSinceHint");
    }

    public static Task<ulong> GetIdleSinceHintMonotonicAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("IdleSinceHintMonotonic");
    }

    public static Task<string> GetBlockInhibitedAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("BlockInhibited");
    }

    public static Task<string> GetDelayInhibitedAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("DelayInhibited");
    }

    public static Task<ulong> GetInhibitDelayMaxUSecAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("InhibitDelayMaxUSec");
    }

    public static Task<ulong> GetUserStopDelayUSecAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("UserStopDelayUSec");
    }

    public static Task<string> GetHandlePowerKeyAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("HandlePowerKey");
    }

    public static Task<string> GetHandlePowerKeyLongPressAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("HandlePowerKeyLongPress");
    }

    public static Task<string> GetHandleRebootKeyAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("HandleRebootKey");
    }

    public static Task<string> GetHandleRebootKeyLongPressAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("HandleRebootKeyLongPress");
    }

    public static Task<string> GetHandleSuspendKeyAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("HandleSuspendKey");
    }

    public static Task<string> GetHandleSuspendKeyLongPressAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("HandleSuspendKeyLongPress");
    }

    public static Task<string> GetHandleHibernateKeyAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("HandleHibernateKey");
    }

    public static Task<string> GetHandleHibernateKeyLongPressAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("HandleHibernateKeyLongPress");
    }

    public static Task<string> GetHandleLidSwitchAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("HandleLidSwitch");
    }

    public static Task<string> GetHandleLidSwitchExternalPowerAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("HandleLidSwitchExternalPower");
    }

    public static Task<string> GetHandleLidSwitchDockedAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("HandleLidSwitchDocked");
    }

    public static Task SetEnableWallMessagesAsync(this ILoginManager o, bool val)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.SetAsync("EnableWallMessages", val);
    }

    public static Task SetWallMessageAsync(this ILoginManager o, string val)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.SetAsync("WallMessage", val);
    }
}
