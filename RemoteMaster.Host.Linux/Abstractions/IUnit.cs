// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Tmds.DBus;

namespace RemoteMaster.Host.Linux.Abstractions;

[DBusInterface("org.freedesktop.systemd1.Unit")]
public interface IUnit : IDBusObject
{
    Task<T> GetAsync<T>(string property);

    Task<UnitProperties> GetAllAsync();

    Task SetAsync(string property, object value);

    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
}
