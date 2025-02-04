// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Tmds.DBus;

namespace RemoteMaster.Host.Linux.Abstractions;

[DBusInterface("org.freedesktop.portal.Screenshot")]
public interface IScreenshotPortal : IDBusObject
{
    Task<(uint response, string? uri)> ScreenshotAsync(string parent, IDictionary<string, object> options);
}
