// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Linux.Models;
using Tmds.DBus;

namespace RemoteMaster.Host.Linux.Abstractions;

[DBusInterface("org.freedesktop.systemd1.Unit")]
public interface ISystemdUnit : IDBusObject
{
    Task<ObjectPath> StartAsync(string Mode);
    
    Task<ObjectPath> StopAsync(string Mode);
    
    Task<ObjectPath> ReloadAsync(string Mode);
    
    Task<ObjectPath> RestartAsync(string Mode);
    
    Task<ObjectPath> TryRestartAsync(string Mode);
    
    Task<ObjectPath> ReloadOrRestartAsync(string Mode);
    
    Task<ObjectPath> ReloadOrTryRestartAsync(string Mode);
    
    Task<(uint jobId, ObjectPath jobPath, string unitId, ObjectPath unitPath, string jobType, (uint, ObjectPath, string, ObjectPath, string)[] affectedJobs)> EnqueueJobAsync(string JobType, string JobMode);
    
    Task KillAsync(string Whom, int Signal);
    
    Task QueueSignalAsync(string Whom, int Signal, int Value);
    
    Task ResetFailedAsync();
    
    Task SetPropertiesAsync(bool Runtime, (string, object)[] Properties);
    
    Task RefAsync();
    
    Task UnrefAsync();
    
    Task CleanAsync(string[] Mask);
    
    Task FreezeAsync();
    
    Task ThawAsync();
    
    Task<T> GetAsync<T>(string prop);
    
    Task<SystemdUnitProperties> GetAllAsync();
    
    Task SetAsync(string prop, object val);
    
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
}
