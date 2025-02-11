// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Linux.Models;
using Tmds.DBus;

namespace RemoteMaster.Host.Linux.Abstractions;

[DBusInterface("org.freedesktop.systemd1.Service")]
public interface ISystemdService : IDBusObject
{
    Task BindMountAsync(string Source, string Destination, bool ReadOnly, bool Mkdir);
    
    Task MountImageAsync(string Source, string Destination, bool ReadOnly, bool Mkdir, (string, string)[] Options);
    
    Task<(string, uint, uint, uint, ulong, uint, uint, string, uint)[]> DumpFileDescriptorStoreAsync();
    
    Task<(string, uint, string)[]> GetProcessesAsync();
    
    Task AttachProcessesAsync(string Subcgroup, uint[] Pids);
    
    Task<T> GetAsync<T>(string prop);
    
    Task<SystemdServiceProperties> GetAllAsync();
    
    Task SetAsync(string prop, object val);
    
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
}
