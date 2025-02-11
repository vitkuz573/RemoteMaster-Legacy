// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Linux.Models;
using Tmds.DBus;

namespace RemoteMaster.Host.Linux.Abstractions;

[DBusInterface("org.freedesktop.login1.Session")]
public interface ILoginSession : IDBusObject
{
    Task TerminateAsync();
    
    Task ActivateAsync();
    
    Task LockAsync();
    
    Task UnlockAsync();
    
    Task SetIdleHintAsync(bool Idle);
    
    Task SetLockedHintAsync(bool Locked);
    
    Task KillAsync(string Who, int SignalNumber);
    
    Task TakeControlAsync(bool Force);
    
    Task ReleaseControlAsync();
    
    Task SetTypeAsync(string Type);
    
    Task SetDisplayAsync(string Display);
    
    Task SetTTYAsync(CloseSafeHandle TtyFd);
    
    Task<(CloseSafeHandle fd, bool inactive)> TakeDeviceAsync(uint Major, uint Minor);
    
    Task ReleaseDeviceAsync(uint Major, uint Minor);
    
    Task PauseDeviceCompleteAsync(uint Major, uint Minor);
    
    Task SetBrightnessAsync(string Subsystem, string Name, uint Brightness);
    
    Task<IDisposable> WatchPauseDeviceAsync(Action<(uint major, uint minor, string type)> handler, Action<Exception> onError = null);
    
    Task<IDisposable> WatchResumeDeviceAsync(Action<(uint major, uint minor, CloseSafeHandle fd)> handler, Action<Exception> onError = null);
    
    Task<IDisposable> WatchLockAsync(Action handler, Action<Exception> onError = null);
    
    Task<IDisposable> WatchUnlockAsync(Action handler, Action<Exception> onError = null);
    
    Task<T> GetAsync<T>(string prop);
    
    Task<LoginSessionProperties> GetAllAsync();

    Task SetAsync(string prop, object val);
    
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
}
