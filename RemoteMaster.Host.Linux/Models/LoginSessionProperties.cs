// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Tmds.DBus;

namespace RemoteMaster.Host.Linux.Models;

[Dictionary]
public class LoginSessionProperties
{
    public string Id { get; set; } = string.Empty;

    public (uint, ObjectPath) User { get; set; }

    public string Name { get; set; } = string.Empty;

    public ulong Timestamp { get; set; }

    public ulong TimestampMonotonic { get; set; }

    public uint VTNr { get; set; }

    public (string, ObjectPath) Seat { get; set; }

    public string TTY { get; set; } = string.Empty;

    public string Display { get; set; } = string.Empty;

    public bool Remote { get; set; }

    public string RemoteHost { get; set; } = string.Empty;

    public string RemoteUser { get; set; } = string.Empty;

    public string Service { get; set; } = string.Empty;

    public string Desktop { get; set; } = string.Empty;

    public string Scope { get; set; } = string.Empty;

    public uint Leader { get; set; }

    public uint Audit { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Class { get; set; } = string.Empty;

    public bool Active { get; set; }

    public string State { get; set; } = string.Empty;

    public bool IdleHint { get; set; }

    public ulong IdleSinceHint { get; set; }

    public ulong IdleSinceHintMonotonic { get; set; }

    public bool LockedHint { get; set; }
}
