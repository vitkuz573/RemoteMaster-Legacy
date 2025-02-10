// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Tmds.DBus;

namespace RemoteMaster.Host.Linux.Abstractions;

[DBusInterface("org.freedesktop.systemd1.Manager")]
public interface ISystemdManager : IDBusObject
{
    Task<ObjectPath> GetUnitAsync(string Name);
    
    Task<ObjectPath> GetUnitByPIDAsync(uint Pid);
    
    Task<ObjectPath> GetUnitByInvocationIDAsync(byte[] InvocationId);
    
    Task<ObjectPath> GetUnitByControlGroupAsync(string Cgroup);
    
    Task<(ObjectPath unit, string unitId, byte[] invocationId)> GetUnitByPIDFDAsync(CloseSafeHandle Pidfd);
    
    Task<ObjectPath> LoadUnitAsync(string Name);
    
    Task<ObjectPath> StartUnitAsync(string Name, string Mode);
    
    Task<ObjectPath> StartUnitWithFlagsAsync(string Name, string Mode, ulong Flags);
    
    Task<ObjectPath> StartUnitReplaceAsync(string OldUnit, string NewUnit, string Mode);
    
    Task<ObjectPath> StopUnitAsync(string Name, string Mode);
    
    Task<ObjectPath> ReloadUnitAsync(string Name, string Mode);
    
    Task<ObjectPath> RestartUnitAsync(string Name, string Mode);
    
    Task<ObjectPath> TryRestartUnitAsync(string Name, string Mode);
    
    Task<ObjectPath> ReloadOrRestartUnitAsync(string Name, string Mode);
    
    Task<ObjectPath> ReloadOrTryRestartUnitAsync(string Name, string Mode);
    
    Task<(uint jobId, ObjectPath jobPath, string unitId, ObjectPath unitPath, string jobType, (uint, ObjectPath, string, ObjectPath, string)[] affectedJobs)> EnqueueUnitJobAsync(string Name, string JobType, string JobMode);
    
    Task KillUnitAsync(string Name, string Whom, int Signal);
    
    Task QueueSignalUnitAsync(string Name, string Whom, int Signal, int Value);
    
    Task CleanUnitAsync(string Name, string[] Mask);
    
    Task FreezeUnitAsync(string Name);
    
    Task ThawUnitAsync(string Name);
    
    Task ResetFailedUnitAsync(string Name);
    
    Task SetUnitPropertiesAsync(string Name, bool Runtime, (string, object)[] Properties);
    
    Task BindMountUnitAsync(string Name, string Source, string Destination, bool ReadOnly, bool Mkdir);
    
    Task MountImageUnitAsync(string Name, string Source, string Destination, bool ReadOnly, bool Mkdir, (string, string)[] Options);
    
    Task RefUnitAsync(string Name);
    
    Task UnrefUnitAsync(string Name);
    
    Task<ObjectPath> StartTransientUnitAsync(string Name, string Mode, (string, object)[] Properties, (string, (string, object)[])[] Aux);
    
    Task<(string, uint, string)[]> GetUnitProcessesAsync(string Name);
    
    Task AttachProcessesToUnitAsync(string UnitName, string Subcgroup, uint[] Pids);
    
    Task AbandonScopeAsync(string Name);
    
    Task<ObjectPath> GetJobAsync(uint Id);
    
    Task<(uint, string, string, string, ObjectPath, ObjectPath)[]> GetJobAfterAsync(uint Id);
    
    Task<(uint, string, string, string, ObjectPath, ObjectPath)[]> GetJobBeforeAsync(uint Id);
    
    Task CancelJobAsync(uint Id);
    
    Task ClearJobsAsync();
    
    Task ResetFailedAsync();
    
    Task SetShowStatusAsync(string Mode);
    
    Task<(string, string, string, string, string, string, ObjectPath, uint, string, ObjectPath)[]> ListUnitsAsync();
    
    Task<(string, string, string, string, string, string, ObjectPath, uint, string, ObjectPath)[]> ListUnitsFilteredAsync(string[] States);
    
    Task<(string, string, string, string, string, string, ObjectPath, uint, string, ObjectPath)[]> ListUnitsByPatternsAsync(string[] States, string[] Patterns);
    
    Task<(string, string, string, string, string, string, ObjectPath, uint, string, ObjectPath)[]> ListUnitsByNamesAsync(string[] Names);
    
    Task<(uint, string, string, string, ObjectPath, ObjectPath)[]> ListJobsAsync();
    
    Task SubscribeAsync();
    
    Task UnsubscribeAsync();
    
    Task<string> DumpAsync();
    
    Task<string> DumpUnitsMatchingPatternsAsync(string[] Patterns);
    
    Task<CloseSafeHandle> DumpByFileDescriptorAsync();
    
    Task<CloseSafeHandle> DumpUnitsMatchingPatternsByFileDescriptorAsync(string[] Patterns);
    
    Task ReloadAsync();
    
    Task ReexecuteAsync();
    
    Task ExitAsync();
    
    Task RebootAsync();
    
    Task SoftRebootAsync(string NewRoot);
    
    Task PowerOffAsync();
    
    Task HaltAsync();
    
    Task KExecAsync();
    
    Task SwitchRootAsync(string NewRoot, string Init);
    
    Task SetEnvironmentAsync(string[] Assignments);
    
    Task UnsetEnvironmentAsync(string[] Names);
    
    Task UnsetAndSetEnvironmentAsync(string[] Names, string[] Assignments);
    
    Task<ObjectPath[]> EnqueueMarkedJobsAsync();
    
    Task<(string, string)[]> ListUnitFilesAsync();
    
    Task<(string, string)[]> ListUnitFilesByPatternsAsync(string[] States, string[] Patterns);
    
    Task<string> GetUnitFileStateAsync(string File);
    
    Task<(bool carriesInstallInfo, (string, string, string)[] changes)> EnableUnitFilesAsync(string[] Files, bool Runtime, bool Force);
    
    Task<(string, string, string)[]> DisableUnitFilesAsync(string[] Files, bool Runtime);
    
    Task<(bool carriesInstallInfo, (string, string, string)[] changes)> EnableUnitFilesWithFlagsAsync(string[] Files, ulong Flags);
    
    Task<(string, string, string)[]> DisableUnitFilesWithFlagsAsync(string[] Files, ulong Flags);
    
    Task<(bool carriesInstallInfo, (string, string, string)[] changes)> DisableUnitFilesWithFlagsAndInstallInfoAsync(string[] Files, ulong Flags);
    
    Task<(bool carriesInstallInfo, (string, string, string)[] changes)> ReenableUnitFilesAsync(string[] Files, bool Runtime, bool Force);
    
    Task<(string, string, string)[]> LinkUnitFilesAsync(string[] Files, bool Runtime, bool Force);
    
    Task<(bool carriesInstallInfo, (string, string, string)[] changes)> PresetUnitFilesAsync(string[] Files, bool Runtime, bool Force);
    
    Task<(bool carriesInstallInfo, (string, string, string)[] changes)> PresetUnitFilesWithModeAsync(string[] Files, string Mode, bool Runtime, bool Force);
    
    Task<(string, string, string)[]> MaskUnitFilesAsync(string[] Files, bool Runtime, bool Force);
    
    Task<(string, string, string)[]> UnmaskUnitFilesAsync(string[] Files, bool Runtime);
    
    Task<(string, string, string)[]> RevertUnitFilesAsync(string[] Files);
    
    Task<(string, string, string)[]> SetDefaultTargetAsync(string Name, bool Force);
    
    Task<string> GetDefaultTargetAsync();
    
    Task<(string, string, string)[]> PresetAllUnitFilesAsync(string Mode, bool Runtime, bool Force);
    
    Task<(string, string, string)[]> AddDependencyUnitFilesAsync(string[] Files, string Target, string Type, bool Runtime, bool Force);
    
    Task<string[]> GetUnitFileLinksAsync(string Name, bool Runtime);
    
    Task SetExitCodeAsync(byte Number);
    
    Task<uint> LookupDynamicUserByNameAsync(string Name);
    
    Task<string> LookupDynamicUserByUIDAsync(uint Uid);
    
    Task<(uint, string)[]> GetDynamicUsersAsync();
    
    Task<(string, uint, uint, uint, ulong, uint, uint, string, uint)[]> DumpUnitFileDescriptorStoreAsync(string Name);
    
    Task<IDisposable> WatchUnitNewAsync(Action<(string id, ObjectPath unit)> handler, Action<Exception> onError = null);
    
    Task<IDisposable> WatchUnitRemovedAsync(Action<(string id, ObjectPath unit)> handler, Action<Exception> onError = null);
    
    Task<IDisposable> WatchJobNewAsync(Action<(uint id, ObjectPath job, string unit)> handler, Action<Exception> onError = null);
    
    Task<IDisposable> WatchJobRemovedAsync(Action<(uint id, ObjectPath job, string unit, string result)> handler, Action<Exception> onError = null);
    
    Task<IDisposable> WatchStartupFinishedAsync(Action<(ulong firmware, ulong loader, ulong kernel, ulong initrd, ulong userspace, ulong total)> handler, Action<Exception> onError = null);
    
    Task<IDisposable> WatchUnitFilesChangedAsync(Action handler, Action<Exception> onError = null);
    
    Task<IDisposable> WatchReloadingAsync(Action<bool> handler, Action<Exception> onError = null);
    
    Task<T> GetAsync<T>(string prop);
    
    Task<ManagerProperties> GetAllAsync();
    
    Task SetAsync(string prop, object val);
    
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
}

[Dictionary]
public class ManagerProperties
{
    private string _Version = default;

    public string Version
    {
        get
        {
            return _Version;
        }

        set
        {
            _Version = (value);
        }
    }

    private string _Features = default;

    public string Features
    {
        get
        {
            return _Features;
        }

        set
        {
            _Features = (value);
        }
    }

    private string _Virtualization = default;

    public string Virtualization
    {
        get
        {
            return _Virtualization;
        }

        set
        {
            _Virtualization = (value);
        }
    }

    private string _ConfidentialVirtualization = default;

    public string ConfidentialVirtualization
    {
        get
        {
            return _ConfidentialVirtualization;
        }

        set
        {
            _ConfidentialVirtualization = (value);
        }
    }

    private string _Architecture = default;

    public string Architecture
    {
        get
        {
            return _Architecture;
        }

        set
        {
            _Architecture = (value);
        }
    }

    private string _Tainted = default;

    public string Tainted
    {
        get
        {
            return _Tainted;
        }

        set
        {
            _Tainted = (value);
        }
    }

    private ulong _FirmwareTimestamp = default;

    public ulong FirmwareTimestamp
    {
        get
        {
            return _FirmwareTimestamp;
        }

        set
        {
            _FirmwareTimestamp = (value);
        }
    }

    private ulong _FirmwareTimestampMonotonic = default;

    public ulong FirmwareTimestampMonotonic
    {
        get
        {
            return _FirmwareTimestampMonotonic;
        }

        set
        {
            _FirmwareTimestampMonotonic = (value);
        }
    }

    private ulong _LoaderTimestamp = default;

    public ulong LoaderTimestamp
    {
        get
        {
            return _LoaderTimestamp;
        }

        set
        {
            _LoaderTimestamp = (value);
        }
    }

    private ulong _LoaderTimestampMonotonic = default;

    public ulong LoaderTimestampMonotonic
    {
        get
        {
            return _LoaderTimestampMonotonic;
        }

        set
        {
            _LoaderTimestampMonotonic = (value);
        }
    }

    private ulong _KernelTimestamp = default;

    public ulong KernelTimestamp
    {
        get
        {
            return _KernelTimestamp;
        }

        set
        {
            _KernelTimestamp = (value);
        }
    }

    private ulong _KernelTimestampMonotonic = default;

    public ulong KernelTimestampMonotonic
    {
        get
        {
            return _KernelTimestampMonotonic;
        }

        set
        {
            _KernelTimestampMonotonic = (value);
        }
    }

    private ulong _InitRDTimestamp = default;

    public ulong InitRDTimestamp
    {
        get
        {
            return _InitRDTimestamp;
        }

        set
        {
            _InitRDTimestamp = (value);
        }
    }

    private ulong _InitRDTimestampMonotonic = default;

    public ulong InitRDTimestampMonotonic
    {
        get
        {
            return _InitRDTimestampMonotonic;
        }

        set
        {
            _InitRDTimestampMonotonic = (value);
        }
    }

    private ulong _UserspaceTimestamp = default;

    public ulong UserspaceTimestamp
    {
        get
        {
            return _UserspaceTimestamp;
        }

        set
        {
            _UserspaceTimestamp = (value);
        }
    }

    private ulong _UserspaceTimestampMonotonic = default;

    public ulong UserspaceTimestampMonotonic
    {
        get
        {
            return _UserspaceTimestampMonotonic;
        }

        set
        {
            _UserspaceTimestampMonotonic = (value);
        }
    }

    private ulong _FinishTimestamp = default;

    public ulong FinishTimestamp
    {
        get
        {
            return _FinishTimestamp;
        }

        set
        {
            _FinishTimestamp = (value);
        }
    }

    private ulong _FinishTimestampMonotonic = default;

    public ulong FinishTimestampMonotonic
    {
        get
        {
            return _FinishTimestampMonotonic;
        }

        set
        {
            _FinishTimestampMonotonic = (value);
        }
    }

    private ulong _SecurityStartTimestamp = default;

    public ulong SecurityStartTimestamp
    {
        get
        {
            return _SecurityStartTimestamp;
        }

        set
        {
            _SecurityStartTimestamp = (value);
        }
    }

    private ulong _SecurityStartTimestampMonotonic = default;

    public ulong SecurityStartTimestampMonotonic
    {
        get
        {
            return _SecurityStartTimestampMonotonic;
        }

        set
        {
            _SecurityStartTimestampMonotonic = (value);
        }
    }

    private ulong _SecurityFinishTimestamp = default;

    public ulong SecurityFinishTimestamp
    {
        get
        {
            return _SecurityFinishTimestamp;
        }

        set
        {
            _SecurityFinishTimestamp = (value);
        }
    }

    private ulong _SecurityFinishTimestampMonotonic = default;

    public ulong SecurityFinishTimestampMonotonic
    {
        get
        {
            return _SecurityFinishTimestampMonotonic;
        }

        set
        {
            _SecurityFinishTimestampMonotonic = (value);
        }
    }

    private ulong _GeneratorsStartTimestamp = default;

    public ulong GeneratorsStartTimestamp
    {
        get
        {
            return _GeneratorsStartTimestamp;
        }

        set
        {
            _GeneratorsStartTimestamp = (value);
        }
    }

    private ulong _GeneratorsStartTimestampMonotonic = default;

    public ulong GeneratorsStartTimestampMonotonic
    {
        get
        {
            return _GeneratorsStartTimestampMonotonic;
        }

        set
        {
            _GeneratorsStartTimestampMonotonic = (value);
        }
    }

    private ulong _GeneratorsFinishTimestamp = default;

    public ulong GeneratorsFinishTimestamp
    {
        get
        {
            return _GeneratorsFinishTimestamp;
        }

        set
        {
            _GeneratorsFinishTimestamp = (value);
        }
    }

    private ulong _GeneratorsFinishTimestampMonotonic = default;

    public ulong GeneratorsFinishTimestampMonotonic
    {
        get
        {
            return _GeneratorsFinishTimestampMonotonic;
        }

        set
        {
            _GeneratorsFinishTimestampMonotonic = (value);
        }
    }

    private ulong _UnitsLoadStartTimestamp = default;

    public ulong UnitsLoadStartTimestamp
    {
        get
        {
            return _UnitsLoadStartTimestamp;
        }

        set
        {
            _UnitsLoadStartTimestamp = (value);
        }
    }

    private ulong _UnitsLoadStartTimestampMonotonic = default;

    public ulong UnitsLoadStartTimestampMonotonic
    {
        get
        {
            return _UnitsLoadStartTimestampMonotonic;
        }

        set
        {
            _UnitsLoadStartTimestampMonotonic = (value);
        }
    }

    private ulong _UnitsLoadFinishTimestamp = default;

    public ulong UnitsLoadFinishTimestamp
    {
        get
        {
            return _UnitsLoadFinishTimestamp;
        }

        set
        {
            _UnitsLoadFinishTimestamp = (value);
        }
    }

    private ulong _UnitsLoadFinishTimestampMonotonic = default;

    public ulong UnitsLoadFinishTimestampMonotonic
    {
        get
        {
            return _UnitsLoadFinishTimestampMonotonic;
        }

        set
        {
            _UnitsLoadFinishTimestampMonotonic = (value);
        }
    }

    private ulong _UnitsLoadTimestamp = default;

    public ulong UnitsLoadTimestamp
    {
        get
        {
            return _UnitsLoadTimestamp;
        }

        set
        {
            _UnitsLoadTimestamp = (value);
        }
    }

    private ulong _UnitsLoadTimestampMonotonic = default;

    public ulong UnitsLoadTimestampMonotonic
    {
        get
        {
            return _UnitsLoadTimestampMonotonic;
        }

        set
        {
            _UnitsLoadTimestampMonotonic = (value);
        }
    }

    private ulong _InitRDSecurityStartTimestamp = default;

    public ulong InitRDSecurityStartTimestamp
    {
        get
        {
            return _InitRDSecurityStartTimestamp;
        }

        set
        {
            _InitRDSecurityStartTimestamp = (value);
        }
    }

    private ulong _InitRDSecurityStartTimestampMonotonic = default;

    public ulong InitRDSecurityStartTimestampMonotonic
    {
        get
        {
            return _InitRDSecurityStartTimestampMonotonic;
        }

        set
        {
            _InitRDSecurityStartTimestampMonotonic = (value);
        }
    }

    private ulong _InitRDSecurityFinishTimestamp = default;

    public ulong InitRDSecurityFinishTimestamp
    {
        get
        {
            return _InitRDSecurityFinishTimestamp;
        }

        set
        {
            _InitRDSecurityFinishTimestamp = (value);
        }
    }

    private ulong _InitRDSecurityFinishTimestampMonotonic = default;

    public ulong InitRDSecurityFinishTimestampMonotonic
    {
        get
        {
            return _InitRDSecurityFinishTimestampMonotonic;
        }

        set
        {
            _InitRDSecurityFinishTimestampMonotonic = (value);
        }
    }

    private ulong _InitRDGeneratorsStartTimestamp = default;

    public ulong InitRDGeneratorsStartTimestamp
    {
        get
        {
            return _InitRDGeneratorsStartTimestamp;
        }

        set
        {
            _InitRDGeneratorsStartTimestamp = (value);
        }
    }

    private ulong _InitRDGeneratorsStartTimestampMonotonic = default;

    public ulong InitRDGeneratorsStartTimestampMonotonic
    {
        get
        {
            return _InitRDGeneratorsStartTimestampMonotonic;
        }

        set
        {
            _InitRDGeneratorsStartTimestampMonotonic = (value);
        }
    }

    private ulong _InitRDGeneratorsFinishTimestamp = default;

    public ulong InitRDGeneratorsFinishTimestamp
    {
        get
        {
            return _InitRDGeneratorsFinishTimestamp;
        }

        set
        {
            _InitRDGeneratorsFinishTimestamp = (value);
        }
    }

    private ulong _InitRDGeneratorsFinishTimestampMonotonic = default;

    public ulong InitRDGeneratorsFinishTimestampMonotonic
    {
        get
        {
            return _InitRDGeneratorsFinishTimestampMonotonic;
        }

        set
        {
            _InitRDGeneratorsFinishTimestampMonotonic = (value);
        }
    }

    private ulong _InitRDUnitsLoadStartTimestamp = default;

    public ulong InitRDUnitsLoadStartTimestamp
    {
        get
        {
            return _InitRDUnitsLoadStartTimestamp;
        }

        set
        {
            _InitRDUnitsLoadStartTimestamp = (value);
        }
    }

    private ulong _InitRDUnitsLoadStartTimestampMonotonic = default;

    public ulong InitRDUnitsLoadStartTimestampMonotonic
    {
        get
        {
            return _InitRDUnitsLoadStartTimestampMonotonic;
        }

        set
        {
            _InitRDUnitsLoadStartTimestampMonotonic = (value);
        }
    }

    private ulong _InitRDUnitsLoadFinishTimestamp = default;

    public ulong InitRDUnitsLoadFinishTimestamp
    {
        get
        {
            return _InitRDUnitsLoadFinishTimestamp;
        }

        set
        {
            _InitRDUnitsLoadFinishTimestamp = (value);
        }
    }

    private ulong _InitRDUnitsLoadFinishTimestampMonotonic = default;

    public ulong InitRDUnitsLoadFinishTimestampMonotonic
    {
        get
        {
            return _InitRDUnitsLoadFinishTimestampMonotonic;
        }

        set
        {
            _InitRDUnitsLoadFinishTimestampMonotonic = (value);
        }
    }

    private string _LogLevel = default;

    public string LogLevel
    {
        get
        {
            return _LogLevel;
        }

        set
        {
            _LogLevel = (value);
        }
    }

    private string _LogTarget = default;

    public string LogTarget
    {
        get
        {
            return _LogTarget;
        }

        set
        {
            _LogTarget = (value);
        }
    }

    private uint _NNames = default;

    public uint NNames
    {
        get
        {
            return _NNames;
        }

        set
        {
            _NNames = (value);
        }
    }

    private uint _NFailedUnits = default;

    public uint NFailedUnits
    {
        get
        {
            return _NFailedUnits;
        }

        set
        {
            _NFailedUnits = (value);
        }
    }

    private uint _NJobs = default;

    public uint NJobs
    {
        get
        {
            return _NJobs;
        }

        set
        {
            _NJobs = (value);
        }
    }

    private uint _NInstalledJobs = default;

    public uint NInstalledJobs
    {
        get
        {
            return _NInstalledJobs;
        }

        set
        {
            _NInstalledJobs = (value);
        }
    }

    private uint _NFailedJobs = default;

    public uint NFailedJobs
    {
        get
        {
            return _NFailedJobs;
        }

        set
        {
            _NFailedJobs = (value);
        }
    }

    private double _Progress = default;

    public double Progress
    {
        get
        {
            return _Progress;
        }

        set
        {
            _Progress = (value);
        }
    }

    private string[] _Environment = default;
    public string[] Environment
    {
        get
        {
            return _Environment;
        }

        set
        {
            _Environment = (value);
        }
    }

    private bool _ConfirmSpawn = default;

    public bool ConfirmSpawn
    {
        get
        {
            return _ConfirmSpawn;
        }

        set
        {
            _ConfirmSpawn = (value);
        }
    }

    private bool _ShowStatus = default;

    public bool ShowStatus
    {
        get
        {
            return _ShowStatus;
        }

        set
        {
            _ShowStatus = (value);
        }
    }

    private string[] _UnitPath = default;
    public string[] UnitPath
    {
        get
        {
            return _UnitPath;
        }

        set
        {
            _UnitPath = (value);
        }
    }

    private string _DefaultStandardOutput = default;

    public string DefaultStandardOutput
    {
        get
        {
            return _DefaultStandardOutput;
        }

        set
        {
            _DefaultStandardOutput = (value);
        }
    }

    private string _DefaultStandardError = default;

    public string DefaultStandardError
    {
        get
        {
            return _DefaultStandardError;
        }

        set
        {
            _DefaultStandardError = (value);
        }
    }

    private string _WatchdogDevice = default;

    public string WatchdogDevice
    {
        get
        {
            return _WatchdogDevice;
        }

        set
        {
            _WatchdogDevice = (value);
        }
    }

    private ulong _WatchdogLastPingTimestamp = default;

    public ulong WatchdogLastPingTimestamp
    {
        get
        {
            return _WatchdogLastPingTimestamp;
        }

        set
        {
            _WatchdogLastPingTimestamp = (value);
        }
    }

    private ulong _WatchdogLastPingTimestampMonotonic = default;

    public ulong WatchdogLastPingTimestampMonotonic
    {
        get
        {
            return _WatchdogLastPingTimestampMonotonic;
        }

        set
        {
            _WatchdogLastPingTimestampMonotonic = (value);
        }
    }

    private ulong _RuntimeWatchdogUSec = default;

    public ulong RuntimeWatchdogUSec
    {
        get
        {
            return _RuntimeWatchdogUSec;
        }

        set
        {
            _RuntimeWatchdogUSec = (value);
        }
    }

    private ulong _RuntimeWatchdogPreUSec = default;

    public ulong RuntimeWatchdogPreUSec
    {
        get
        {
            return _RuntimeWatchdogPreUSec;
        }

        set
        {
            _RuntimeWatchdogPreUSec = (value);
        }
    }

    private string _RuntimeWatchdogPreGovernor = default;

    public string RuntimeWatchdogPreGovernor
    {
        get
        {
            return _RuntimeWatchdogPreGovernor;
        }

        set
        {
            _RuntimeWatchdogPreGovernor = (value);
        }
    }

    private ulong _RebootWatchdogUSec = default;

    public ulong RebootWatchdogUSec
    {
        get
        {
            return _RebootWatchdogUSec;
        }

        set
        {
            _RebootWatchdogUSec = (value);
        }
    }

    private ulong _KExecWatchdogUSec = default;

    public ulong KExecWatchdogUSec
    {
        get
        {
            return _KExecWatchdogUSec;
        }

        set
        {
            _KExecWatchdogUSec = (value);
        }
    }

    private bool _ServiceWatchdogs = default;

    public bool ServiceWatchdogs
    {
        get
        {
            return _ServiceWatchdogs;
        }

        set
        {
            _ServiceWatchdogs = (value);
        }
    }

    private string _ControlGroup = default;

    public string ControlGroup
    {
        get
        {
            return _ControlGroup;
        }

        set
        {
            _ControlGroup = (value);
        }
    }

    private string _SystemState = default;

    public string SystemState
    {
        get
        {
            return _SystemState;
        }

        set
        {
            _SystemState = (value);
        }
    }

    private byte _ExitCode = default;

    public byte ExitCode
    {
        get
        {
            return _ExitCode;
        }

        set
        {
            _ExitCode = (value);
        }
    }

    private ulong _DefaultTimerAccuracyUSec = default;

    public ulong DefaultTimerAccuracyUSec
    {
        get
        {
            return _DefaultTimerAccuracyUSec;
        }

        set
        {
            _DefaultTimerAccuracyUSec = (value);
        }
    }

    private ulong _DefaultTimeoutStartUSec = default;

    public ulong DefaultTimeoutStartUSec
    {
        get
        {
            return _DefaultTimeoutStartUSec;
        }

        set
        {
            _DefaultTimeoutStartUSec = (value);
        }
    }

    private ulong _DefaultTimeoutStopUSec = default;

    public ulong DefaultTimeoutStopUSec
    {
        get
        {
            return _DefaultTimeoutStopUSec;
        }

        set
        {
            _DefaultTimeoutStopUSec = (value);
        }
    }

    private ulong _DefaultTimeoutAbortUSec = default;

    public ulong DefaultTimeoutAbortUSec
    {
        get
        {
            return _DefaultTimeoutAbortUSec;
        }

        set
        {
            _DefaultTimeoutAbortUSec = (value);
        }
    }

    private ulong _DefaultDeviceTimeoutUSec = default;

    public ulong DefaultDeviceTimeoutUSec
    {
        get
        {
            return _DefaultDeviceTimeoutUSec;
        }

        set
        {
            _DefaultDeviceTimeoutUSec = (value);
        }
    }

    private ulong _DefaultRestartUSec = default;

    public ulong DefaultRestartUSec
    {
        get
        {
            return _DefaultRestartUSec;
        }

        set
        {
            _DefaultRestartUSec = (value);
        }
    }

    private ulong _DefaultStartLimitIntervalUSec = default;

    public ulong DefaultStartLimitIntervalUSec
    {
        get
        {
            return _DefaultStartLimitIntervalUSec;
        }

        set
        {
            _DefaultStartLimitIntervalUSec = (value);
        }
    }

    private uint _DefaultStartLimitBurst = default;

    public uint DefaultStartLimitBurst
    {
        get
        {
            return _DefaultStartLimitBurst;
        }

        set
        {
            _DefaultStartLimitBurst = (value);
        }
    }

    private bool _DefaultCPUAccounting = default;

    public bool DefaultCPUAccounting
    {
        get
        {
            return _DefaultCPUAccounting;
        }

        set
        {
            _DefaultCPUAccounting = (value);
        }
    }

    private bool _DefaultBlockIOAccounting = default;

    public bool DefaultBlockIOAccounting
    {
        get
        {
            return _DefaultBlockIOAccounting;
        }

        set
        {
            _DefaultBlockIOAccounting = (value);
        }
    }

    private bool _DefaultIOAccounting = default;

    public bool DefaultIOAccounting
    {
        get
        {
            return _DefaultIOAccounting;
        }

        set
        {
            _DefaultIOAccounting = (value);
        }
    }

    private bool _DefaultIPAccounting = default;

    public bool DefaultIPAccounting
    {
        get
        {
            return _DefaultIPAccounting;
        }

        set
        {
            _DefaultIPAccounting = (value);
        }
    }

    private bool _DefaultMemoryAccounting = default;

    public bool DefaultMemoryAccounting
    {
        get
        {
            return _DefaultMemoryAccounting;
        }

        set
        {
            _DefaultMemoryAccounting = (value);
        }
    }

    private bool _DefaultTasksAccounting = default;

    public bool DefaultTasksAccounting
    {
        get
        {
            return _DefaultTasksAccounting;
        }

        set
        {
            _DefaultTasksAccounting = (value);
        }
    }

    private ulong _DefaultLimitCPU = default;

    public ulong DefaultLimitCPU
    {
        get
        {
            return _DefaultLimitCPU;
        }

        set
        {
            _DefaultLimitCPU = (value);
        }
    }

    private ulong _DefaultLimitCPUSoft = default;

    public ulong DefaultLimitCPUSoft
    {
        get
        {
            return _DefaultLimitCPUSoft;
        }

        set
        {
            _DefaultLimitCPUSoft = (value);
        }
    }

    private ulong _DefaultLimitFSIZE = default;

    public ulong DefaultLimitFSIZE
    {
        get
        {
            return _DefaultLimitFSIZE;
        }

        set
        {
            _DefaultLimitFSIZE = (value);
        }
    }

    private ulong _DefaultLimitFSIZESoft = default;

    public ulong DefaultLimitFSIZESoft
    {
        get
        {
            return _DefaultLimitFSIZESoft;
        }

        set
        {
            _DefaultLimitFSIZESoft = (value);
        }
    }

    private ulong _DefaultLimitDATA = default;

    public ulong DefaultLimitDATA
    {
        get
        {
            return _DefaultLimitDATA;
        }

        set
        {
            _DefaultLimitDATA = (value);
        }
    }

    private ulong _DefaultLimitDATASoft = default;

    public ulong DefaultLimitDATASoft
    {
        get
        {
            return _DefaultLimitDATASoft;
        }

        set
        {
            _DefaultLimitDATASoft = (value);
        }
    }

    private ulong _DefaultLimitSTACK = default;

    public ulong DefaultLimitSTACK
    {
        get
        {
            return _DefaultLimitSTACK;
        }

        set
        {
            _DefaultLimitSTACK = (value);
        }
    }

    private ulong _DefaultLimitSTACKSoft = default;

    public ulong DefaultLimitSTACKSoft
    {
        get
        {
            return _DefaultLimitSTACKSoft;
        }

        set
        {
            _DefaultLimitSTACKSoft = (value);
        }
    }

    private ulong _DefaultLimitCORE = default;

    public ulong DefaultLimitCORE
    {
        get
        {
            return _DefaultLimitCORE;
        }

        set
        {
            _DefaultLimitCORE = (value);
        }
    }

    private ulong _DefaultLimitCORESoft = default;

    public ulong DefaultLimitCORESoft
    {
        get
        {
            return _DefaultLimitCORESoft;
        }

        set
        {
            _DefaultLimitCORESoft = (value);
        }
    }

    private ulong _DefaultLimitRSS = default;

    public ulong DefaultLimitRSS
    {
        get
        {
            return _DefaultLimitRSS;
        }

        set
        {
            _DefaultLimitRSS = (value);
        }
    }

    private ulong _DefaultLimitRSSSoft = default;

    public ulong DefaultLimitRSSSoft
    {
        get
        {
            return _DefaultLimitRSSSoft;
        }

        set
        {
            _DefaultLimitRSSSoft = (value);
        }
    }

    private ulong _DefaultLimitNOFILE = default;

    public ulong DefaultLimitNOFILE
    {
        get
        {
            return _DefaultLimitNOFILE;
        }

        set
        {
            _DefaultLimitNOFILE = (value);
        }
    }

    private ulong _DefaultLimitNOFILESoft = default;

    public ulong DefaultLimitNOFILESoft
    {
        get
        {
            return _DefaultLimitNOFILESoft;
        }

        set
        {
            _DefaultLimitNOFILESoft = (value);
        }
    }

    private ulong _DefaultLimitAS = default;

    public ulong DefaultLimitAS
    {
        get
        {
            return _DefaultLimitAS;
        }

        set
        {
            _DefaultLimitAS = (value);
        }
    }

    private ulong _DefaultLimitASSoft = default;

    public ulong DefaultLimitASSoft
    {
        get
        {
            return _DefaultLimitASSoft;
        }

        set
        {
            _DefaultLimitASSoft = (value);
        }
    }

    private ulong _DefaultLimitNPROC = default;

    public ulong DefaultLimitNPROC
    {
        get
        {
            return _DefaultLimitNPROC;
        }

        set
        {
            _DefaultLimitNPROC = (value);
        }
    }

    private ulong _DefaultLimitNPROCSoft = default;

    public ulong DefaultLimitNPROCSoft
    {
        get
        {
            return _DefaultLimitNPROCSoft;
        }

        set
        {
            _DefaultLimitNPROCSoft = (value);
        }
    }

    private ulong _DefaultLimitMEMLOCK = default;

    public ulong DefaultLimitMEMLOCK
    {
        get
        {
            return _DefaultLimitMEMLOCK;
        }

        set
        {
            _DefaultLimitMEMLOCK = (value);
        }
    }

    private ulong _DefaultLimitMEMLOCKSoft = default;

    public ulong DefaultLimitMEMLOCKSoft
    {
        get
        {
            return _DefaultLimitMEMLOCKSoft;
        }

        set
        {
            _DefaultLimitMEMLOCKSoft = (value);
        }
    }

    private ulong _DefaultLimitLOCKS = default;

    public ulong DefaultLimitLOCKS
    {
        get
        {
            return _DefaultLimitLOCKS;
        }

        set
        {
            _DefaultLimitLOCKS = (value);
        }
    }

    private ulong _DefaultLimitLOCKSSoft = default;

    public ulong DefaultLimitLOCKSSoft
    {
        get
        {
            return _DefaultLimitLOCKSSoft;
        }

        set
        {
            _DefaultLimitLOCKSSoft = (value);
        }
    }

    private ulong _DefaultLimitSIGPENDING = default;

    public ulong DefaultLimitSIGPENDING
    {
        get
        {
            return _DefaultLimitSIGPENDING;
        }

        set
        {
            _DefaultLimitSIGPENDING = (value);
        }
    }

    private ulong _DefaultLimitSIGPENDINGSoft = default;

    public ulong DefaultLimitSIGPENDINGSoft
    {
        get
        {
            return _DefaultLimitSIGPENDINGSoft;
        }

        set
        {
            _DefaultLimitSIGPENDINGSoft = (value);
        }
    }

    private ulong _DefaultLimitMSGQUEUE = default;

    public ulong DefaultLimitMSGQUEUE
    {
        get
        {
            return _DefaultLimitMSGQUEUE;
        }

        set
        {
            _DefaultLimitMSGQUEUE = (value);
        }
    }

    private ulong _DefaultLimitMSGQUEUESoft = default;

    public ulong DefaultLimitMSGQUEUESoft
    {
        get
        {
            return _DefaultLimitMSGQUEUESoft;
        }

        set
        {
            _DefaultLimitMSGQUEUESoft = (value);
        }
    }

    private ulong _DefaultLimitNICE = default;

    public ulong DefaultLimitNICE
    {
        get
        {
            return _DefaultLimitNICE;
        }

        set
        {
            _DefaultLimitNICE = (value);
        }
    }

    private ulong _DefaultLimitNICESoft = default;

    public ulong DefaultLimitNICESoft
    {
        get
        {
            return _DefaultLimitNICESoft;
        }

        set
        {
            _DefaultLimitNICESoft = (value);
        }
    }

    private ulong _DefaultLimitRTPRIO = default;

    public ulong DefaultLimitRTPRIO
    {
        get
        {
            return _DefaultLimitRTPRIO;
        }

        set
        {
            _DefaultLimitRTPRIO = (value);
        }
    }

    private ulong _DefaultLimitRTPRIOSoft = default;

    public ulong DefaultLimitRTPRIOSoft
    {
        get
        {
            return _DefaultLimitRTPRIOSoft;
        }

        set
        {
            _DefaultLimitRTPRIOSoft = (value);
        }
    }

    private ulong _DefaultLimitRTTIME = default;

    public ulong DefaultLimitRTTIME
    {
        get
        {
            return _DefaultLimitRTTIME;
        }

        set
        {
            _DefaultLimitRTTIME = (value);
        }
    }

    private ulong _DefaultLimitRTTIMESoft = default;

    public ulong DefaultLimitRTTIMESoft
    {
        get
        {
            return _DefaultLimitRTTIMESoft;
        }

        set
        {
            _DefaultLimitRTTIMESoft = (value);
        }
    }

    private ulong _DefaultTasksMax = default;

    public ulong DefaultTasksMax
    {
        get
        {
            return _DefaultTasksMax;
        }

        set
        {
            _DefaultTasksMax = (value);
        }
    }

    private ulong _DefaultMemoryPressureThresholdUSec = default;

    public ulong DefaultMemoryPressureThresholdUSec
    {
        get
        {
            return _DefaultMemoryPressureThresholdUSec;
        }

        set
        {
            _DefaultMemoryPressureThresholdUSec = (value);
        }
    }

    private string _DefaultMemoryPressureWatch = default;

    public string DefaultMemoryPressureWatch
    {
        get
        {
            return _DefaultMemoryPressureWatch;
        }

        set
        {
            _DefaultMemoryPressureWatch = (value);
        }
    }

    private ulong _TimerSlackNSec = default;

    public ulong TimerSlackNSec
    {
        get
        {
            return _TimerSlackNSec;
        }

        set
        {
            _TimerSlackNSec = (value);
        }
    }

    private string _DefaultOOMPolicy = default;

    public string DefaultOOMPolicy
    {
        get
        {
            return _DefaultOOMPolicy;
        }

        set
        {
            _DefaultOOMPolicy = (value);
        }
    }

    private int _DefaultOOMScoreAdjust = default;

    public int DefaultOOMScoreAdjust
    {
        get
        {
            return _DefaultOOMScoreAdjust;
        }

        set
        {
            _DefaultOOMScoreAdjust = (value);
        }
    }

    private string _CtrlAltDelBurstAction = default;

    public string CtrlAltDelBurstAction
    {
        get
        {
            return _CtrlAltDelBurstAction;
        }

        set
        {
            _CtrlAltDelBurstAction = (value);
        }
    }
}

public static class ManagerExtensions
{
    public static Task<string> GetVersionAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("Version");
    }

    public static Task<string> GetFeaturesAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("Features");
    }

    public static Task<string> GetVirtualizationAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("Virtualization");
    }

    public static Task<string> GetConfidentialVirtualizationAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("ConfidentialVirtualization");
    }

    public static Task<string> GetArchitectureAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("Architecture");
    }

    public static Task<string> GetTaintedAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("Tainted");
    }

    public static Task<ulong> GetFirmwareTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("FirmwareTimestamp");
    }

    public static Task<ulong> GetFirmwareTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("FirmwareTimestampMonotonic");
    }

    public static Task<ulong> GetLoaderTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("LoaderTimestamp");
    }

    public static Task<ulong> GetLoaderTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("LoaderTimestampMonotonic");
    }

    public static Task<ulong> GetKernelTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("KernelTimestamp");
    }

    public static Task<ulong> GetKernelTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("KernelTimestampMonotonic");
    }

    public static Task<ulong> GetInitRDTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("InitRDTimestamp");
    }

    public static Task<ulong> GetInitRDTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("InitRDTimestampMonotonic");
    }

    public static Task<ulong> GetUserspaceTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("UserspaceTimestamp");
    }

    public static Task<ulong> GetUserspaceTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("UserspaceTimestampMonotonic");
    }

    public static Task<ulong> GetFinishTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("FinishTimestamp");
    }

    public static Task<ulong> GetFinishTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("FinishTimestampMonotonic");
    }

    public static Task<ulong> GetSecurityStartTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("SecurityStartTimestamp");
    }

    public static Task<ulong> GetSecurityStartTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("SecurityStartTimestampMonotonic");
    }

    public static Task<ulong> GetSecurityFinishTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("SecurityFinishTimestamp");
    }

    public static Task<ulong> GetSecurityFinishTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("SecurityFinishTimestampMonotonic");
    }

    public static Task<ulong> GetGeneratorsStartTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("GeneratorsStartTimestamp");
    }

    public static Task<ulong> GetGeneratorsStartTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("GeneratorsStartTimestampMonotonic");
    }

    public static Task<ulong> GetGeneratorsFinishTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("GeneratorsFinishTimestamp");
    }

    public static Task<ulong> GetGeneratorsFinishTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("GeneratorsFinishTimestampMonotonic");
    }

    public static Task<ulong> GetUnitsLoadStartTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("UnitsLoadStartTimestamp");
    }

    public static Task<ulong> GetUnitsLoadStartTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("UnitsLoadStartTimestampMonotonic");
    }

    public static Task<ulong> GetUnitsLoadFinishTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("UnitsLoadFinishTimestamp");
    }

    public static Task<ulong> GetUnitsLoadFinishTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("UnitsLoadFinishTimestampMonotonic");
    }

    public static Task<ulong> GetUnitsLoadTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("UnitsLoadTimestamp");
    }

    public static Task<ulong> GetUnitsLoadTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("UnitsLoadTimestampMonotonic");
    }

    public static Task<ulong> GetInitRDSecurityStartTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("InitRDSecurityStartTimestamp");
    }

    public static Task<ulong> GetInitRDSecurityStartTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("InitRDSecurityStartTimestampMonotonic");
    }

    public static Task<ulong> GetInitRDSecurityFinishTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("InitRDSecurityFinishTimestamp");
    }

    public static Task<ulong> GetInitRDSecurityFinishTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("InitRDSecurityFinishTimestampMonotonic");
    }

    public static Task<ulong> GetInitRDGeneratorsStartTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("InitRDGeneratorsStartTimestamp");
    }

    public static Task<ulong> GetInitRDGeneratorsStartTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("InitRDGeneratorsStartTimestampMonotonic");
    }

    public static Task<ulong> GetInitRDGeneratorsFinishTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("InitRDGeneratorsFinishTimestamp");
    }

    public static Task<ulong> GetInitRDGeneratorsFinishTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("InitRDGeneratorsFinishTimestampMonotonic");
    }

    public static Task<ulong> GetInitRDUnitsLoadStartTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("InitRDUnitsLoadStartTimestamp");
    }

    public static Task<ulong> GetInitRDUnitsLoadStartTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("InitRDUnitsLoadStartTimestampMonotonic");
    }

    public static Task<ulong> GetInitRDUnitsLoadFinishTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("InitRDUnitsLoadFinishTimestamp");
    }

    public static Task<ulong> GetInitRDUnitsLoadFinishTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("InitRDUnitsLoadFinishTimestampMonotonic");
    }

    public static Task<string> GetLogLevelAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("LogLevel");
    }

    public static Task<string> GetLogTargetAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("LogTarget");
    }

    public static Task<uint> GetNNamesAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>("NNames");
    }

    public static Task<uint> GetNFailedUnitsAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>("NFailedUnits");
    }

    public static Task<uint> GetNJobsAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>("NJobs");
    }

    public static Task<uint> GetNInstalledJobsAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>("NInstalledJobs");
    }

    public static Task<uint> GetNFailedJobsAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>("NFailedJobs");
    }

    public static Task<double> GetProgressAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<double>("Progress");
    }

    public static Task<string[]> GetEnvironmentAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>("Environment");
    }

    public static Task<bool> GetConfirmSpawnAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>("ConfirmSpawn");
    }

    public static Task<bool> GetShowStatusAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>("ShowStatus");
    }

    public static Task<string[]> GetUnitPathAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>("UnitPath");
    }

    public static Task<string> GetDefaultStandardOutputAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("DefaultStandardOutput");
    }

    public static Task<string> GetDefaultStandardErrorAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("DefaultStandardError");
    }

    public static Task<string> GetWatchdogDeviceAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("WatchdogDevice");
    }

    public static Task<ulong> GetWatchdogLastPingTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("WatchdogLastPingTimestamp");
    }

    public static Task<ulong> GetWatchdogLastPingTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("WatchdogLastPingTimestampMonotonic");
    }

    public static Task<ulong> GetRuntimeWatchdogUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("RuntimeWatchdogUSec");
    }

    public static Task<ulong> GetRuntimeWatchdogPreUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("RuntimeWatchdogPreUSec");
    }

    public static Task<string> GetRuntimeWatchdogPreGovernorAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("RuntimeWatchdogPreGovernor");
    }

    public static Task<ulong> GetRebootWatchdogUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("RebootWatchdogUSec");
    }

    public static Task<ulong> GetKExecWatchdogUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("KExecWatchdogUSec");
    }

    public static Task<bool> GetServiceWatchdogsAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>("ServiceWatchdogs");
    }

    public static Task<string> GetControlGroupAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("ControlGroup");
    }

    public static Task<string> GetSystemStateAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("SystemState");
    }

    public static Task<byte> GetExitCodeAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<byte>("ExitCode");
    }

    public static Task<ulong> GetDefaultTimerAccuracyUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultTimerAccuracyUSec");
    }

    public static Task<ulong> GetDefaultTimeoutStartUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultTimeoutStartUSec");
    }

    public static Task<ulong> GetDefaultTimeoutStopUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultTimeoutStopUSec");
    }

    public static Task<ulong> GetDefaultTimeoutAbortUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultTimeoutAbortUSec");
    }

    public static Task<ulong> GetDefaultDeviceTimeoutUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultDeviceTimeoutUSec");
    }

    public static Task<ulong> GetDefaultRestartUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultRestartUSec");
    }

    public static Task<ulong> GetDefaultStartLimitIntervalUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultStartLimitIntervalUSec");
    }

    public static Task<uint> GetDefaultStartLimitBurstAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>("DefaultStartLimitBurst");
    }

    public static Task<bool> GetDefaultCPUAccountingAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>("DefaultCPUAccounting");
    }

    public static Task<bool> GetDefaultBlockIOAccountingAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>("DefaultBlockIOAccounting");
    }

    public static Task<bool> GetDefaultIOAccountingAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>("DefaultIOAccounting");
    }

    public static Task<bool> GetDefaultIPAccountingAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>("DefaultIPAccounting");
    }

    public static Task<bool> GetDefaultMemoryAccountingAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>("DefaultMemoryAccounting");
    }

    public static Task<bool> GetDefaultTasksAccountingAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>("DefaultTasksAccounting");
    }

    public static Task<ulong> GetDefaultLimitCPUAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitCPU");
    }

    public static Task<ulong> GetDefaultLimitCPUSoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitCPUSoft");
    }

    public static Task<ulong> GetDefaultLimitFSIZEAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitFSIZE");
    }

    public static Task<ulong> GetDefaultLimitFSIZESoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitFSIZESoft");
    }

    public static Task<ulong> GetDefaultLimitDATAAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitDATA");
    }

    public static Task<ulong> GetDefaultLimitDATASoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitDATASoft");
    }

    public static Task<ulong> GetDefaultLimitSTACKAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitSTACK");
    }

    public static Task<ulong> GetDefaultLimitSTACKSoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitSTACKSoft");
    }

    public static Task<ulong> GetDefaultLimitCOREAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitCORE");
    }

    public static Task<ulong> GetDefaultLimitCORESoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitCORESoft");
    }

    public static Task<ulong> GetDefaultLimitRSSAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitRSS");
    }

    public static Task<ulong> GetDefaultLimitRSSSoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitRSSSoft");
    }

    public static Task<ulong> GetDefaultLimitNOFILEAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitNOFILE");
    }

    public static Task<ulong> GetDefaultLimitNOFILESoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitNOFILESoft");
    }

    public static Task<ulong> GetDefaultLimitASAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitAS");
    }

    public static Task<ulong> GetDefaultLimitASSoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitASSoft");
    }

    public static Task<ulong> GetDefaultLimitNPROCAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitNPROC");
    }

    public static Task<ulong> GetDefaultLimitNPROCSoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitNPROCSoft");
    }

    public static Task<ulong> GetDefaultLimitMEMLOCKAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitMEMLOCK");
    }

    public static Task<ulong> GetDefaultLimitMEMLOCKSoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitMEMLOCKSoft");
    }

    public static Task<ulong> GetDefaultLimitLOCKSAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitLOCKS");
    }

    public static Task<ulong> GetDefaultLimitLOCKSSoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitLOCKSSoft");
    }

    public static Task<ulong> GetDefaultLimitSIGPENDINGAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitSIGPENDING");
    }

    public static Task<ulong> GetDefaultLimitSIGPENDINGSoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitSIGPENDINGSoft");
    }

    public static Task<ulong> GetDefaultLimitMSGQUEUEAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitMSGQUEUE");
    }

    public static Task<ulong> GetDefaultLimitMSGQUEUESoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitMSGQUEUESoft");
    }

    public static Task<ulong> GetDefaultLimitNICEAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitNICE");
    }

    public static Task<ulong> GetDefaultLimitNICESoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitNICESoft");
    }

    public static Task<ulong> GetDefaultLimitRTPRIOAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitRTPRIO");
    }

    public static Task<ulong> GetDefaultLimitRTPRIOSoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitRTPRIOSoft");
    }

    public static Task<ulong> GetDefaultLimitRTTIMEAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitRTTIME");
    }

    public static Task<ulong> GetDefaultLimitRTTIMESoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitRTTIMESoft");
    }

    public static Task<ulong> GetDefaultTasksMaxAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultTasksMax");
    }

    public static Task<ulong> GetDefaultMemoryPressureThresholdUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultMemoryPressureThresholdUSec");
    }

    public static Task<string> GetDefaultMemoryPressureWatchAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("DefaultMemoryPressureWatch");
    }

    public static Task<ulong> GetTimerSlackNSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("TimerSlackNSec");
    }

    public static Task<string> GetDefaultOOMPolicyAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("DefaultOOMPolicy");
    }

    public static Task<int> GetDefaultOOMScoreAdjustAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<int>("DefaultOOMScoreAdjust");
    }

    public static Task<string> GetCtrlAltDelBurstActionAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("CtrlAltDelBurstAction");
    }

    public static Task SetLogLevelAsync(this ISystemdManager o, string val)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.SetAsync("LogLevel", val);
    }

    public static Task SetLogTargetAsync(this ISystemdManager o, string val)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.SetAsync("LogTarget", val);
    }

    public static Task SetRuntimeWatchdogUSecAsync(this ISystemdManager o, ulong val)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.SetAsync("RuntimeWatchdogUSec", val);
    }

    public static Task SetRuntimeWatchdogPreUSecAsync(this ISystemdManager o, ulong val)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.SetAsync("RuntimeWatchdogPreUSec", val);
    }

    public static Task SetRuntimeWatchdogPreGovernorAsync(this ISystemdManager o, string val)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.SetAsync("RuntimeWatchdogPreGovernor", val);
    }

    public static Task SetRebootWatchdogUSecAsync(this ISystemdManager o, ulong val)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.SetAsync("RebootWatchdogUSec", val);
    }

    public static Task SetKExecWatchdogUSecAsync(this ISystemdManager o, ulong val)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.SetAsync("KExecWatchdogUSec", val);
    }

    public static Task SetServiceWatchdogsAsync(this ISystemdManager o, bool val)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.SetAsync("ServiceWatchdogs", val);
    }
}
