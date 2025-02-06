// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Tmds.DBus;

namespace RemoteMaster.Host.Linux.Abstractions;

[DBusInterface("org.freedesktop.systemd1.Manager")]
public interface ISystemdManager : IDBusObject
{
    Task<ObjectPath> StartTransientUnitAsync(string name, string mode, (string, object)[] properties, (string, (string, object)[])[] aux);
}
