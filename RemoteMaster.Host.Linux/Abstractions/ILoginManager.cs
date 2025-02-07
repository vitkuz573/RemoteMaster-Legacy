// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Tmds.DBus;

namespace RemoteMaster.Host.Linux.Abstractions;

[DBusInterface("org.freedesktop.login1.Manager")]
public interface ILoginManager : IDBusObject
{
    Task<IDisposable> WatchSessionNewAsync(Action<(string sessionId, ObjectPath sessionPath)> handler, Action<Exception>? onError = null);

    Task<IDisposable> WatchSessionRemovedAsync(Action<(string sessionId, ObjectPath sessionPath)> handler, Action<Exception>? onError = null);
}
