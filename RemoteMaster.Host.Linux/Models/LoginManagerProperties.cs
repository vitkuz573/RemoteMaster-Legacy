// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Tmds.DBus;

namespace RemoteMaster.Host.Linux.Models;

[Dictionary]
public class LoginManagerProperties
{
    public bool EnableWallMessages { get; set; } = default;

    public string WallMessage { get; set; } = string.Empty;

    public uint NAutoVTs { get; set; } = default;

    public string[] KillOnlyUsers { get; set; } = [];

    public string[] KillExcludeUsers { get; set; } = [];

    public bool KillUserProcesses { get; set; } = default;

    public string RebootParameter { get; set; } = string.Empty;

    public bool RebootToFirmwareSetup { get; set; } = default;

    public ulong RebootToBootLoaderMenu { get; set; } = default;

    public string RebootToBootLoaderEntry { get; set; } = string.Empty;

    public string[] BootLoaderEntries { get; set; } = [];

    public bool IdleHint { get; set; } = default;

    public ulong IdleSinceHint { get; set; } = default;

    public ulong IdleSinceHintMonotonic { get; set; } = default;

    public string BlockInhibited { get; set; } = string.Empty;

    public string DelayInhibited { get; set; } = string.Empty;

    public ulong InhibitDelayMaxUSec { get; set; } = default;

    public ulong UserStopDelayUSec { get; set; } = default;

    public string HandlePowerKey { get; set; } = string.Empty;

    public string HandlePowerKeyLongPress { get; set; } = string.Empty;

    public string HandleRebootKey { get; set; } = string.Empty;

    public string HandleRebootKeyLongPress { get; set; } = string.Empty;

    public string HandleSuspendKey { get; set; } = string.Empty;

    public string HandleSuspendKeyLongPress { get; set; } = string.Empty;

    public string HandleHibernateKey { get; set; } = string.Empty;

    public string HandleHibernateKeyLongPress { get; set; } = string.Empty;

    public string HandleLidSwitch { get; set; } = string.Empty;

    public string HandleLidSwitchExternalPower { get; set; } = string.Empty;

    public string HandleLidSwitchDocked { get; set; } = string.Empty;

    public ulong HoldoffTimeoutUSec { get; set; } = default;

    public string IdleAction { get; set; } = string.Empty;

    public ulong IdleActionUSec { get; set; } = default;

    public bool PreparingForShutdown { get; set; } = default;

    public bool PreparingForSleep { get; set; } = default;

    public (string, ulong) ScheduledShutdown { get; set; } = default;

    public bool Docked { get; set; } = default;

    public bool LidClosed { get; set; } = default;

    public bool OnExternalPower { get; set; } = default;

    public bool RemoveIPC { get; set; } = default;

    public ulong RuntimeDirectorySize { get; set; } = default;

    public ulong RuntimeDirectoryInodesMax { get; set; } = default;

    public ulong InhibitorsMax { get; set; } = default;

    public ulong NCurrentInhibitors { get; set; } = default;

    public ulong SessionsMax { get; set; } = default;

    public ulong NCurrentSessions { get; set; } = default;

    public ulong StopIdleSessionUSec { get; set; } = default;
}
