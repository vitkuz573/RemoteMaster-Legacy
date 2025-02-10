// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Tmds.DBus;

namespace RemoteMaster.Host.Linux.Models;

[Dictionary]
public class LoginManagerProperties
{
    private bool _EnableWallMessages = default;

    public bool EnableWallMessages
    {
        get => _EnableWallMessages;
        set => _EnableWallMessages = value;
    }

    private string _WallMessage = string.Empty;

    public string WallMessage
    {
        get => _WallMessage;
        set => _WallMessage = value;
    }

    private uint _NAutoVTs = default;

    public uint NAutoVTs
    {
        get => _NAutoVTs;
        set => _NAutoVTs = value;
    }

    private string[] _KillOnlyUsers = [];

    public string[] KillOnlyUsers
    {
        get => _KillOnlyUsers;
        set => _KillOnlyUsers = value;
    }

    private string[] _KillExcludeUsers = [];

    public string[] KillExcludeUsers
    {
        get => _KillExcludeUsers;
        set => _KillExcludeUsers = value;
    }

    private bool _KillUserProcesses = default;

    public bool KillUserProcesses
    {
        get => _KillUserProcesses;
        set => _KillUserProcesses = value;
    }

    private string _RebootParameter = string.Empty;

    public string RebootParameter
    {
        get => _RebootParameter;
        set => _RebootParameter = value;
    }

    private bool _RebootToFirmwareSetup = default;

    public bool RebootToFirmwareSetup
    {
        get => _RebootToFirmwareSetup;
        set => _RebootToFirmwareSetup = value;
    }

    private ulong _RebootToBootLoaderMenu = default;

    public ulong RebootToBootLoaderMenu
    {
        get => _RebootToBootLoaderMenu;
        set => _RebootToBootLoaderMenu = value;
    }

    private string _RebootToBootLoaderEntry = string.Empty;

    public string RebootToBootLoaderEntry
    {
        get => _RebootToBootLoaderEntry;
        set => _RebootToBootLoaderEntry = value;
    }

    private string[] _BootLoaderEntries = [];

    public string[] BootLoaderEntries
    {
        get => _BootLoaderEntries;
        set => _BootLoaderEntries = value;
    }

    private bool _IdleHint = default;

    public bool IdleHint
    {
        get => _IdleHint;
        set => _IdleHint = value;
    }

    private ulong _IdleSinceHint = default;

    public ulong IdleSinceHint
    {
        get => _IdleSinceHint;
        set => _IdleSinceHint = value;
    }

    private ulong _IdleSinceHintMonotonic = default;

    public ulong IdleSinceHintMonotonic
    {
        get => _IdleSinceHintMonotonic;
        set => _IdleSinceHintMonotonic = value;
    }

    private string _BlockInhibited = string.Empty;

    public string BlockInhibited
    {
        get => _BlockInhibited;
        set => _BlockInhibited = value;
    }

    private string _DelayInhibited = string.Empty;

    public string DelayInhibited
    {
        get => _DelayInhibited;
        set => _DelayInhibited = value;
    }

    private ulong _InhibitDelayMaxUSec = default;

    public ulong InhibitDelayMaxUSec
    {
        get => _InhibitDelayMaxUSec;
        set => _InhibitDelayMaxUSec = value;
    }

    private ulong _UserStopDelayUSec = default;

    public ulong UserStopDelayUSec
    {
        get => _UserStopDelayUSec;
        set => _UserStopDelayUSec = value;
    }

    private string _HandlePowerKey = string.Empty;

    public string HandlePowerKey
    {
        get => _HandlePowerKey;
        set => _HandlePowerKey = value;
    }

    private string _HandlePowerKeyLongPress = string.Empty;

    public string HandlePowerKeyLongPress
    {
        get => _HandlePowerKeyLongPress;
        set => _HandlePowerKeyLongPress = value;
    }

    private string _HandleRebootKey = string.Empty;

    public string HandleRebootKey
    {
        get => _HandleRebootKey;
        set => _HandleRebootKey = value;
    }

    private string _HandleRebootKeyLongPress = string.Empty;

    public string HandleRebootKeyLongPress
    {
        get => _HandleRebootKeyLongPress;
        set => _HandleRebootKeyLongPress = value;
    }

    private string _HandleSuspendKey = string.Empty;

    public string HandleSuspendKey
    {
        get => _HandleSuspendKey;
        set => _HandleSuspendKey = value;
    }

    private string _HandleSuspendKeyLongPress = string.Empty;

    public string HandleSuspendKeyLongPress
    {
        get => _HandleSuspendKeyLongPress;
        set => _HandleSuspendKeyLongPress = value;
    }

    private string _HandleHibernateKey = string.Empty;

    public string HandleHibernateKey
    {
        get => _HandleHibernateKey;
        set => _HandleHibernateKey = value;
    }

    private string _HandleHibernateKeyLongPress = string.Empty;

    public string HandleHibernateKeyLongPress
    {
        get => _HandleHibernateKeyLongPress;
        set => _HandleHibernateKeyLongPress = value;
    }

    private string _HandleLidSwitch = string.Empty;

    public string HandleLidSwitch
    {
        get => _HandleLidSwitch;
        set => _HandleLidSwitch = value;
    }

    private string _HandleLidSwitchExternalPower = string.Empty;

    public string HandleLidSwitchExternalPower
    {
        get => _HandleLidSwitchExternalPower;
        set => _HandleLidSwitchExternalPower = value;
    }

    private string _HandleLidSwitchDocked = string.Empty;

    public string HandleLidSwitchDocked
    {
        get => _HandleLidSwitchDocked;
        set => _HandleLidSwitchDocked = value;
    }

    private ulong _HoldoffTimeoutUSec = default;

    public ulong HoldoffTimeoutUSec
    {
        get => _HoldoffTimeoutUSec;
        set => _HoldoffTimeoutUSec = value;
    }

    private string _IdleAction = string.Empty;

    public string IdleAction
    {
        get => _IdleAction;
        set => _IdleAction = value;
    }

    private ulong _IdleActionUSec = default;

    public ulong IdleActionUSec
    {
        get => _IdleActionUSec;
        set => _IdleActionUSec = value;
    }

    private bool _PreparingForShutdown = default;

    public bool PreparingForShutdown
    {
        get => _PreparingForShutdown;
        set => _PreparingForShutdown = value;
    }

    private bool _PreparingForSleep = default;

    public bool PreparingForSleep
    {
        get => _PreparingForSleep;
        set => _PreparingForSleep = value;
    }

    private (string, ulong) _ScheduledShutdown = default;

    public (string, ulong) ScheduledShutdown
    {
        get => _ScheduledShutdown;
        set => _ScheduledShutdown = value;
    }

    private bool _Docked = default;

    public bool Docked
    {
        get => _Docked;
        set => _Docked = value;
    }

    private bool _LidClosed = default;

    public bool LidClosed
    {
        get => _LidClosed;
        set => _LidClosed = value;
    }

    private bool _OnExternalPower = default;

    public bool OnExternalPower
    {
        get => _OnExternalPower;
        set => _OnExternalPower = value;
    }

    private bool _RemoveIPC = default;

    public bool RemoveIPC
    {
        get => _RemoveIPC;
        set => _RemoveIPC = value;
    }

    private ulong _RuntimeDirectorySize = default;

    public ulong RuntimeDirectorySize
    {
        get => _RuntimeDirectorySize;
        set => _RuntimeDirectorySize = value;
    }

    private ulong _RuntimeDirectoryInodesMax = default;

    public ulong RuntimeDirectoryInodesMax
    {
        get => _RuntimeDirectoryInodesMax;
        set => _RuntimeDirectoryInodesMax = value;
    }

    private ulong _InhibitorsMax = default;

    public ulong InhibitorsMax
    {
        get => _InhibitorsMax;
        set => _InhibitorsMax = value;
    }

    private ulong _NCurrentInhibitors = default;

    public ulong NCurrentInhibitors
    {
        get => _NCurrentInhibitors;
        set => _NCurrentInhibitors = value;
    }

    private ulong _SessionsMax = default;

    public ulong SessionsMax
    {
        get => _SessionsMax;
        set => _SessionsMax = value;
    }

    private ulong _NCurrentSessions = default;

    public ulong NCurrentSessions
    {
        get => _NCurrentSessions;
        set => _NCurrentSessions = value;
    }

    private ulong _StopIdleSessionUSec = default;

    public ulong StopIdleSessionUSec
    {
        get => _StopIdleSessionUSec;
        set => _StopIdleSessionUSec = value;
    }
}
