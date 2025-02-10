// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Linux.Models;
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
    
    Task<IDisposable> WatchSessionNewAsync(Action<(string sessionId, ObjectPath objectPath)> handler, Action<Exception>? onError = null);
    
    Task<IDisposable> WatchSessionRemovedAsync(Action<(string sessionId, ObjectPath objectPath)> handler, Action<Exception>? onError = null);
    
    Task<IDisposable> WatchUserNewAsync(Action<(uint uid, ObjectPath objectPath)> handler, Action<Exception>? onError = null);
    
    Task<IDisposable> WatchUserRemovedAsync(Action<(uint uid, ObjectPath objectPath)> handler, Action<Exception>? onError = null);
    
    Task<IDisposable> WatchSeatNewAsync(Action<(string seatId, ObjectPath objectPath)> handler, Action<Exception>? onError = null);
    
    Task<IDisposable> WatchSeatRemovedAsync(Action<(string seatId, ObjectPath objectPath)> handler, Action<Exception>? onError = null);
    
    Task<IDisposable> WatchPrepareForShutdownAsync(Action<bool> handler, Action<Exception>? onError = null);
    
    Task<IDisposable> WatchPrepareForShutdownWithMetadataAsync(Action<(bool start, IDictionary<string, object> metadata)> handler, Action<Exception>? onError = null);
    
    Task<IDisposable> WatchPrepareForSleepAsync(Action<bool> handler, Action<Exception>? onError = null);
    
    Task<T> GetAsync<T>(string prop);
    
    Task<LoginManagerProperties> GetAllAsync();
    
    Task SetAsync(string prop, object val);
    
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
}
