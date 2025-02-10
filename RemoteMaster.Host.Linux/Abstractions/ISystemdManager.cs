// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Linux.Models;
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
    
    Task<IDisposable> WatchUnitNewAsync(Action<(string id, ObjectPath unit)> handler, Action<Exception>? onError = null);
    
    Task<IDisposable> WatchUnitRemovedAsync(Action<(string id, ObjectPath unit)> handler, Action<Exception>? onError = null);
    
    Task<IDisposable> WatchJobNewAsync(Action<(uint id, ObjectPath job, string unit)> handler, Action<Exception>? onError = null);
    
    Task<IDisposable> WatchJobRemovedAsync(Action<(uint id, ObjectPath job, string unit, string result)> handler, Action<Exception>? onError = null);
    
    Task<IDisposable> WatchStartupFinishedAsync(Action<(ulong firmware, ulong loader, ulong kernel, ulong initrd, ulong userspace, ulong total)> handler, Action<Exception>? onError = null);
    
    Task<IDisposable> WatchUnitFilesChangedAsync(Action handler, Action<Exception>? onError = null);
    
    Task<IDisposable> WatchReloadingAsync(Action<bool> handler, Action<Exception>? onError = null);
    
    Task<T> GetAsync<T>(string prop);
    
    Task<SystemdManagerProperties> GetAllAsync();
    
    Task SetAsync(string prop, object val);
    
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
}
